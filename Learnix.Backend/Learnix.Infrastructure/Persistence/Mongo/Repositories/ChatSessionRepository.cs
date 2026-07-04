using Learnix.Application.AiChat.Abstractions;
using Learnix.Application.AiChat.Abstractions.Models;
using Learnix.Infrastructure.Persistence.Mongo.Documents;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Learnix.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class ChatSessionRepository(MongoDbContext context) : IChatSessionRepository
{
    public async Task<ChatSession?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var filter = Builders<ChatSessionDocument>.Filter.And(
            Builders<ChatSessionDocument>.Filter.Eq(s => s.UserId, userId),
            Builders<ChatSessionDocument>.Filter.Eq(s => s.IsActive, true));

        var doc = await context.ChatSessions
            .Find(filter)
            .FirstOrDefaultAsync(ct);

        return doc is null ? null : MapToModel(doc);
    }

    public async Task<ChatSession> CreateAsync(Guid userId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var doc = new ChatSessionDocument
        {
            Id = ObjectId.GenerateNewId(),
            UserId = userId,
            IsActive = true,
            Messages = [],
            CreatedAt = now,
            UpdatedAt = now
        };

        await context.ChatSessions.InsertOneAsync(doc, cancellationToken: ct);
        return MapToModel(doc);
    }

    public async Task AppendMessagesAsync(string sessionId, IEnumerable<ChatMessage> messages, CancellationToken ct = default)
    {
        var id = ObjectId.Parse(sessionId);
        var docs = messages.Select(MapMessageToDocument).ToList();

        var update = Builders<ChatSessionDocument>.Update
            .PushEach(s => s.Messages, docs)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var filter = Builders<ChatSessionDocument>.Filter.Eq(s => s.Id, id);
        await context.ChatSessions.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task CloseSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var id = ObjectId.Parse(sessionId);
        var now = DateTime.UtcNow;

        var update = Builders<ChatSessionDocument>.Update
            .Set(s => s.IsActive, false)
            .Set(s => s.ClosedAt, now)
            .Set(s => s.UpdatedAt, now);

        var filter = Builders<ChatSessionDocument>.Filter.Eq(s => s.Id, id);
        await context.ChatSessions.UpdateOneAsync(filter, update, cancellationToken: ct);
    }

    public async Task DeleteOlderThanAsync(DateTime threshold, CancellationToken ct = default)
    {
        var filter = Builders<ChatSessionDocument>.Filter.And(
            Builders<ChatSessionDocument>.Filter.Eq(s => s.IsActive, false),
            Builders<ChatSessionDocument>.Filter.Lt(s => s.UpdatedAt, threshold));

        await context.ChatSessions.DeleteManyAsync(filter, ct);
    }

    private static ChatSession MapToModel(ChatSessionDocument doc) => new()
    {
        Id = doc.Id.ToString(),
        UserId = doc.UserId,
        IsActive = doc.IsActive,
        Messages = doc.Messages.Select(MapMessageToModel).ToList(),
        CreatedAt = doc.CreatedAt,
        UpdatedAt = doc.UpdatedAt
    };

    private static ChatMessage MapMessageToModel(ChatMessageDocument doc) => new(
        doc.Role,
        doc.Content,
        doc.SentAt,
        doc.ToolCalls?.Select(t => new ToolCall(t.CallId, t.ToolName, t.ArgumentsJson, t.ResultJson)).ToList());

    private static ChatMessageDocument MapMessageToDocument(ChatMessage msg) => new()
    {
        Role = msg.Role,
        Content = msg.Content,
        SentAt = msg.SentAt,
        ToolCalls = msg.ToolCalls?.Select(t => new ToolCallDocument
        {
            CallId = t.CallId,
            ToolName = t.ToolName,
            ArgumentsJson = t.ArgumentsJson,
            ResultJson = t.ResultJson
        }).ToList()
    };
}
