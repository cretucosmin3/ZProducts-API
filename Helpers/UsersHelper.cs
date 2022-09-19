using MongoDB.Driver;
using ProductAPI.DbContracts;
using ProductAPI.Services;

namespace ProductAPI.Helpers.Database;

public class UsersHelper
{
    private IDatabaseService DbService { get; set; } = default!;

    public static UsersHelper WithService(IDatabaseService dbService)
    {
        return new UsersHelper()
        {
            DbService = dbService
        };
    }

    public async Task<bool> EmailExists(string email)
    {
        var users = DbService.Database.GetCollection<User>("users");

        var usersFilter = Builders<User>.Filter.Eq(x => x.Email, email);
        var found = await users.Find(usersFilter).ToListAsync();

        return found.Any();
    }

    public async Task<bool> CreateNew(User newUser)
    {
        var users = DbService.Database.GetCollection<User>("users");

        try { await users.InsertOneAsync(newUser); }
        catch (Exception _) { return false; }

        return true;
    }
}