using WorkSlack.Models;

namespace WorkSlack.Services;

/// <summary>
/// Provides in-memory chat data operations for channels, users, and messages.
/// </summary>
public interface IChatService
{
    IReadOnlyList<Channel> GetChannels();
    IReadOnlyList<Channel> GetDirectMessages();
    IReadOnlyList<ChatMessage> GetMessages(string channelId);
    Channel? GetChannel(string channelId);
    ChatUser? GetUser(string userId);
    ChatUser GetCurrentUser();
    IReadOnlyList<ChatUser> GetAllUsers();
    ChatMessage SendMessage(string channelId, string content);
    void AddReaction(string messageId, string emoji);
}
