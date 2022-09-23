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

    public async Task<User?> GetUserByEmail(string email)
    {
        var users = DbService.Database.GetCollection<User>("users");

        var usersFilter = Builders<User>.Filter.Eq(x => x.Email, email);
        var found = await users.Find(usersFilter).ToListAsync();

        return found.Any() ? found.First() : null;
    }

    public async Task<User?> FindUserWithToken(string token)
    {
        var users = DbService.Database.GetCollection<User>("users");

        var usersFilter = Builders<User>.Filter.Eq(x => x.RefreshToken, token);
        var found = await users.Find(usersFilter).ToListAsync();

        return found.Any() ? found.First() : null;
    }

    public async Task<bool> UpdateOne(User user)
    {
        var users = DbService.Database.GetCollection<User>("users");

        var usersFilter = Builders<User>.Filter.Eq(x => x.Email, user.Email);
        var result = await users.ReplaceOneAsync(usersFilter, user);

        return result.ModifiedCount > 0;
    }
}