using WorkSlack.Models;

namespace WorkSlack.Services;

/// <summary>
/// In-memory implementation of <see cref="IChatService"/> with seed data.
/// </summary>
internal sealed class ChatService : IChatService
{
    private readonly Dictionary<string, ChatUser> _users;
    private readonly List<Channel> _channels;
    private readonly Dictionary<string, List<ChatMessage>> _messages;
    private readonly Dictionary<string, DateTimeOffset> _lastRead;
    private readonly HashSet<string> _bookmarks = [];
    private ChatUser _currentUser;

    public ChatService()
    {
        _currentUser = new ChatUser("u1", "Taro Yamada", "#4A90D9", UserStatus.Online, "💻", "Coding");

        _users = new Dictionary<string, ChatUser>
        {
            ["u1"] = _currentUser,
            ["u2"] = new("u2", "Hanako Sato", "#E06B56", UserStatus.Online, "📊", "Preparing Q3 report"),
            ["u3"] = new("u3", "Kenji Tanaka", "#2BAC76", UserStatus.Away, "☕", "On break"),
            ["u4"] = new("u4", "Yuki Suzuki", "#E0A32E", UserStatus.Online),
            ["u5"] = new("u5", "Akira Watanabe", "#9B59B6", UserStatus.DoNotDisturb, "🎧", "Focus time"),
            ["u6"] = new("u6", "Miki Ito", "#1ABC9C", UserStatus.Offline),
        };

        _channels =
        [
            new("ch-general", "general", "Company-wide announcements and work-based matters"),
            new("ch-random", "random", "Non-work banter and water cooler conversation"),
            new("ch-engineering", "engineering", "Engineering team discussions"),
            new("ch-design", "design", "Design reviews and feedback"),
            new("ch-marketing", "marketing", "Marketing campaigns and analytics"),
            new("dm-u2", "Hanako Sato", IsDirectMessage: true, DmUserId: "u2"),
            new("dm-u3", "Kenji Tanaka", IsDirectMessage: true, DmUserId: "u3"),
            new("dm-u5", "Akira Watanabe", IsDirectMessage: true, DmUserId: "u5"),
        ];

        var today = DateTimeOffset.Now.Date;
        var yesterday = today.AddDays(-1);

        _messages = new Dictionary<string, List<ChatMessage>>
        {
            ["ch-general"] =
            [
                Msg("m01", "ch-general", "u2", "Good morning everyone! 🌞 Hope you all had a great weekend.", yesterday.AddHours(9).AddMinutes(2)),
                Msg("m02", "ch-general", "u3", "Morning! Ready for the new sprint.", yesterday.AddHours(9).AddMinutes(5)),
                Msg("m03", "ch-general", "u4", "Don't forget: all-hands meeting at 3pm today in the main conference room.", yesterday.AddHours(9).AddMinutes(15),
                    [new("👍", ["u1", "u2", "u3"]), new("📅", ["u5"])]),
                Msg("m04", "ch-general", "u1", "Thanks for the reminder Yuki! I'll be there.", yesterday.AddHours(9).AddMinutes(18)),
                Msg("m05", "ch-general", "u5", "The new deployment pipeline is live. Please check your CI/CD configs.", yesterday.AddHours(14).AddMinutes(30)),
                Msg("m06", "ch-general", "u2", "Has anyone seen the *Q3 report* yet? I need the updated figures for my presentation.", today.AddHours(8).AddMinutes(45)),
                Msg("m07", "ch-general", "u4", "@Hanako I uploaded it to the shared drive yesterday. Let me know if you can't find it.", today.AddHours(8).AddMinutes(52)),
                Msg("m08", "ch-general", "u2", "Found it, thanks Yuki! 🙏", today.AddHours(9)),
            ],
            ["ch-random"] =
            [
                Msg("m10", "ch-random", "u3", "Anyone up for lunch at the new ramen place? 🍜", yesterday.AddHours(11).AddMinutes(30)),
                Msg("m11", "ch-random", "u6", "Count me in! I've been wanting to try it.", yesterday.AddHours(11).AddMinutes(32)),
                Msg("m12", "ch-random", "u1", "I'm in! Let's meet at the lobby at noon.", yesterday.AddHours(11).AddMinutes(35)),
                Msg("m13", "ch-random", "u3", "The ramen was amazing 🤤 We should go again next week.", yesterday.AddHours(13).AddMinutes(45),
                    [new("🍜", ["u1", "u6"]), new("😋", ["u2"])]),
                Msg("m14", "ch-random", "u4", "Just saw the funniest cat video. Sharing in the thread 😂", today.AddHours(10).AddMinutes(15)),
            ],
            ["ch-engineering"] =
            [
                Msg("m20", "ch-engineering", "u5", "I've refactored the authentication module. PR is up for review: `#428`", yesterday.AddHours(10)),
                Msg("m21", "ch-engineering", "u1", "Looking at it now. The token refresh logic looks much cleaner.", yesterday.AddHours(10).AddMinutes(30)),
                Msg("m22", "ch-engineering", "u3", "Nice work Akira! I left a few comments on the error handling.", yesterday.AddHours(11),
                    [new("🙌", ["u5"])]),
                Msg("m23", "ch-engineering", "u5", "Thanks for the review! I've addressed all the comments. Ready for another look.", yesterday.AddHours(15)),
                Msg("m24", "ch-engineering", "u1", "We need to discuss the *database migration strategy* for v2.0. Can we set up a meeting this week?", today.AddHours(9).AddMinutes(10)),
                Msg("m25", "ch-engineering", "u3", "How about Thursday at 2pm? I'll book the room.", today.AddHours(9).AddMinutes(20)),
                Msg("m26", "ch-engineering", "u5", "Works for me. I'll prepare the schema diagrams.", today.AddHours(9).AddMinutes(25),
                    [new("👍", ["u1", "u3"])]),
            ],
            ["ch-design"] =
            [
                Msg("m30", "ch-design", "u4", "Uploaded the new landing page mockups to Figma. Please review when you get a chance!", yesterday.AddHours(14)),
                Msg("m31", "ch-design", "u6", "Love the color palette! The gradient on the hero section is 🔥", yesterday.AddHours(14).AddMinutes(20)),
                Msg("m32", "ch-design", "u2", "The typography choices are great. One suggestion: can we increase the CTA button size?", yesterday.AddHours(15)),
                Msg("m33", "ch-design", "u4", "Good call! Updated. V2 is now in Figma.", yesterday.AddHours(16),
                    [new("✨", ["u2", "u6"])]),
            ],
            ["ch-marketing"] =
            [
                Msg("m40", "ch-marketing", "u2", "The email campaign for the product launch is ready for review.", today.AddHours(8)),
                Msg("m41", "ch-marketing", "u6", "I'll check the copy. Are we still targeting the same segments?", today.AddHours(8).AddMinutes(15)),
                Msg("m42", "ch-marketing", "u2", "Yes, same segments. I've added A/B variants for the subject line.", today.AddHours(8).AddMinutes(20)),
            ],
            ["dm-u2"] =
            [
                Msg("m50", "dm-u2", "u2", "Hey, do you have time for a quick sync today?", yesterday.AddHours(16)),
                Msg("m51", "dm-u2", "u1", "Sure! How about 4:30?", yesterday.AddHours(16).AddMinutes(5)),
                Msg("m52", "dm-u2", "u2", "Perfect, I'll send a calendar invite. ✅", yesterday.AddHours(16).AddMinutes(7)),
            ],
            ["dm-u3"] =
            [
                Msg("m60", "dm-u3", "u3", "Can you review my PR when you get a chance? It's the API pagination fix.", today.AddHours(10)),
                Msg("m61", "dm-u3", "u1", "Will do! Give me about 30 minutes.", today.AddHours(10).AddMinutes(3)),
            ],
            ["dm-u5"] =
            [
                Msg("m70", "dm-u5", "u5", "The staging environment is having some issues. Are you seeing the same?", today.AddHours(7).AddMinutes(30)),
                Msg("m71", "dm-u5", "u1", "Yes, I noticed some 503 errors. Let me check the logs.", today.AddHours(7).AddMinutes(35)),
                Msg("m72", "dm-u5", "u5", "Found it — it's a memory leak in the cache layer. Deploying a fix now.", today.AddHours(8)),
                Msg("m73", "dm-u5", "u1", "Great catch! Let me know when it's deployed.", today.AddHours(8).AddMinutes(2)),
            ],
        };

        // Seed thread replies
        SeedThreadReply("m03", "ch-general", "u1", "I'll set a reminder. Thanks!", yesterday.AddHours(9).AddMinutes(20));
        SeedThreadReply("m03", "ch-general", "u2", "Can we also discuss the roadmap?", yesterday.AddHours(9).AddMinutes(25));
        SeedThreadReply("m20", "ch-engineering", "u1", "The token expiry edge case on line 142 — should we add a retry?", yesterday.AddHours(10).AddMinutes(15));
        SeedThreadReply("m20", "ch-engineering", "u5", "Good catch. I'll add exponential backoff.", yesterday.AddHours(10).AddMinutes(25));
        SeedThreadReply("m20", "ch-engineering", "u3", "Also consider adding a circuit breaker pattern here.", yesterday.AddHours(10).AddMinutes(40));

        // Set last-read so some channels appear unread
        _lastRead = new Dictionary<string, DateTimeOffset>
        {
            ["ch-general"] = yesterday.AddHours(14).AddMinutes(30),
            ["ch-random"] = yesterday.AddHours(13).AddMinutes(45),
            ["ch-engineering"] = yesterday.AddHours(15),
            ["ch-design"] = yesterday.AddHours(16),
            ["ch-marketing"] = yesterday,
            ["dm-u2"] = yesterday.AddHours(16).AddMinutes(7),
            ["dm-u3"] = yesterday,
            ["dm-u5"] = today.AddHours(8).AddMinutes(2),
        };
    }

