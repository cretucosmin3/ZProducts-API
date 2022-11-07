using System.Linq.Expressions;
using MongoDB.Driver;
using ProductAPI.DbContracts;
using ProductAPI.Services;

namespace ProductAPI.Helpers.Database;

public class ProcessHelper
{
    private string DbCollectionName = "processes";
    private IDatabaseService DbService { get; set; } = default!;

    public static ProcessHelper WithService(IDatabaseService dbService)
    {
        return new ProcessHelper()
        {
            DbService = dbService
        };
    }

    public string? AddProcess(Process newProc)
    {
        var processes = DbService.Database.GetCollection<Process>(DbCollectionName);

        try { processes.InsertOne(newProc); }
        catch (Exception _) { return null; }

        return newProc.Id.ToString();
    }

    public bool HasInProgress(string processName)
    {
        var tokens = DbService.Database.GetCollection<Process>(DbCollectionName);
        var counter = tokens.CountDocuments(x => x.Name == processName && x.Finished == false);

        return counter > 0;
    }

    public Process? LatestFinished(string processName)
    {
        var tokens = DbService.Database.GetCollection<Process>(DbCollectionName);
        var found = tokens.Find(x => x.Name == processName && x.Finished).SortBy(e => e.FinishedAt);
        return found.First();
    }

    public Process? FindInProgress(string processName)
    {
        var tokens = DbService.Database.GetCollection<Process>(DbCollectionName);
        var found = tokens.Find(x => x.Name == processName && x.Finished == false).ToList();

        if (found == null || !found.Any()) return null;

        return found.First();
    }

    public bool UpdateProgress(string processName, string progressText, int progress)
    {
        var processes = DbService.Database.GetCollection<Process>(DbCollectionName);

        Expression<Func<Process, bool>> filter = m => m.Name == processName && !m.Finished;

        UpdateDefinition<Process> update = Builders<Process>.Update
            .Set(m => m.HasError, false)
            .Set(m => m.Progress, progress)
            .Set(m => m.ProgressText, progressText)
            .Set(m => m.Finished, progress == 100)
            .Set(m => m.FinishedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Process, Process>
        {
            IsUpsert = false,
            ReturnDocument = ReturnDocument.After
        };

        var updated = processes.FindOneAndUpdate(filter, update, options);
        return updated.Progress == progress;
    }

    public bool UpdateError(string processName, string errorText)
    {
        var processes = DbService.Database.GetCollection<Process>(DbCollectionName);

        Expression<Func<Process, bool>> filter = m => m.Name == processName && !m.Finished;

        UpdateDefinition<Process> update = Builders<Process>.Update
            .Set(m => m.Finished, true)
            .Set(m => m.HasError, true)
            .Set(m => m.FinishedAt, DateTime.UtcNow)
            .Set(m => m.ErrorMessage, errorText);

        var options = new FindOneAndUpdateOptions<Process, Process>
        {
            IsUpsert = false,
            ReturnDocument = ReturnDocument.After
        };

        var updated = processes.FindOneAndUpdate(filter, update, options);
        return updated.HasError == true;
    }

    public bool Delete(string accessCode)
    {
        var tokens = DbService.Database.GetCollection<AccountToken>(DbCollectionName);
        var tokensFilter = Builders<AccountToken>.Filter.Eq(x => x.AccessCode, accessCode);
        var result = tokens.DeleteOne(tokensFilter);

        return result.DeletedCount > 0;
    }
}