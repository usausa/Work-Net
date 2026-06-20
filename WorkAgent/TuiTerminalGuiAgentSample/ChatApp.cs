namespace TuiTerminalGuiAgentSample;

using System.Drawing;
using System.Text;

using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

using TuiAgentSampleCore;

/// <summary>
/// Terminal.Gui v2 (instance API) で組んだエージェントチャット画面。
/// ロゴバナー + スクロール可能な色分けトランスクリプト + 入力欄 + ステータス行。
/// </summary>
internal sealed class ChatApp : IDisposable
{
    private const string SpinnerFrames = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏";

    private readonly IAgentConversation agent;
    private readonly StringBuilder assistantText = new();
    private readonly List<Label> blocks = [];

    private IApplication application = null!;
    private Window window = null!;
    private FrameView banner = null!;
    private Label logo = null!;
    private Label subtitle = null!;
    private FrameView conversation = null!;
    private Label prompt = null!;
    private TextField input = null!;
    private Label status = null!;
    private Label? assistantBlock;
    private bool thinking;
    private int spinnerIndex;
    private int nextY;
    private int lastLines;
    private string lastThought = string.Empty;

    public ChatApp()
    {
        // 実エージェントへ差し替える場合はこの戻り値を別実装に変えるだけでよい。
        agent = CreateAgent();
        static IAgentConversation CreateAgent() => new SimulatedAgentConversation();
    }

    public int Run()
    {
        application = Application.Create();
        application.Init();
        window = BuildWindow();
        application.AddTimeout(TimeSpan.FromMilliseconds(120), OnSpinnerTick);
        application.Run(window);
        return 0;
    }

    public void Dispose()
    {
        // View は標準の Dispose パターン (二重呼び出しは no-op) に従うため、
        // 子ビューを個別に破棄しても window のツリー破棄と二重にならない。
        logo?.Dispose();
        subtitle?.Dispose();
        banner?.Dispose();
        prompt?.Dispose();
        input?.Dispose();
        status?.Dispose();
        conversation?.Dispose();
        window?.Dispose();
        application?.Dispose();
        GC.SuppressFinalize(this);
    }

    private Window BuildWindow()
    {
        var shell = new Window
        {
            Title = $" {AgentBranding.Title} ",
            BorderStyle = LineStyle.Rounded
        };
        shell.SetScheme(ChatTheme.Window);

        banner = BuildBanner();
        shell.Add(banner);

        conversation = new FrameView
        {
            Title = " conversation ",
            X = 0,
            Y = Pos.Bottom(banner),
            Width = Dim.Fill(),
            Height = Dim.Fill(2),
            BorderStyle = LineStyle.Rounded
        };
        conversation.SetScheme(ChatTheme.Window);
        conversation.VerticalScrollBar.Visible = true;
        shell.Add(conversation);

        prompt = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(2),
            Text = "you >"
        };
        prompt.SetScheme(ChatTheme.User);

        input = new TextField
        {
            X = Pos.Right(prompt) + 1,
            Y = Pos.AnchorEnd(2),
            Width = Dim.Fill(1)
        };
        input.Accepting += OnAccepting;

        status = new Label
        {
            X = 1,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(1),
            Text = ReadyText()
        };
        status.SetScheme(ChatTheme.Ready);

        shell.Add(prompt, input, status);

