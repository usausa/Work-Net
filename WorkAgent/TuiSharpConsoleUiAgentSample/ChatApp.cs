namespace TuiSharpConsoleUiAgentSample;

using System.Collections.Concurrent;
using System.Text;

using SharpConsoleUI;
using SharpConsoleUI.Builders;
using SharpConsoleUI.Controls;
using SharpConsoleUI.Drivers;

using TuiAgentSampleCore;

/// <summary>
/// SharpConsoleUI のマルチウィンドウ機能を活かしたエージェントチャット。
/// 左に会話ウィンドウ、右にアクティビティ(思考/ツール)ウィンドウを並べる。
/// </summary>
internal sealed class ChatApp : IDisposable
{
    private const string SpinnerFrames = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏";

    // 既定テーマだと背景と灰文字が同化するため、暗背景を明示する。
    // Color(string) は 16 進のみ受け付ける (色名は不可) ため #RRGGBB で指定する。
    private static readonly Color Foreground = new("#D4D4D4");
    private static readonly Color Background = new("#0F1117");
    private static readonly Color BorderColor = new("#4C6FA5");

    private readonly IAgentConversation agent;
    private readonly List<string> history = [];
    private readonly StringBuilder assistantBuffer = new();
    private readonly ConcurrentQueue<string> activityQueue = new();
    private readonly List<string> activityLog = [];

    private ConsoleWindowSystem windowSystem = null!;
    private MarkupControl header = null!;
    private MarkupControl transcript = null!;
    private MarkupControl activity = null!;
    private PromptControl input = null!;
    private volatile bool thinking;
    private string lastThought = string.Empty;
    private int spinnerIndex;

    public ChatApp()
    {
        // 実エージェントへ差し替える場合はこの戻り値を別実装に変えるだけでよい。
        agent = CreateAgent();
        static IAgentConversation CreateAgent() => new SimulatedAgentConversation();
    }

    public int Run()
    {
        windowSystem = new ConsoleWindowSystem(new NetConsoleDriver(RenderMode.Buffer));
        BuildChatWindow();
        BuildActivityWindow();
        ResetTranscript();
        return windowSystem.Run();
    }

