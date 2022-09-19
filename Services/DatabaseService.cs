using System.Security.Claims;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ProductAPI.Services;

public class DatabaseService : IDatabaseService
{
    private readonly IOptions<AppConfig> config;

    public DatabaseService(IOptions<AppConfig> config)
    {
        this.config = config;
    }

    public IMongoDatabase Database => GetClient().GetDatabase("ProductDB");

    public MongoClient GetClient()
    {
        var settings = MongoClientSettings.FromConnectionString(config.Value.DbConnectionString);
        var client = new MongoClient(settings);
        return client;
    }

    public IMongoDatabase GetDatabase()
    {
        return GetClient().GetDatabase("ProductDB");
    }
}