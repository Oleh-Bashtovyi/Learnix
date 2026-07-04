using Learnix.Infrastructure.Persistence.Mongo.Conventions;
using Learnix.Infrastructure.Persistence.Mongo.Documents;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Learnix.Infrastructure.Persistence.Mongo;

public sealed class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IMongoClient client, IOptions<MongoSettings> options)
    {
        MongoConventionRegistration.Register();
        _database = client.GetDatabase(options.Value.DatabaseName);
    }

    public IMongoCollection<ChatSessionDocument> ChatSessions =>
        _database.GetCollection<ChatSessionDocument>("chat_sessions");
}
