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
    void RemoveReaction(string messageId, string emoji);
    void ToggleReaction(string messageId, string emoji);
    void EditMessage(string messageId, string newContent);
    void DeleteMessage(string messageId);
    int GetUnreadCount(string channelId);
    void MarkAsRead(string channelId);
    IReadOnlyList<ChatMessage> GetThreadReplies(string parentMessageId);
    ChatMessage SendThreadReply(string channelId, string parentMessageId, string content);
    IReadOnlyList<ChatMessage> SearchMessages(string query);
}
