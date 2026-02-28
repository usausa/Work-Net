namespace WorkSlack.Models;

public record Channel(
    string Id,
    string Name,
    string? Description = null,
    bool IsDirectMessage = false,
    string? DmUserId = null);
