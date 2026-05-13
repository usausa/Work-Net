namespace WorkChat.ViewModels;

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WorkChat.Models;

public sealed partial class MainPageViewModel : ObservableObject
{
    private string inputText = string.Empty;

    public ObservableCollection<ChatMessage> Messages { get; } = [];

    public string CurrentUser { get; } = "Me";

    public string InputText
    {
        get => inputText;
        set
        {
            if (SetProperty(ref inputText, value))
            {
                SendCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public MainPageViewModel()
    {
        LoadSampleMessages();
    }

    private bool CanSend() => !string.IsNullOrWhiteSpace(InputText);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private void Send()
    {
        Messages.Add(new ChatMessage
        {
            Type = MessageType.Send,
            Author = CurrentUser,
            AvatarColor = Color.FromArgb("#4CAF50"),
            TextContent = InputText.Trim()
        });

        InputText = string.Empty;
    }

    private void LoadSampleMessages()
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        Messages.Add(new ChatMessage
        {
            Type = MessageType.System,
            DateTime = yesterday,
            TextContent = yesterday.ToString("yyyy年M月d日")
        });

        Messages.Add(new ChatMessage
        {
            Type = MessageType.Receive,
            DateTime = yesterday.AddHours(9).AddMinutes(15),
            Author = "Alice",
            AvatarColor = Color.FromArgb("#E91E63"),
            TextContent = "おはようございます！今日もよろしくお願いします。"
        });

        Messages.Add(new ChatMessage
        {
            Type = MessageType.Send,
            DateTime = yesterday.AddHours(9).AddMinutes(18),
            Author = CurrentUser,
            AvatarColor = Color.FromArgb("#4CAF50"),
            TextContent = "おはようございます！こちらこそ。",
            Reactions =
            [
                new MessageReaction { Emoji = "👍", Count = 2 },
                new MessageReaction { Emoji = "🎉", Count = 1 }
            ]
        });

        Messages.Add(new ChatMessage
        {
            Type = MessageType.Receive,
            DateTime = yesterday.AddHours(12).AddMinutes(30),
            Author = "Bob",
            AvatarColor = Color.FromArgb("#2196F3"),
            TextContent = "お昼ご飯食べてきます〜",
            Reactions =
            [
                new MessageReaction { Emoji = "🍱", Count = 3 }
            ]
        });

        Messages.Add(new ChatMessage
        {
            Type = MessageType.System,
            DateTime = today,
            TextContent = today.ToString("yyyy年M月d日")
        });

        Messages.Add(new ChatMessage
        {
            Type = MessageType.Receive,
            DateTime = today.AddHours(10).AddMinutes(5),
            Author = "Alice",
            AvatarColor = Color.FromArgb("#E91E63"),
            TextContent = "資料できましたー！確認お願いします。"
        });

        Messages.Add(new ChatMessage
        {
            Type = MessageType.Send,
            DateTime = today.AddHours(10).AddMinutes(7),
            Author = CurrentUser,
            AvatarColor = Color.FromArgb("#4CAF50"),
            TextContent = "ありがとうございます！見ますね。"
        });

        Messages.Add(new ChatMessage
        {
            Type = MessageType.Receive,
            DateTime = today.AddHours(10).AddMinutes(10),
            Author = "Bob",
            AvatarColor = Color.FromArgb("#2196F3"),
            StampSource = "dotnet_bot.png",
            TextContent = string.Empty
        });

        Messages.Add(new ChatMessage
        {
            Type = MessageType.Send,
            DateTime = today.AddHours(10).AddMinutes(12),
            Author = CurrentUser,
            AvatarColor = Color.FromArgb("#4CAF50"),
            TextContent = "👀 確認中…",
            Reactions =
            [
                new MessageReaction { Emoji = "👀", Count = 1 }
            ]
        });
    }
}
