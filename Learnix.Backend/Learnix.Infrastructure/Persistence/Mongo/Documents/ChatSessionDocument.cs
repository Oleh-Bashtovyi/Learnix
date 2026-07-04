using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Learnix.Infrastructure.Persistence.Mongo.Documents;

public sealed class ChatSessionDocument
{
    [BsonId]
    public ObjectId Id { get; set; }

    public Guid UserId { get; set; }
    public bool IsActive { get; set; }
    public List<ChatMessageDocument> Messages { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