    public IReadOnlyList<Channel> GetChannels() =>
        _channels.Where(c => !c.IsDirectMessage).ToList();

    public IReadOnlyList<Channel> GetDirectMessages() =>
        _channels.Where(c => c.IsDirectMessage).ToList();

    public IReadOnlyList<ChatMessage> GetMessages(string channelId) =>
        _messages.TryGetValue(channelId, out var messages)
            ? messages.Where(m => m.ParentMessageId is null && !m.IsDeleted).ToList()
            : [];

    public Channel? GetChannel(string channelId) =>
        _channels.FirstOrDefault(c => c.Id == channelId);

    public ChatUser? GetUser(string userId) =>
        _users.GetValueOrDefault(userId);

    public ChatUser GetCurrentUser() => _currentUser;

    public IReadOnlyList<ChatUser> GetAllUsers() => [.. _users.Values];

    public ChatMessage SendMessage(string channelId, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var message = new ChatMessage
        {
            Id = $"m{DateTimeOffset.UtcNow.Ticks}",
            ChannelId = channelId,
            AuthorId = _currentUser.Id,
            Content = content,
            Timestamp = DateTimeOffset.Now,
        };

        if (!_messages.TryGetValue(channelId, out var messages))
        {
            messages = [];
            _messages[channelId] = messages;
        }

        messages.Add(message);
        return message;
    }

