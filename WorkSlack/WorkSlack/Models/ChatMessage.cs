namespace WorkSlack.Models;

public sealed class ChatMessage
{
    public required string Id { get; init; }
    public required string ChannelId { get; init; }
    public required string AuthorId { get; init; }
    public required string Content { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public List<Reaction> Reactions { get; init; } = [];
    public bool IsEdited { get; init; }
}

public record Reaction(string Emoji, List<string> UserIds);