        // レイアウト確定後 (幅が決まってから) に起動メッセージを表示する。
        shell.Initialized += OnShellInitialized;
        return shell;
    }

    private FrameView BuildBanner()
    {
        var frame = new FrameView
        {
            Title = " welcome ",
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = AgentBranding.LogoLines.Count + 4,
            BorderStyle = LineStyle.Rounded
        };
        frame.SetScheme(ChatTheme.Window);

        logo = new Label
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = AgentBranding.LogoLines.Count,
            Text = string.Join('\n', AgentBranding.LogoLines)
        };
        logo.SetScheme(ChatTheme.Logo);

        subtitle = new Label
        {
            X = 1,
            Y = Pos.Bottom(logo),
            Width = Dim.Fill(1),
            Text = AgentBranding.Tagline
        };
        subtitle.SetScheme(ChatTheme.Title);

        frame.Add(logo, subtitle);
        return frame;
    }

    private void OnShellInitialized(object? sender, EventArgs e)
    {
        CreateBlock($"> {agent.AgentName}", ChatTheme.Assistant);
        foreach (var tip in AgentBranding.Tips)
        {
            CreateBlock($"  - {tip}", ChatTheme.ToolResult);
        }

        input.SetFocus();
    }

    private void OnAccepting(object? sender, CommandEventArgs e)
    {
        e.Handled = true;
        var text = input.Text.Trim();
        if (text.Length == 0)
        {
            return;
        }

        if (text is "/exit" or "exit")
        {
            application.RequestStop(window);
            return;
        }

        input.Text = string.Empty;

        if (text is "/clear")
        {
            ClearTranscript();
            return;
        }

        CreateBlock($"> you  {text}", ChatTheme.User);
        StartResponse(text);
    }

    private void StartResponse(string text)
    {
        input.Enabled = false;
        assistantBlock = null;
        assistantText.Clear();
        thinking = false;
        _ = Task.Run(() => StreamAsync(text));
    }

    private async Task StreamAsync(string text)
    {
        await foreach (var ev in agent.SendAsync(text))
        {
            var captured = ev;
            application.Invoke(() => Apply(captured));
        }
    }

    private void Apply(AgentEvent ev)
    {
        switch (ev)
        {
            case ThinkingStarted:
                thinking = true;
                status.SetScheme(ChatTheme.Thinking);
                break;
            case ThinkingDelta delta:
                lastThought = delta.Text;
                break;
            case ThinkingCompleted:
                thinking = false;
                status.Text = "generating...";
                break;
            case ToolCallStarted started:
                CreateBlock($"> {started.Name}({started.Arguments})", ChatTheme.ToolHeader);
                break;
            case ToolCallCompleted completed:
                CreateBlock(completed.Result, ChatTheme.ToolResult);
                break;
            case TextDelta textDelta:
                AppendAssistant(textDelta.Text);
                break;
            case ResponseCompleted:
                FinishResponse();
                break;
            default:
                break;
        }
    }

    private void AppendAssistant(string token)
    {
        if (assistantBlock is null)
        {
            CreateBlock($"> {agent.AgentName}", ChatTheme.Assistant);   // 見出し (シアン)
            assistantText.Clear();
            CreateBlock(string.Empty, ChatTheme.AssistantBody);          // 本文 (白)
            assistantBlock = blocks[^1];
        }

        assistantText.Append(token);
        UpdateStreaming();
    }

    private void FinishResponse()
    {
        thinking = false;
        status.Text = ReadyText();
        status.SetScheme(ChatTheme.Ready);
        input.Enabled = true;
        input.SetFocus();
        assistantBlock = null;
    }

    private bool OnSpinnerTick()
    {
        if (thinking)
        {
            spinnerIndex++;
            status.Text = $"{SpinnerFrames[spinnerIndex % SpinnerFrames.Length]} thinking...  {lastThought}";
            status.SetNeedsDraw();
        }

        return true;
    }

    private string ReadyText() =>
        $"model {agent.ModelName}   |   simulated   |   Enter=send   /clear   /exit";

    // --- トランスクリプト管理 (色分けした Label を縦に積み、自動スクロール) -----------

    private void CreateBlock(string text, Scheme scheme)
    {
        var width = ContentWidth();
        var wrapped = TextWrap.Wrap(text, width);
        var label = new Label
        {
            X = 0,
            Y = nextY,
            Width = width,
            Height = wrapped.Count,
            Text = string.Join('\n', wrapped)
        };
        label.SetScheme(scheme);
        conversation.Add(label);
        blocks.Add(label);

        nextY += wrapped.Count + 1;
        lastLines = wrapped.Count;
        UpdateScroll(width);
    }

    private void UpdateStreaming()
    {
        if (assistantBlock is null)
        {
            return;
        }

        var width = ContentWidth();
        var wrapped = TextWrap.Wrap(assistantText.ToString(), width);

        // 末尾ブロック (ストリーミング中) のみ高さが変化する前提で nextY を補正する。
        nextY -= lastLines + 1;
        assistantBlock.Text = string.Join('\n', wrapped);
        assistantBlock.Height = wrapped.Count;
        nextY += wrapped.Count + 1;
        lastLines = wrapped.Count;
        UpdateScroll(width);
    }

    private void ClearTranscript()
    {
        foreach (var block in blocks)
        {
            conversation.Remove(block);
            block.Dispose();
        }

        blocks.Clear();
        assistantBlock = null;
        nextY = 0;
        lastLines = 0;
        conversation.SetContentSize(new Size(ContentWidth(), 0));
        conversation.SetNeedsDraw();
    }

    private int ContentWidth() => Math.Max(20, conversation.Viewport.Width);

    private void UpdateScroll(int width)
    {
        conversation.SetContentSize(new Size(width, Math.Max(nextY, conversation.Viewport.Height)));
        var maxY = Math.Max(0, nextY - conversation.Viewport.Height);
        conversation.Viewport = conversation.Viewport with { Y = maxY };
        conversation.SetNeedsDraw();
    }
}