    public void AddReaction(string messageId, string emoji)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);

        var message = FindMessage(messageId);
        if (message is null) return;

        var existing = message.Reactions.FirstOrDefault(r => r.Emoji == emoji);
        if (existing is not null)
        {
            if (!existing.UserIds.Contains(_currentUser.Id))
            {
                existing.UserIds.Add(_currentUser.Id);
            }
        }
        else
        {
            message.Reactions.Add(new Reaction(emoji, [_currentUser.Id]));
        }
    }

    public void RemoveReaction(string messageId, string emoji)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);

        var message = FindMessage(messageId);
        if (message is null) return;

        var existing = message.Reactions.FirstOrDefault(r => r.Emoji == emoji);
        if (existing is null) return;

        existing.UserIds.Remove(_currentUser.Id);
        if (existing.UserIds.Count == 0)
        {
            message.Reactions.Remove(existing);
        }
    }

    public void ToggleReaction(string messageId, string emoji)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(emoji);

        var message = FindMessage(messageId);
        if (message is null) return;

        var existing = message.Reactions.FirstOrDefault(r => r.Emoji == emoji);
        if (existing is not null && existing.UserIds.Contains(_currentUser.Id))
        {
            RemoveReaction(messageId, emoji);
        }
        else
        {
            AddReaction(messageId, emoji);
        }
    }

    public void EditMessage(string messageId, string newContent)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(newContent);

        var message = FindMessage(messageId);
        if (message is null || message.AuthorId != _currentUser.Id) return;

        message.Content = newContent;
        message.IsEdited = true;
    }

    public void DeleteMessage(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var message = FindMessage(messageId);
        if (message is null || message.AuthorId != _currentUser.Id) return;

        message.IsDeleted = true;
    }

    public int GetUnreadCount(string channelId)
    {
        if (!_messages.TryGetValue(channelId, out var messages)) return 0;
        if (!_lastRead.TryGetValue(channelId, out var lastRead)) return messages.Count;

        return messages.Count(m => m.Timestamp > lastRead && m.AuthorId != _currentUser.Id && !m.IsDeleted);
    }

    public void MarkAsRead(string channelId)
    {
        _lastRead[channelId] = DateTimeOffset.Now;
    }

    public IReadOnlyList<ChatMessage> GetThreadReplies(string parentMessageId) =>
        _messages.Values
            .SelectMany(m => m)
            .Where(m => m.ParentMessageId == parentMessageId && !m.IsDeleted)
            .OrderBy(m => m.Timestamp)
            .ToList();

    public ChatMessage SendThreadReply(string channelId, string parentMessageId, string content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(channelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(parentMessageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        var reply = new ChatMessage
        {
            Id = $"m{DateTimeOffset.UtcNow.Ticks}",
            ChannelId = channelId,
            AuthorId = _currentUser.Id,
            Content = content,
            Timestamp = DateTimeOffset.Now,
            ParentMessageId = parentMessageId,
        };

        if (!_messages.TryGetValue(channelId, out var messages))
        {
            messages = [];
            _messages[channelId] = messages;
        }

        messages.Add(reply);

        var parent = FindMessage(parentMessageId);
        if (parent is not null)
        {
            parent.ReplyCount = GetThreadReplies(parentMessageId).Count;
        }

        return reply;
    }

    public IReadOnlyList<ChatMessage> SearchMessages(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        return _messages.Values
            .SelectMany(m => m)
            .Where(m => !m.IsDeleted && m.Content.Contains(query, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => m.Timestamp)
            .Take(20)
            .ToList();
    }

    public void SetCustomStatus(string? emoji, string? text)
    {
        _currentUser = _currentUser with { CustomStatusEmoji = emoji, CustomStatusText = text };
        _users[_currentUser.Id] = _currentUser;
    }

    public void ToggleBookmark(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        if (!_bookmarks.Add(messageId))
            _bookmarks.Remove(messageId);
    }

    public bool IsBookmarked(string messageId) => _bookmarks.Contains(messageId);

    public IReadOnlyList<ChatMessage> GetBookmarkedMessages() =>
        _messages.Values
            .SelectMany(m => m)
            .Where(m => _bookmarks.Contains(m.Id) && !m.IsDeleted)
            .OrderByDescending(m => m.Timestamp)
            .ToList();

    public void PinMessage(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var msg = FindMessage(messageId);
        if (msg is not null) msg.IsPinned = true;
    }

    public void UnpinMessage(string messageId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        var msg = FindMessage(messageId);
        if (msg is not null) msg.IsPinned = false;
    }

    public IReadOnlyList<ChatMessage> GetPinnedMessages(string channelId) =>
        _messages.TryGetValue(channelId, out var msgs)
            ? msgs.Where(m => m.IsPinned && !m.IsDeleted && m.ParentMessageId is null).ToList()
            : [];

    private ChatMessage? FindMessage(string messageId)
    {
        foreach (var messages in _messages.Values)
        {
            var message = messages.FirstOrDefault(m => m.Id == messageId);
            if (message is not null) return message;
        }
        return null;
    }

    private void SeedThreadReply(string parentId, string channelId, string authorId, string content, DateTimeOffset timestamp)
    {
        var reply = new ChatMessage
        {
            Id = $"reply-{parentId}-{timestamp.Ticks}",
            ChannelId = channelId,
            AuthorId = authorId,
            Content = content,
            Timestamp = timestamp,
            ParentMessageId = parentId,
        };

        if (!_messages.TryGetValue(channelId, out var messages))
        {
            messages = [];
            _messages[channelId] = messages;
        }

        messages.Add(reply);

        var parent = FindMessage(parentId);
        if (parent is not null)
        {
            parent.ReplyCount = _messages.Values
                .SelectMany(m => m)
                .Count(m => m.ParentMessageId == parentId);
        }
    }

    private static ChatMessage Msg(
        string id,
        string channelId,
        string authorId,
        string content,
        DateTimeOffset timestamp,
        List<Reaction>? reactions = null) => new()
        {
            Id = id,
            ChannelId = channelId,
            AuthorId = authorId,
            Content = content,
            Timestamp = timestamp,
            Reactions = reactions ?? [],
        };
}
