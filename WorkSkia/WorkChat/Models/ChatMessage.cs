namespace WorkChat.Models;

using System.Collections.Generic;

public sealed class ChatMessage
{
    public MessageType Type { get; set; }

    public DateTime DateTime { get; init; } = DateTime.Now;

    public string Author { get; set; } = default!;

    public string TextContent { get; set; } = default!;

    public string? AvatarSource { get; set; }

    public string? StampSource { get; set; }

    public bool IsRead { get; set; }

    public IReadOnlyList<MessageReaction> Reactions { get; set; } = [];

    public bool IsStamp => !string.IsNullOrEmpty(StampSource);

    public bool HasText => !IsStamp && !string.IsNullOrEmpty(TextContent);

    public bool HasReactions => Reactions.Count > 0;

    public bool ShowReadIndicator => Type == MessageType.Send && IsRead;

    public string TimeText => DateTime.ToString("HH:mm");
}
