using System.Globalization;
using ProductAPI.Services;

namespace ProductAPI.Helpers.Database;

public class HelperBase
{
    public virtual string DbCollectionName { get; set; }
    public IDatabaseService DbService { get; set; } = default!;

    public HelperBase(IDatabaseService dbService)
    {
        DbService = dbService;
    }
}