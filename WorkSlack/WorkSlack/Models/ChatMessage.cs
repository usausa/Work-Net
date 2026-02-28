namespace WorkSlack.Models;

public sealed class ChatMessage
{
    public required string Id { get; init; }
    public required string ChannelId { get; init; }
    public required string AuthorId { get; init; }
    public string Content { get; set; } = "";
    public required DateTimeOffset Timestamp { get; init; }
    public List<Reaction> Reactions { get; init; } = [];
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsPinned { get; set; }
    public string? ParentMessageId { get; init; }
    public int ReplyCount { get; set; }
}

public record Reaction(string Emoji, List<string> UserIds);
