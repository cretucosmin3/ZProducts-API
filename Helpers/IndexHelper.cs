using System.Linq.Expressions;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Driver;
using ProductAPI.DbContracts;
using ProductAPI.Services;
using ProductAPI.SinglarRHubs;

namespace ProductAPI.Helpers.Database;

public class IndexHelper
{
    private readonly string DbCollectionName = "search-indexes";
    private IDatabaseService DbService { get; set; } = default!;

    public static IndexHelper WithService(IDatabaseService dbService)
    {
        return new IndexHelper()
        {
            DbService = dbService
        };
    }

    public bool IndexExists(string indexText)
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);

        var iTlower = indexText.ToLower();
        var tokensFilter = Builders<SearchIndex>.Filter.Eq(x => x.TextToSearch, iTlower);
        return searchIndexes.CountDocuments(tokensFilter) > 0;
    }

    public bool AddIndex(SearchIndex newIndex)
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);

        try { searchIndexes.InsertOne(newIndex); }
        catch (Exception _) { return false; }

        return true;
    }

    public List<SearchIndex> ListIndexes(string filter, int skip, int take)
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);

        if (string.IsNullOrEmpty(filter))
            return searchIndexes.Find(e => true).Skip(skip).Limit(take).ToList<SearchIndex>();

        filter = filter.ToLower();
        var searchFilter = Builders<SearchIndex>.Filter.AnyStringIn("TextToSearch", filter);
        return searchIndexes.Find(searchFilter).Skip(skip).Limit(take).ToList<SearchIndex>();
    }

    public List<SearchIndex> AllIndexes()
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);
        return searchIndexes.Find(e => true).ToList<SearchIndex>();
    }

    public string[] AllIndexeNames()
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);
        return searchIndexes.Find(e => true).ToList<SearchIndex>().Select(e => e.TextToSearch).ToArray();
    }

    public string[] IndexNamesForCrawling()
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);
        return searchIndexes.Find(e => true)
            .ToList()
            .Where(e => DateTime.UtcNow.Day != e.LastUpdate.Day)
            .Select(e => e.TextToSearch)
            .ToArray();
    }

    public long CountIndexes(string filter = "")
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);

        if (string.IsNullOrEmpty(filter))
            return searchIndexes.CountDocuments(e => true);

        filter = filter.ToLower();
        return searchIndexes.CountDocuments(e => e.TextToSearch.Contains(filter));
    }

    public bool Delete(string textToSearch)
    {
        textToSearch = textToSearch.ToLower();

        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);
        var tokensFilter = Builders<SearchIndex>.Filter.Eq(x => x.TextToSearch, textToSearch.ToLower());
        var result = searchIndexes.DeleteOne(tokensFilter);

        return result.DeletedCount > 0;
    }

    public SearchIndex? GetIndex(string indexText)
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);

        var iTlower = indexText.ToLower();
        var tokensFilter = Builders<SearchIndex>.Filter.Eq(x => x.TextToSearch, iTlower);

        return searchIndexes.Find(tokensFilter).First() ?? null;
    }

    public bool UpdateIndex(SearchIndex index)
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);

        Expression<Func<SearchIndex, bool>> filter = m => m.TextToSearch == index.TextToSearch;

        UpdateDefinition<SearchIndex> update = Builders<SearchIndex>.Update
            .Set(m => m.AveragePrice, index.AveragePrice)
            .Set(m => m.ImageUrl, index.ImageUrl)
            .Set(m => m.SitesIndexed, index.SitesIndexed)
            .Set(m => m.LastUpdate, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<SearchIndex, SearchIndex>
        {
            IsUpsert = false,
            ReturnDocument = ReturnDocument.After
        };

        var updated = searchIndexes.FindOneAndUpdate(filter, update, options);
        return updated != null;
    }

    public bool UpdateTime(string indexText)
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(DbCollectionName);

        Expression<Func<SearchIndex, bool>> filter = m => m.TextToSearch == indexText;

        UpdateDefinition<SearchIndex> update = Builders<SearchIndex>.Update
            .Set(m => m.LastUpdate, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<SearchIndex, SearchIndex>
        {
            IsUpsert = false,
            ReturnDocument = ReturnDocument.After
        };

        var updated = searchIndexes.FindOneAndUpdate(filter, update, options);
        return updated != null;
    }
}