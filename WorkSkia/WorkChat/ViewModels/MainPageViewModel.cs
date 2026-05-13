namespace WorkChat.ViewModels;

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WorkChat.Models;

public sealed partial class MainPageViewModel : ObservableObject
{
    private const string AvatarAlice = "avatar_alice.png";
    private const string AvatarBob = "avatar_bob.png";
    private const string AvatarCarol = "avatar_carol.png";
    private const string AvatarDave = "avatar_dave.png";
    private const string AvatarMe = "avatar_me.png";

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
            AvatarSource = AvatarMe,
            TextContent = InputText.Trim()
        });

        InputText = string.Empty;
    }

    [RelayCommand]
    private static void PickImage()
    {
        // Camera/gallery picker placeholder
    }

    [RelayCommand]
    private static void PickSticker()
    {
        // Sticker picker placeholder
    }

    private void LoadSampleMessages()
    {
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);
        var twoDaysAgo = today.AddDays(-2);

        // ---- Two days ago ----
        AddSystem(twoDaysAgo);

        AddReceive(twoDaysAgo.AddHours(9), "Alice", AvatarAlice,
            "おはようございます。今日の進捗どうですか？");
        AddSend(twoDaysAgo.AddHours(9).AddMinutes(5),
            "おはようございます。順調です。今日中に PR 出します。", isRead: true);
        AddReceive(twoDaysAgo.AddHours(9).AddMinutes(8), "Bob", AvatarBob,
            "お疲れさまです。レビュー入りましたらお願いします。");
        AddSend(twoDaysAgo.AddHours(9).AddMinutes(10), "了解です！", isRead: true);
        AddReceive(twoDaysAgo.AddHours(11).AddMinutes(30), "Carol", AvatarCarol,
            "今日の定例は 14:00 からです。");
        AddSend(twoDaysAgo.AddHours(11).AddMinutes(32),
            "ありがとうございます。", isRead: true);
        AddReceive(twoDaysAgo.AddHours(12), "Alice", AvatarAlice,
            "お昼ご飯どこ行きます？");
        AddSend(twoDaysAgo.AddHours(12).AddMinutes(5),
            "近くのカフェにしようかと。", isRead: true,
            reactions: [new MessageReaction { Emoji = "👍", Count = 2 }]);
        AddReceive(twoDaysAgo.AddHours(12).AddMinutes(7), "Bob", AvatarBob,
            "ご一緒します！");
        AddSend(twoDaysAgo.AddHours(17).AddMinutes(45),
            "明日の予定です：\n10:00 朝会\n14:00 定例\n16:00 1on1", isRead: true);
        AddReceive(twoDaysAgo.AddHours(18), "Dave", AvatarDave,
            "今日もお疲れさまでした！");

        // ---- Yesterday ----
        AddSystem(yesterday);

        AddReceive(yesterday.AddHours(9).AddMinutes(15), "Alice", AvatarAlice,
            "おはようございます。");
        AddSend(yesterday.AddHours(9).AddMinutes(18),
            "おはようございます。", isRead: true);
        AddReceive(yesterday.AddHours(9).AddMinutes(30), "Bob", AvatarBob,
            "昨日の PR レビューしました。CI が通っていないようなのでテストの修正をお願いできますか？コメントもいくつか書いてあります。");
        AddSend(yesterday.AddHours(9).AddMinutes(32),
            "ありがとうございます！\n午前中に対応します。", isRead: true);
        AddReceive(yesterday.AddHours(12).AddMinutes(30), "Bob", AvatarBob,
            "お昼ご飯食べてきます〜",
            reactions: [new MessageReaction { Emoji = "🍱", Count = 3 }]);
        AddReceive(yesterday.AddHours(14), "Carol", AvatarCarol,
            "定例始めます。");
        AddSend(yesterday.AddHours(14).AddMinutes(1),
            "入ります。", isRead: true);
        AddReceive(yesterday.AddHours(16), "Alice", AvatarAlice,
            "資料 PDF 共有しますね。");
        AddSend(yesterday.AddHours(16).AddMinutes(5),
            "確認しました！", isRead: true,
            reactions: [new MessageReaction { Emoji = "🙏", Count = 1 }]);
        AddReceive(yesterday.AddHours(18).AddMinutes(30), "Dave", AvatarDave,
            "お疲れさまでした！");

        // ---- Today ----
        AddSystem(today);

        AddReceive(today.AddHours(10).AddMinutes(5), "Alice", AvatarAlice,
            "資料できましたー！来週の会議で使うものなので、月曜日までに確認をお願いします🙏");
        AddSend(today.AddHours(10).AddMinutes(7),
            "了解しました！\n以下の点を確認します。\n・議事録\n・来週の資料\n・レビュー", isRead: true);
        AddReceiveStamp(today.AddHours(10).AddMinutes(10), "Bob", AvatarBob,
            "dotnet_bot.png");
        AddSend(today.AddHours(10).AddMinutes(12),
            "👀 確認中…", isRead: true,
            reactions: [new MessageReaction { Emoji = "👀", Count = 1 }]);
        AddReceive(today.AddHours(10).AddMinutes(30), "Carol", AvatarCarol,
            "今日は 15:00 から会議です。");
        AddSend(today.AddHours(10).AddMinutes(32),
            "了解しました。", isRead: true);
        AddReceive(today.AddHours(11), "Alice", AvatarAlice,
            "ランチ何にします？");
        AddReceive(today.AddHours(11).AddMinutes(2), "Bob", AvatarBob,
            "寿司でどうでしょう。",
            reactions: [new MessageReaction { Emoji = "🍣", Count = 2 }]);
        AddSend(today.AddHours(11).AddMinutes(5),
            "いいですね！ちなみに本日のミーティングお疲れさまでした。共有いただいた資料についていくつか質問があるので、後ほど別途連絡いたします。",
            isRead: false);
    }

    private void AddSystem(DateTime date) =>
        Messages.Add(new ChatMessage
        {
            Type = MessageType.System,
            DateTime = date,
            TextContent = date.ToString("yyyy年M月d日 (ddd)")
        });

    private void AddReceive(
        DateTime dateTime,
        string author,
        string avatar,
        string text,
        IReadOnlyList<MessageReaction>? reactions = null) =>
        Messages.Add(new ChatMessage
        {
            Type = MessageType.Receive,
            DateTime = dateTime,
            Author = author,
            AvatarSource = avatar,
            TextContent = text,
            Reactions = reactions ?? []
        });

    private void AddReceiveStamp(
        DateTime dateTime,
        string author,
        string avatar,
        string stampSource) =>
        Messages.Add(new ChatMessage
        {
            Type = MessageType.Receive,
            DateTime = dateTime,
            Author = author,
            AvatarSource = avatar,
            StampSource = stampSource,
            TextContent = string.Empty
        });

    private void AddSend(
        DateTime dateTime,
        string text,
        bool isRead,
        IReadOnlyList<MessageReaction>? reactions = null) =>
        Messages.Add(new ChatMessage
        {
            Type = MessageType.Send,
            DateTime = dateTime,
            Author = CurrentUser,
            AvatarSource = AvatarMe,
            TextContent = text,
            IsRead = isRead,
            Reactions = reactions ?? []
        });
}
