namespace WorkSlack.Models;

public record ChatUser(
    string Id,
    string DisplayName,
    string AvatarColor,
    UserStatus Status = UserStatus.Online);

public enum UserStatus
{
    Online,
    Away,
    DoNotDisturb,
    Offline
}
