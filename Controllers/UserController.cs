using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

    [HttpGet("islogged"), Authorize]
    public ActionResult<bool> IsLogged()
    {
        return Ok(true);
    }
}
