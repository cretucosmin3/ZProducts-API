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
                float lastPrice = -1;
                var history = dbData.First().PriceHistory;

                if (history != null && history.Count > 0)
                {
                    toAddOrUpdate.PriceHistory = history;
                    lastPrice = dbData.First().PriceHistory.Last().Value;
                }
                else toAddOrUpdate.PriceHistory = new Dictionary<string, float>();

                if (lastPrice != price)
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
        }

        float averagePrice = 0;
        float count = 0;
        var allIndexSites = siteHelper.FindForIndex(update.Index);

        foreach (var site in allIndexSites)
        {
            count += 1f;
            averagePrice += site.Price;
        }

        averagePrice /= count;

        relatedIndex.AveragePrice = (int)averagePrice;
        relatedIndex.SitesIndexed = (int)count;
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
        var hoursPassed = latest == null || DateTime.UtcNow.Day != latest.FinishedAt.Day;

        if (processHelper.HasInProgress("General Indexing")) return false;

        return hoursPassed;
    }

    [HttpGet("get-index-names-for-crawling"), ApiKey]
    public ActionResult<string[]> IndexNeedsCrawling()
    {
        var indexHeleper = IndexHelper.WithService(dbService);
        return indexHeleper.IndexNamesForCrawling();
    }

    [HttpGet("get-index-names"), ApiKey]
    public ActionResult<string[]> GetIndexNames()
    {
        var indexHeleper = IndexHelper.WithService(dbService);
        var indexes = indexHeleper.AllIndexeNames();

        return indexes.Length > 0 ? indexHeleper.AllIndexeNames() : new string[0];
    }

    [HttpGet("get-index-sites"), ApiKey]
    public ActionResult<string[]> GetIndexSites(string indexName)
    {
        try
        {
            var siteHelper = SiteHelper.WithService(dbService);
            var sites = siteHelper.FindForIndex(indexName);
            return sites.Any() ? sites.Select(e => e.Url).ToArray() : new string[0];
        }
        catch (Exception x)
        {
            Console.WriteLine(x.Message);
        }

        return new string[0];
    }

    [HttpGet("get-index-data"), ApiKey]
    public ActionResult<SearchIndex> GetIndexData(string indexName)
    {
        var indexHeleper = IndexHelper.WithService(dbService);
        var index = indexHeleper.GetIndex(indexName);

        if (index == null) return BadRequest("Index not found");

        return index;
    }

    [HttpGet("add-process"), ApiKey]
    public ActionResult<string?> AddProcess(string processName)
    {
        var processHelper = ProcessHelper.WithService(dbService);
        var processId = processHelper.AddProcess(new Process()
        {
            Name = processName,
            StartedAt = DateTime.UtcNow,
        });

        return processId;
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