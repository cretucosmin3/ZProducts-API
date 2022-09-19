using MongoDB.Driver;

namespace ProductAPI.Services;

public interface IDatabaseService
{
    IMongoDatabase Database { get; }
    MongoClient GetClient();
    IMongoDatabase GetDatabase();
}