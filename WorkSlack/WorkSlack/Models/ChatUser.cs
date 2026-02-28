namespace WorkSlack.Models;

public record ChatUser(
    string Id,
    string DisplayName,
    string AvatarColor,
    UserStatus Status = UserStatus.Online,
    string? CustomStatusEmoji = null,
    string? CustomStatusText = null);

public enum UserStatus
{
    Online,
    Away,
    DoNotDisturb,
    Offline
}
