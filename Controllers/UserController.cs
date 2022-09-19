// using System.Collections.Specialized;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ProductAPI.Services;

namespace ProductAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly IDatabaseService dbService;

    public UserController(IDatabaseService dbS)
    {
        dbService = dbS;
    }

    [HttpGet("islogged"), Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public ActionResult<bool> IsLogged()
    {
        return Ok(true);
    }
}
