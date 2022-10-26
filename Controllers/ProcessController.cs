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
public class ProcessController : ControllerBase
{
    private readonly IDatabaseService dbService;
    private readonly IHttpClientFactory clientFactory;
    private readonly IHubContext<ProgressHub> progressHub;

    public ProcessController(IDatabaseService dbS, IHttpClientFactory cFactory, IHubContext<ProgressHub> hubContext)
    {
        dbService = dbS;
        clientFactory = cFactory;
        progressHub = hubContext;
    }

    [HttpGet("update"), ApiKey]
    public ActionResult Update(string processName, int progress, string progressText)
    {
        var processHelper = ProcessHelper.WithService(dbService);
        var updated = processHelper.UpdateProgress(processName, progressText, progress);

        progressHub.Clients.All.SendAsync(processName, new ProcessUpdate()
        {
            Progress = progress,
            ProgressText = progressText
        });

        return updated ? Ok() : BadRequest();
    }

    [HttpGet("error"), ApiKey]
    public ActionResult Error(string processName, string errorText)
    {
        var processHelper = ProcessHelper.WithService(dbService);

        var updated = processHelper.UpdateError(processName, errorText);

        progressHub.Clients.All.SendAsync(processName, new ProcessUpdate()
        {
            HasError = true,
            ErrorMessage = errorText,
        });

        return updated ? Ok() : BadRequest();
    }
}