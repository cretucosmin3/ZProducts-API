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
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class IndexController : ControllerBase
{
    private readonly IDatabaseService dbService;
    private readonly IHttpClientFactory clientFactory;
    private readonly IHubContext<ProgressHub> progressHub;

    public IndexController(IDatabaseService dbS, IHttpClientFactory cFactory, IHubContext<ProgressHub> hubContext)
    {
        dbService = dbS;
        clientFactory = cFactory;
        progressHub = hubContext;
    }
    private int _pageRows = 15;

    [HttpPost("add-new")]
    public ActionResult<bool> AddNew(AddIndexForm indexData)
    {
        // Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(formData));
        var indexHeleper = IndexHelper.WithService(dbService);

        if (indexHeleper.IndexExists(indexData.TextToSearch))
            return BadRequest("Index already exists");

        var added = indexHeleper.AddIndex(new DbContracts.SearchIndex()
        {
            TextToSearch = indexData.TextToSearch.ToLower(),
            TitleKeywords = indexData.TitleKeywords.ToLower(),
            MaxSites = indexData.MaxSites,
            RelativePrice = indexData.RelativePrice,
            UseGoogle = indexData.UseGoogle,
            UseYahoo = indexData.UseYahoo,
            UseBing = indexData.UseBing,
            SitesIndexed = 0,
            LastUpdate = DateTime.UtcNow,
        });

        if (!added)
            return BadRequest("Internal error while adding index");

        return Ok(true);
    }

    [HttpGet("list-indexes")]
    public ActionResult<List<SearchIndex>> ListIndexes()
    {
        var indexHeleper = IndexHelper.WithService(dbService);
        return indexHeleper.AllIndexes();
    }

    [HttpGet("force-indexing")]
    public async Task<ActionResult<bool>> ForceIndexing(string indexText)
    {
        var processHelper = ProcessHelper.WithService(dbService);
        string processName = $"Indexing '{indexText}'";

        if (processHelper.HasInProgress(processName))
            return BadRequest("Already in progress");

        var processId = processHelper.AddProcess(new Process()
        {
            Name = processName,
            StartedAt = DateTime.UtcNow,
        });

        if (string.IsNullOrEmpty(processId)) return BadRequest("Process failed");

        var indexHeleper = IndexHelper.WithService(dbService);
        var searchIndex = indexHeleper.GetIndex(indexText);

        if (searchIndex == null)
            return BadRequest("Index doesn't exist");

        var http = HttpHelper.WithFactory(clientFactory);

        var parameters = new Dictionary<string, string?>()
        {
            // {"processId", processId},
            {"processName", processName},
            {"textToSearch", searchIndex.TextToSearch},
            {"titleKeywords", searchIndex.TitleKeywords},
            {"relativePrice", searchIndex.RelativePrice.ToString()}
        };

        var result = await http.Get<bool>("force-index", 10, parameters);

        if (result.Failed)
            return BadRequest("Something went wrong internally");

        return true;
    }

    [HttpGet("get-full-info")]
    public ActionResult<IndexFullInfo> GetFullInfo(string indexText)
    {
        string processName = $"Indexing '{indexText}'";
        var indexHelper = IndexHelper.WithService(dbService);
        var siteHelper = SiteHelper.WithService(dbService);

        var searchIndex = indexHelper.GetIndex(indexText);
        var indexSites = siteHelper.FindForIndex(indexText);

        if (searchIndex == null || indexSites == null)
            return NotFound("Data not found");

        return Ok(new IndexFullInfo()
        {
            index = searchIndex,
            sites = indexSites
        });
    }

    [HttpGet("has-in-progress")]
    public async Task<ActionResult<Process?>> HasInProgress(string indexText)
    {
        string processName = $"Indexing '{indexText}'";
        var processHelper = ProcessHelper.WithService(dbService);

        var process = processHelper.FindInProgress(processName);

        var http = HttpHelper.WithFactory(clientFactory);
        var parameters = new Dictionary<string, string?>()
        {
            {"processName", processName},
        };

        if (process == null)
            return NotFound("Process not found");

        var exists = await http.Get<bool>("process-exists", 10, parameters);

        Console.WriteLine($"Process exists? {exists.Data.ToString()}");

        if (!exists.Failed && !exists.Data)
        {
            processHelper.UpdateError(processName, "Unknown error.");
        }

        return processHelper.FindInProgress(processName);
    }

    [HttpGet("pages-count")]
    public ActionResult<long> PagesCount(string? filter)
    {
        var indexHeleper = IndexHelper.WithService(dbService);
        return 1 + indexHeleper.CountIndexes(filter ?? "") / _pageRows;
    }

    [HttpGet("delete")]
    public ActionResult<bool> DeleteIndex(string indexText)
    {
        var indexHeleper = IndexHelper.WithService(dbService);
        var siteHelper = SiteHelper.WithService(dbService);

        return indexHeleper.Delete(indexText) && siteHelper.DeleteIndexSites(indexText);
    }
}

public class AddIndexForm
{
    [JsonPropertyName("productText")]
    public string TextToSearch { get; set; } = String.Empty;

    [JsonPropertyName("relativePrice")]
    public float RelativePrice { get; set; }

    [JsonPropertyName("maxSites")]
    public double MaxSites { get; set; }

    [JsonPropertyName("titleKeywords")]
    public string TitleKeywords { get; set; } = String.Empty;

    [JsonPropertyName("specialPriceFormula")]
    public string SpecialPriceFormula { get; set; } = String.Empty;

    [JsonPropertyName("useGoogle")]
    public bool UseGoogle { get; set; } = true;

    [JsonPropertyName("useYahoo")]
    public bool UseYahoo { get; set; } = true;

    [JsonPropertyName("useBing")]
    public bool UseBing { get; set; } = true;
}

public class ProcessUpdate
{
    public bool Finished { get; set; } = false;
    public bool HasError { get; set; } = false;
    public int Progress { get; set; } = 0;
    public string ProgressText { get; set; } = default!;
    public string ErrorMessage { get; set; } = default!;
}

public class IndexFullInfo
{
    public SearchIndex index { get; set; } = default!;
    public List<Site> sites { get; set; } = default!;
}