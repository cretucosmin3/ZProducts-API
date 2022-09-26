using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductAPI.DbContracts;
using ProductAPI.Helpers.Database;
using ProductAPI.Services;

namespace ProductAPI.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin")]
public class IndexController : ControllerBase
{
    private readonly IDatabaseService dbService;
    public IndexController(IDatabaseService dbS) => dbService = dbS;
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
    public ActionResult<List<SearchIndex>> ListIndexes(string? filter, int page = 1)
    {
        var indexHeleper = IndexHelper.WithService(dbService);

        if (page > 0)
            return indexHeleper.ListIndexes(filter ?? "", _pageRows * (page - 1), _pageRows * page);

        return indexHeleper.AllIndexes();
    }

    [HttpGet("pages-count")]
    public ActionResult<long> PagesCount(string? filter)
    {
        var indexHeleper = IndexHelper.WithService(dbService);
        return 1 + indexHeleper.CountIndexes(filter ?? "") / _pageRows;
    }

    [HttpGet("delete")]
    public ActionResult<bool> DeleteIndex(string? indexText)
    {
        var indexHeleper = IndexHelper.WithService(dbService);
        return indexHeleper.Delete(indexText);
    }
}

public class AddIndexForm
{
    [JsonPropertyName("productText")]
    public string TextToSearch { get; set; } = String.Empty;

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