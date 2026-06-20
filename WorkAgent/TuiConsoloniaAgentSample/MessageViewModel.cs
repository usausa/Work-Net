namespace TuiConsoloniaAgentSample;

using Avalonia.Media;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// 1 メッセージブロック。ヘッダ/本文と、種別に応じた表示色 (ブラシ) を持つ。
/// </summary>
internal sealed partial class MessageViewModel : ObservableObject
{
    [ObservableProperty]
    private string body;

    public MessageViewModel(string header, string initialBody, IBrush headerBrush, IBrush bodyBrush)
    {
        Header = header;
        body = initialBody;
        HeaderBrush = headerBrush;
        BodyBrush = bodyBrush;
    }

    public string Header { get; }

    public IBrush HeaderBrush { get; }

    public IBrush BodyBrush { get; }
}
