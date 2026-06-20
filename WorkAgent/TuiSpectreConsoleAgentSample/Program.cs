using System.Diagnostics;
using System.Text;

using Spectre.Console;
using Spectre.Console.Rendering;

using TuiAgentSampleCore;

// ---------------------------------------------------------------------------
// Spectre.Console 0.57.0 による「最近のエージェント風」チャット (基準実装)
//   - 出力ストリーム型ライブラリ。AnsiConsole.Live で 1 応答分の領域を更新描画。
//   - 思考スピナー / ツールカード / 本文ストリーミングを 1 つの Live で合成表示。
// ---------------------------------------------------------------------------

Console.OutputEncoding = Encoding.UTF8;

// 実エージェントへ差し替える場合は、IAgentConversation を実装した別クラスを
// この CreateAgent の戻り値に変えるだけでよい (UI 側は変更不要)。
var agent = CreateAgent();

PrintHeader(agent.ModelName);

while (true)
{
    var input = ReadInput();
    if (input is null)
    {
        break; // EOF (リダイレクト入力の終端)
    }

    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    var command = input.Trim();
    if (command.Equals("/exit", StringComparison.OrdinalIgnoreCase) ||
        command.Equals("exit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (command.Equals("/clear", StringComparison.OrdinalIgnoreCase))
    {
        AnsiConsole.Clear();
        PrintHeader(agent.ModelName);
        continue;
    }

    await RespondAsync(agent, input);
}

AnsiConsole.MarkupLine("[grey70]session closed.[/]");
return 0;

static IAgentConversation CreateAgent() => new SimulatedAgentConversation();

// 対話端末では Spectre のリッチプロンプト、リダイレクト入力時は素の ReadLine を使う。
static string? ReadInput()
{
    if (Console.IsInputRedirected)
    {
        AnsiConsole.Markup("[bold green]you[/] [grey70]>[/] ");
        return Console.ReadLine();
    }

    return AnsiConsole.Prompt(new TextPrompt<string>("[bold green]you[/] [grey70]>[/]").AllowEmpty());
}

static void PrintHeader(string model)
{
    AnsiConsole.WriteLine();
    string[] palette = ["aqua", "aqua", "deepskyblue1", "deepskyblue1", "dodgerblue1", "dodgerblue1"];
    var logo = AgentBranding.LogoLines;
    for (var i = 0; i < logo.Count; i++)
    {
        AnsiConsole.MarkupLine($"  [{palette[i % palette.Length]}]{Markup.Escape(logo[i])}[/]");
    }

    AnsiConsole.MarkupLine($"  [grey70]{Markup.Escape(AgentBranding.Tagline)}[/]");
    AnsiConsole.Write(new Rule($"[bold aqua] {AgentBranding.Title} - Spectre.Console [/]").RuleStyle("grey").Centered());
    AnsiConsole.MarkupLine(
        $"[grey70]model[/] [aqua]{Markup.Escape(model)}[/]    [grey70]|[/]    [yellow]simulated[/]    [grey70]|[/]    [grey70]commands[/] [grey70]/clear[/] [grey70]/exit[/]");
    foreach (var tip in AgentBranding.Tips)
    {
        AnsiConsole.MarkupLine($"[grey70]- {Markup.Escape(tip)}[/]");
    }

    AnsiConsole.WriteLine();
}

static async Task RespondAsync(IAgentConversation agent, string input)
{
    AnsiConsole.MarkupLine($"[bold green]> you[/]  {Markup.Escape(input)}");

    var state = new ResponseState();
    var stopwatch = Stopwatch.StartNew();

    // 出力がリダイレクトされている場合は Live (カーソル制御) が使えないため、
    // 全イベントを集約してから最終ビューを 1 回だけ描画する。
    if (Console.IsOutputRedirected)
    {
        await foreach (var ev in agent.SendAsync(input))
        {
            Apply(state, ev);
        }

        AnsiConsole.Write(SpectreChatView.Build(agent.AgentName, state, stopwatch));
        AnsiConsole.WriteLine();
        return;
    }

    await AnsiConsole.Live(new Markup(string.Empty))
        .AutoClear(false)
        .StartAsync(async ctx =>
        {
            await foreach (var ev in agent.SendAsync(input))
            {
                Apply(state, ev);
                ctx.UpdateTarget(SpectreChatView.Build(agent.AgentName, state, stopwatch));
                ctx.Refresh();
            }
        });

    AnsiConsole.WriteLine();
}

static void Apply(ResponseState state, AgentEvent ev)
{
    switch (ev)
    {
        case ThinkingStarted:
            state.Thinking = true;
            break;
        case ThinkingDelta delta:
            state.LastThought = delta.Text;
            state.SpinnerIndex++;
            break;
        case ThinkingCompleted:
            state.Thinking = false;
            break;
        case ToolCallStarted started:
            state.Tools.Add(new ToolCard(started.Name, started.Arguments));
            break;
        case ToolCallCompleted completed:
            state.Tools[^1] = state.Tools[^1] with { Result = completed.Result };
            break;
        case TextDelta text:
            state.Answer.Append(text.Text);
            state.Tokens++;
            break;
        case ResponseCompleted:
            state.Completed = true;
            break;
        default:
            break;
    }
}

internal sealed record ToolCard(string Name, string Arguments)
{
    public string? Result { get; init; }
}

internal sealed class ResponseState
{
    public bool Thinking { get; set; }

    public bool Completed { get; set; }

    public string LastThought { get; set; } = string.Empty;

    public int SpinnerIndex { get; set; }

    public int Tokens { get; set; }

    public List<ToolCard> Tools { get; } = [];

    public StringBuilder Answer { get; } = new();
}

internal static class SpectreChatView
{
    private const string SpinnerFrames = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏";

    public static IRenderable Build(string agentName, ResponseState state, Stopwatch stopwatch)
    {
        var rows = new List<IRenderable>();

        if (state.Thinking)
        {
            var frame = SpinnerFrames[state.SpinnerIndex % SpinnerFrames.Length];
            rows.Add(new Markup($"[yellow]{frame} thinking...[/]  [grey70]{Markup.Escape(state.LastThought)}[/]"));
        }

        // 枠 (Panel) は付けず、役割を色付きの見出し行で示す。
        foreach (var tool in state.Tools)
        {
            rows.Add(new Markup($"[blue]> {Markup.Escape(tool.Name)}[/]  [grey70]{Markup.Escape(tool.Arguments)}[/]"));
            var result = tool.Result ?? "running...";
            foreach (var line in result.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                rows.Add(new Markup($"  [grey70]{Markup.Escape(line)}[/]"));
            }
        }

        if (state.Answer.Length > 0 || state.Completed)
        {
            rows.Add(new Markup($"[aqua]> {Markup.Escape(agentName)}[/]"));
            // 本文 (応答の文章) は白。実ターミナルでは bold + 明るい白が背景と同化するため
            // useBold:false で素の白にし、記号類は本文と分かるよう明るめのグレーにする。
            var content = state.Completed
                ? MarkupFormatter.ToConsoleMarkup(state.Answer.ToString(), useBold: false, mutedColor: "grey70")
                : $"[white]{Markup.Escape(state.Answer.ToString())}[/]";
            rows.Add(new Markup(content));
        }

        if (state.Completed)
        {
            rows.Add(new Markup($"[grey70]{state.Tokens} tokens, {stopwatch.ElapsedMilliseconds} ms[/]"));
        }

        return new Rows(rows);
    }
}
