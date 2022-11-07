using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using ProductAPI.DbContracts;
using ProductAPI.Helpers;
using ProductAPI.Helpers.Database;
using ProductAPI.Services;
using ProductAPI.SinglarRHubs;
using ProductAPI.Attributes;

namespace ProductAPI.Controllers;

[ApiController]
// [Route("[controller]")]
[Route("crawler")]
public class CrawlerController : ControllerBase
{
    private readonly IDatabaseService dbService;
    private readonly IHttpClientFactory clientFactory;
    private readonly IHubContext<ProgressHub> progressHub;

    public CrawlerController(IDatabaseService dbS, IHttpClientFactory cFactory, IHubContext<ProgressHub> hubContext)
    {
        dbService = dbS;
        clientFactory = cFactory;
        progressHub = hubContext;
    }

    [HttpPost("update-sites"), ApiKey]
    public ActionResult UpdateSites(UpdateSites update)
    {
        var siteHelper = SiteHelper.WithService(dbService);
        var indexHelper = IndexHelper.WithService(dbService);

        var relatedIndex = indexHelper.GetIndex(update.Index);

        if (relatedIndex == null)
            return BadRequest("Could not find index to update");

        var sites = siteHelper.FindForIndex(relatedIndex.TextToSearch);

        Dictionary<string, Site> ToAddOrUpdate = new Dictionary<string, Site>();

        float averagePrice = 0;
        foreach (var (url, price) in update.Sites)
        {
            Uri uriAddress = new Uri(url);
            var domain = uriAddress.GetLeftPart(UriPartial.Authority);
            domain = domain.Substring(domain.IndexOf("//") + 2).Replace("www.", "");

            var dbData = sites.Where(e => e.Url == url) ?? null;

            var toAddOrUpdate = new Site()
            {
                Url = url,
                Domain = domain,
                IndexParent = relatedIndex.TextToSearch,
                Price = price
            };

            if (dbData != null && dbData.Any())
            {
                toAddOrUpdate.PriceHistory = dbData.First().PriceHistory;
                toAddOrUpdate.PriceHistory.TryAdd(DateTime.UtcNow.ToShortDateString(), price);
            }
            else
            {
                toAddOrUpdate.PriceHistory = new Dictionary<string, float>()
                {
                    { DateTime.UtcNow.ToShortDateString(), (int)price }
                };
            }

            siteHelper.AddOrUpdate(toAddOrUpdate, relatedIndex);

            averagePrice += price;
        }

        averagePrice /= update.Sites.Count;
        relatedIndex.AveragePrice = (int)averagePrice;
        relatedIndex.SitesIndexed = update.Sites.Count;
        relatedIndex.ImageUrl = update.Image ?? "";

        if (!indexHelper.UpdateIndex(relatedIndex))
            return BadRequest("Could not updaate index");

        return Ok();
    }

    [HttpGet("needs-general-search"), ApiKey]
    public ActionResult<bool> NeedsGeneralSearch()
    {
        var processHelper = ProcessHelper.WithService(dbService);
        var latest = processHelper.LatestFinished("General Indexing");

        return latest != null && DateTime.UtcNow.Day != latest.FinishedAt.Day;
    }
}

public class UpdateSites
{
    [JsonPropertyName("index")]
    public string Index { get; set; } = default!;

    [JsonPropertyName("image")]
    public string? Image { get; set; } = default!;

    [JsonPropertyName("sites")]
    public Dictionary<string, float> Sites { get; set; } = default!;

}