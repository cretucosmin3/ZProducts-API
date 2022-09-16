// using System.Collections.Specialized;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using ProductAPI.Models;

namespace ProductAPI.Controllers;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{

    [HttpGet("SetCookie"), Authorize(Roles = "Admin")]
    public IActionResult SetCookie()
    {
        return Ok("Gata gogule");
    }

    [HttpGet("GetUsers")]
    public async Task<List<Person>> Get()
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb+srv://productgod22:SQPWOX4iin1fEyoq@product-cluster.3ydguo2.mongodb.net/?retryWrites=true&w=majority");

        var client = new MongoClient(settings);
        var database = client.GetDatabase("ProductDB");
        var user = database.GetCollection<Person>("Users");

        var list = await user.Find(_ => true)
                              .ToListAsync();

        return list;
    }

    [HttpGet("GetUser")]
    [ProducesResponseType(200, Type = typeof(List<Person>))]
    [ProducesResponseType(400, Type = typeof(IActionResult))]
    public async Task<IActionResult> Get(string UserName)
    {
        var settings = MongoClientSettings.FromConnectionString("mongodb+srv://productgod22:SQPWOX4iin1fEyoq@product-cluster.3ydguo2.mongodb.net/?retryWrites=true&w=majority");

        var client = new MongoClient(settings);
        var database = client.GetDatabase("ProductDB");
        var user = database.GetCollection<Person>("Users");

        var filter = Builders<Person>.Filter.Eq(x => x.Name, UserName);
        var list = await user.Find(filter).ToListAsync();

        if (!list.Any())
        {
            return BadRequest("No such user found");
        }

        // return StatusCode(500);
        return Ok(list);
    }
}