    public void Dispose()
    {
        header?.Dispose();
        transcript?.Dispose();
        activity?.Dispose();
        input?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void BuildChatWindow()
    {
        var headerLines = new List<string>();
        foreach (var line in AgentBranding.LogoLines)
        {
            headerLines.Add($"[bold cyan]{line}[/]");
        }

        headerLines.Add($"[silver]{AgentBranding.Tagline}[/]");

        header = new MarkupControl(headerLines);
        transcript = new MarkupControl([]);
        input = Controls.Prompt("input")
            .WithPrompt("you > ")
            .OnEntered(new EventHandler<string>(OnInput))
            .UnfocusOnEnter(false)
            .StickyBottom()
            .Build();

        var chatWindow = new WindowBuilder(windowSystem)
            .WithTitle($" {AgentBranding.Title} ")
            .WithBounds(1, 1, 84, 34)
            .WithColors(Foreground, Background)
            .WithBorderColor(BorderColor)
            .AddControl(header)
            .AddControl(transcript)
            .AddControl(input)
            .Build();
        windowSystem.AddWindow(chatWindow);
    }

    private void BuildActivityWindow()
    {
        activity = new MarkupControl(["[silver]idle[/]"]);
        var activityWindow = new WindowBuilder(windowSystem)
            .WithTitle(" activity ")
            .WithBounds(86, 1, 40, 34)
            .WithColors(Foreground, Background)
            .WithBorderColor(BorderColor)
            .AddControl(activity)
            .WithAsyncWindowThread(ActivityLoopAsync)
            .Build();
        windowSystem.AddWindow(activityWindow);
    }

    private void OnInput(object? sender, string text)
    {
        var trimmed = text.Trim();
        input.Input = string.Empty;
        if (trimmed.Length == 0)
        {
            return;
        }

        if (trimmed is "/exit" or "exit")
        {
            windowSystem.Shutdown(0);
            return;
        }

        if (trimmed is "/clear")
        {
            ResetTranscript();
            return;
        }

        history.Add($"[bold green]> you[/]  {MarkupFormatter.Escape(trimmed)}");
        history.Add(string.Empty);
        RenderTranscript();
        _ = Task.Run(() => StreamAsync(trimmed));
    }

    private async Task StreamAsync(string text)
    {
        thinking = false;
        assistantBuffer.Clear();

        await foreach (var ev in agent.SendAsync(text))
        {
            switch (ev)
            {
                case ThinkingStarted:
                    thinking = true;
                    break;
                case ThinkingDelta delta:
                    lastThought = delta.Text;
                    break;
                case ThinkingCompleted:
                    thinking = false;
                    break;
                case ToolCallStarted started:
                    activityQueue.Enqueue($"[blue]> {MarkupFormatter.Escape(started.Name)}[/] [silver]{MarkupFormatter.Escape(started.Arguments)}[/]");
                    break;
                case ToolCallCompleted completed:
                    activityQueue.Enqueue($"[green]+ {MarkupFormatter.Escape(completed.Name)}[/]");
                    AppendToolBlock(completed.Name, completed.Result);
                    break;
                case TextDelta textDelta:
                    assistantBuffer.Append(textDelta.Text);
                    var snapshot = assistantBuffer.ToString();
                    windowSystem.EnqueueOnUIThread(() => RenderTranscript(snapshot));
                    break;
                case ResponseCompleted:
                    var finalBody = assistantBuffer.ToString();
                    windowSystem.EnqueueOnUIThread(() => FinalizeAssistant(finalBody));
                    break;
                default:
                    break;
            }
        }
    }

    private async Task ActivityLoopAsync(Window window, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            while (activityQueue.TryDequeue(out var line))
            {
                activityLog.Add(line);
                if (activityLog.Count > 40)
                {
                    activityLog.RemoveAt(0);
                }
            }

            var display = new List<string>();
            if (thinking)
            {
                spinnerIndex++;
                display.Add($"[yellow]{SpinnerFrames[spinnerIndex % SpinnerFrames.Length]} thinking...[/]");
                var thought = lastThought;
                if (thought.Length > 0)
                {
                    display.Add($"[silver]{MarkupFormatter.Escape(thought)}[/]");
                }

                display.Add(string.Empty);
            }

            display.AddRange(activityLog);
            if (display.Count == 0)
            {
                display.Add("[silver]idle[/]");
            }

            activity.SetContent(display);
            await Task.Delay(120, cancellationToken).ConfigureAwait(false);
        }
    }

    private void AppendToolBlock(string name, string result)
    {
        windowSystem.EnqueueOnUIThread(() =>
        {
            history.Add($"[blue]> {MarkupFormatter.Escape(name)}[/]");
            foreach (var line in result.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
            {
                history.Add($"[silver]{MarkupFormatter.Escape(line)}[/]");
            }

            history.Add(string.Empty);
            RenderTranscript();
        });
    }

    private void FinalizeAssistant(string body)
    {
        history.Add($"[aqua]> {MarkupFormatter.Escape(agent.AgentName)}[/]");
        foreach (var line in MarkupFormatter.ToConsoleMarkup(body).Split('\n'))
        {
            history.Add(line);
        }

        history.Add(string.Empty);
        RenderTranscript();
    }

    private void ResetTranscript()
    {
        history.Clear();
        history.Add($"[aqua]> {MarkupFormatter.Escape(agent.AgentName)}[/]");
        foreach (var tip in AgentBranding.Tips)
        {
            history.Add($"[silver]- {MarkupFormatter.Escape(tip)}[/]");
        }

        history.Add(string.Empty);
        RenderTranscript();
    }

    private void RenderTranscript(string? streamingBody = null)
    {
        var display = new List<string>(history);
        if (streamingBody is not null)
        {
            display.Add($"[aqua]> {MarkupFormatter.Escape(agent.AgentName)}[/]");
            display.AddRange(MarkupFormatter.ToConsoleMarkup(streamingBody).Split('\n'));
        }

        transcript.SetContent(display);
    }
}
