namespace WorkChat.Models;

using System.Collections.Generic;

public sealed class ChatMessage
{
    public MessageType Type { get; set; }

    public DateTime DateTime { get; init; } = DateTime.Now;

    public string Author { get; set; } = default!;

    public string TextContent { get; set; } = default!;

    public string? AvatarSource { get; set; }

    public Color? AvatarColor { get; set; }

    public string? StampSource { get; set; }

    public IReadOnlyList<MessageReaction> Reactions { get; set; } = [];

    public bool IsStamp => !string.IsNullOrEmpty(StampSource);

    public bool HasText => !IsStamp && !string.IsNullOrEmpty(TextContent);

    public bool HasReactions => Reactions.Count > 0;

    public string TimeText => DateTime.ToString("HH:mm");

    public string AvatarInitial =>
        string.IsNullOrEmpty(Author) ? "?" : Author[..1].ToUpperInvariant();
}
