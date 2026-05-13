namespace WorkChat.Models;

public sealed class MessageReaction
{
    public string Emoji { get; set; } = default!;

    public int Count { get; set; }
}
