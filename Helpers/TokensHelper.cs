using MongoDB.Driver;
using ProductAPI.DbContracts;
using ProductAPI.Services;

namespace ProductAPI.Helpers.Database;

public class TokensHelper
{
    private readonly string collectionName = "account-tokens";
    private IDatabaseService DbService { get; set; } = default!;

    public static TokensHelper WithService(IDatabaseService dbService)
    {
        return new TokensHelper()
        {
            DbService = dbService
        };
    }

    public async Task<AccountToken?> GetByAccessCode(string accessCode)
    {
        var tokens = DbService.Database.GetCollection<AccountToken>(collectionName);

        var tokensFilter = Builders<AccountToken>.Filter.Eq(x => x.AccessCode, accessCode);
        var found = await tokens.Find(tokensFilter).ToListAsync();

        return found.Any() ? found.First() : null;
    }

    public async Task<bool> Delete(string accessCode)
    {
        var tokens = DbService.Database.GetCollection<AccountToken>(collectionName);
        var tokensFilter = Builders<AccountToken>.Filter.Eq(x => x.AccessCode, accessCode);
        var result = await tokens.DeleteOneAsync(tokensFilter);

        return result.DeletedCount > 0;
    }
}