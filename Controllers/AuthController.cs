using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;

using ProductAPI.Types;
using ProductAPI.Services;
using ProductAPI.DbContracts;
using MongoDB.Driver;
using ProductAPI.Helpers.Database;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;
using ProductAPI.SinglarRHubs;

namespace ProductAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly IDatabaseService dbService;
    private readonly IHubContext<ProgressHub> _progressHub;
    private readonly int TokenExpiration = 2;

    public AuthController(IConfiguration configuration, IUserService userService, IDatabaseService dbS, IHubContext<ProgressHub> questionHubContext)
    {
        _configuration = configuration;
        _userService = userService;
        _progressHub = questionHubContext;
        dbService = dbS;
    }

    [HttpGet("Details")]
    public async Task<ActionResult<string>> GetMe()
    {
        // var userName = _userService.GetRole();
        await _progressHub.Clients.All.SendAsync("ReceiveMessage", "Server", new MessageData()
        {
            Reason = "None",
            Message = "Hello"
        });

        return Ok("Sent");
    }

    [HttpGet("is-logged"), Authorize]
    public ActionResult<bool> IsLogged()
    {
        return Ok(true);
    }

    [HttpPost("register")]
    public async Task<ActionResult<bool>> Register(RegisterUserDto request)
    {
        var tokensHelper = TokensHelper.WithService(dbService);
        var dbToken = await tokensHelper.GetByAccessCode(request.AccessCode);
        if (dbToken == null)
            return BadRequest("Invalid access code");


        var usersHelper = UsersHelper.WithService(dbService);

        if (usersHelper.EmailExists(request.Email))
            return BadRequest("Email already registered");

        CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);

        var userCreated = await usersHelper.CreateNew(new DbContracts.User()
        {
            Email = request.Email,
            Role = dbToken.Role,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
        });

        // Delete access code from db
        if (userCreated)
            await tokensHelper.Delete(request.AccessCode);

        return Ok(userCreated);
    }

    [HttpPost("login")]
    public async Task<ActionResult<string>> Login(UserDto request)
    {
        var usersHelper = UsersHelper.WithService(dbService);

        var dbUser = await usersHelper.GetUserByEmail(request.Email);

        if (dbUser == null)
            return BadRequest("Incorrect login details.");

        var passwordMatch = VerifyPasswordHash(request.Password, dbUser.PasswordHash, dbUser.PasswordSalt);

        if (dbUser.Email != request.Email || passwordMatch == false)
            return BadRequest("Incorrect login details.");

        string token = CreateToken(dbUser);

        var refreshToken = GenerateRefreshToken();

        dbUser.RefreshToken = refreshToken.Token;
        dbUser.TokenCreated = refreshToken.Created;
        dbUser.TokenExpires = refreshToken.Expires;

        var updated = await usersHelper.UpdateOne(dbUser);

        if (updated == false)
            return BadRequest("Internal error");

        SetRefreshToken(refreshToken);

        return Ok(token);
    }

    [HttpGet("refresh-token")]
    public async Task<ActionResult<AuthRefresh>> RefreshToken()
    {
        var identifier = HttpContext.TraceIdentifier;
        HttpContext.Request.Cookies.TryGetValue("refresh-token", out string refreshToken);

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("Auth token not present");

        var usersHelper = UsersHelper.WithService(dbService);

        var dbUser = await usersHelper.FindUserWithToken(refreshToken);

        if (dbUser == null)
        {
            return Unauthorized("Invalid request");
        }

        string token = CreateToken(dbUser);

        if (dbUser.TokenExpires < DateTime.Now)
        {
            var newRefreshToken = GenerateRefreshToken();
            SetRefreshToken(newRefreshToken);

            dbUser.RefreshToken = newRefreshToken.Token;
            dbUser.TokenCreated = newRefreshToken.Created;
            dbUser.TokenExpires = newRefreshToken.Expires;

            var updated = await usersHelper.UpdateOne(dbUser);

            if (updated == false)
                return BadRequest("Internal error");
        }

        return Ok(new AuthRefresh()
        {
            RefreshToken = token
        });
    }

    private RefreshToken GenerateRefreshToken()
    {
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
            Expires = DateTime.Now.AddMonths(3),
            Created = DateTime.Now
        };

        return refreshToken;
    }

    private void SetRefreshToken(RefreshToken newRefreshToken)
    {
        var cookieOptions = new CookieOptions()
        {
            Secure = false,
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Unspecified,
            Path = "/",
            Expires = DateTimeOffset.Now.AddDays(90),
            MaxAge = TimeSpan.FromDays(90),
        };

        Response.Cookies.Append("refresh-token", newRefreshToken.Token, cookieOptions);
    }

    private string CreateToken(User user)
    {
        List<Claim> claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString()),
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
            _configuration.GetSection("Jwt:SecretKey").Value));

        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.Now.AddDays(30), // seconds for testing
            signingCredentials: creds);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);

        return jwt;
    }

    private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512())
        {
            passwordSalt = hmac.Key;
            passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        }
    }

    private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
    {
        using (var hmac = new HMACSHA512(passwordSalt))
        {
            var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return computedHash.SequenceEqual(passwordHash);
        }
    }
}

public class AuthRefresh
{
    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; } = default!;
}

public class MessageData
{
    public string Reason { get; set; }
    public string Message { get; set; }
}