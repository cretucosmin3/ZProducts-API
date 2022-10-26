using System.Linq.Expressions;
using MongoDB.Driver;
using ProductAPI.DbContracts;
using ProductAPI.Services;

namespace ProductAPI.Helpers.Database;

public class SiteHelper
{
    private string DbCollectionName = "sites";
    private IDatabaseService DbService { get; set; } = default!;

    public static SiteHelper WithService(IDatabaseService dbService)
    {
        return new SiteHelper()
        {
            DbService = dbService
        };
    }

    public bool AddOrUpdate(Site site, SearchIndex index)
    {
        var sites = DbService.Database.GetCollection<Site>(DbCollectionName);

        Expression<Func<Site, bool>> filter = m => m.Url == site.Url && m.IndexParent == index.TextToSearch;

        UpdateDefinition<Site> update = Builders<Site>.Update
            .Set(m => m.Url, site.Url)
            .Set(m => m.Domain, site.Domain)
            .Set(m => m.IndexParent, index.TextToSearch)
            .Set(m => m.Price, site.Price)
            .Set(m => m.PriceHistory, site.PriceHistory)
            .Set(m => m.LastUpdate, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<Site, Site>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After
        };

        var updated = sites.FindOneAndUpdate(filter, update, options);
        return updated.Price == site.Price;
    }

    public List<Site> FindForIndex(string indexText)
    {
        var sites = DbService.Database.GetCollection<Site>(DbCollectionName);
        var filter = Builders<Site>.Filter.Eq(e => e.IndexParent, indexText);

        return sites.Find(filter).ToList<Site>();
    }

    public bool DeleteIndexSites(string indexText)
    {
        var sites = DbService.Database.GetCollection<Site>(DbCollectionName);
        var filter = Builders<Site>.Filter.Eq(x => x.IndexParent, indexText.ToLower());
        var result = sites.DeleteMany(filter);

        return result.DeletedCount > 0;
    }
}