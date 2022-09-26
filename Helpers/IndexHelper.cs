using MongoDB.Driver;
using ProductAPI.DbContracts;
using ProductAPI.Services;

namespace ProductAPI.Helpers.Database;

public class IndexHelper
{
    private readonly string collectionName = "search-indexes";
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
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(collectionName);

        var iTlower = indexText.ToLower();
        var tokensFilter = Builders<SearchIndex>.Filter.Eq(x => x.TextToSearch, iTlower);
        return searchIndexes.CountDocuments(tokensFilter) > 0;
    }

    public bool AddIndex(SearchIndex newIndex)
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(collectionName);

        try { searchIndexes.InsertOne(newIndex); }
        catch (Exception _) { return false; }

        return true;
    }

    public List<SearchIndex> ListIndexes(string filter, int skip, int take)
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(collectionName);

        if (string.IsNullOrEmpty(filter))
            return searchIndexes.Find(e => true).Skip(skip).Limit(take).ToList<SearchIndex>();

        filter = filter.ToLower();
        var searchFilter = Builders<SearchIndex>.Filter.AnyStringIn("TextToSearch", filter);
        return searchIndexes.Find(searchFilter).Skip(skip).Limit(take).ToList<SearchIndex>();
    }

    public List<SearchIndex> AllIndexes()
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(collectionName);
        return searchIndexes.Find(e => true).ToList<SearchIndex>();
    }

    public long CountIndexes(string filter = "")
    {
        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(collectionName);

        if (string.IsNullOrEmpty(filter))
            return searchIndexes.CountDocuments(e => true);

        filter = filter.ToLower();
        return searchIndexes.CountDocuments(e => e.TextToSearch.Contains(filter));
    }

    public bool Delete(string textToSearch)
    {
        textToSearch = textToSearch.ToLower();

        var searchIndexes = DbService.Database.GetCollection<SearchIndex>(collectionName);
        var tokensFilter = Builders<SearchIndex>.Filter.Eq(x => x.TextToSearch, textToSearch.ToLower());
        var result = searchIndexes.DeleteOne(tokensFilter);

        return result.DeletedCount > 0;
    }
}