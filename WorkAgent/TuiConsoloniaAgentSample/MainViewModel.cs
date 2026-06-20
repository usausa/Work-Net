namespace TuiConsoloniaAgentSample;

using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using TuiAgentSampleCore;

/// <summary>
/// チャット画面の ViewModel。擬似エージェントの応答イベントを購読し、
/// 役割ごとに配色した <see cref="MessageViewModel"/> を追加・更新する。
/// </summary>
internal sealed partial class MainViewModel : ObservableObject
{
    private readonly IAgentConversation agent;

    [ObservableProperty]
    private string input = string.Empty;

    [ObservableProperty]
    private string status = string.Empty;

    public MainViewModel()
    {
        // 実エージェントへ差し替える場合はこの戻り値を別実装に変えるだけでよい。
        agent = CreateAgent();
        static IAgentConversation CreateAgent() => new SimulatedAgentConversation();

        Logo = string.Join('\n', AgentBranding.LogoLines);
        Tagline = AgentBranding.Tagline;
        status = ReadyStatus();
        AddGreeting();
    }

    public ObservableCollection<MessageViewModel> Messages { get; } = [];

    public string Logo { get; }

    public string Tagline { get; }

    [RelayCommand]
    private async Task SendAsync()
    {
        var text = Input.Trim();
        Input = string.Empty;
        if (text.Length == 0)
        {
            return;
        }

        if (text is "/exit" or "exit")
        {
            Shutdown();
            return;
        }

        if (text is "/clear")
        {
            Messages.Clear();
            AddGreeting();
            return;
        }

        Messages.Add(new MessageViewModel("> you", text, Brushes.LimeGreen, Brushes.White));
        Status = "thinking...";

        MessageViewModel? assistant = null;
        var raw = new StringBuilder();

        await foreach (var ev in agent.SendAsync(text))
        {
            switch (ev)
            {
                case ThinkingStarted:
                    Status = "thinking...";
                    break;
                case ThinkingDelta delta:
                    Status = $"thinking...  {delta.Text}";
                    break;
                case ThinkingCompleted:
                    Status = "generating...";
                    break;
                case ToolCallStarted started:
                    Messages.Add(new MessageViewModel($"> {started.Name}", started.Arguments, Brushes.DeepSkyBlue, Brushes.Silver));
                    break;
                case ToolCallCompleted completed:
                    Messages.Add(new MessageViewModel($"> {completed.Name} -> result", completed.Result, Brushes.DeepSkyBlue, Brushes.Silver));
                    break;
                case TextDelta textDelta:
                    assistant ??= AddAssistant();
                    raw.Append(textDelta.Text);
                    assistant.Body = MarkupFormatter.ToPlainText(raw.ToString());
                    break;
                case ResponseCompleted:
                    Status = ReadyStatus();
                    break;
                default:
                    break;
            }
        }
    }

    private MessageViewModel AddAssistant()
    {
        var assistant = new MessageViewModel($"> {agent.AgentName}", string.Empty, Brushes.Cyan, Brushes.White);
        Messages.Add(assistant);
        return assistant;
    }

    private void AddGreeting() =>
        Messages.Add(new MessageViewModel(
            $"> {agent.AgentName}",
            string.Join('\n', AgentBranding.Tips),
            Brushes.Cyan,
            Brushes.Silver));

    private string ReadyStatus() =>
        $"model {agent.ModelName}   |   simulated   |   Enter=send   /clear   /exit";

    private static void Shutdown()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
