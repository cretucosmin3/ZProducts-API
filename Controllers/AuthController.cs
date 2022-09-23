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

namespace ProductAPI.Controllers;

[Route("[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IUserService _userService;
    private readonly IDatabaseService dbService;
    private readonly int TokenExpiration = 2;

    public AuthController(IConfiguration configuration, IUserService userService, IDatabaseService dbS)
    {
        _configuration = configuration;
        _userService = userService;
        dbService = dbS;
    }

    [HttpGet("Details"), Authorize]
    public ActionResult<string> GetMe()
    {
        var userName = _userService.GetMyName();
        return Ok(userName);
    }

    [HttpPost("register")]
    public async Task<ActionResult<bool>> Register(RegisterUserDto request)
    {
        var tokensHelper = TokensHelper.WithService(dbService);
        var dbToken = await tokensHelper.GetByAccessCode(request.AccessCode);
        if (dbToken == null)
            return BadRequest("Invalid access code");


        var usersHelper = UsersHelper.WithService(dbService);

        if (await usersHelper.EmailExists(request.Email))
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
        HttpContext.Session.SetString("keyname", "Testing");

        Console.WriteLine("-- Login");
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
        Console.WriteLine("-- Refreshing token");
        var refreshToken = HttpContext.Request.Cookies["refreshToken"];

        Console.WriteLine($"Cookie exists: {refreshToken}");

        if (string.IsNullOrEmpty(refreshToken))
            return BadRequest("Auth token not present");

        var usersHelper = UsersHelper.WithService(dbService);

        var dbUser = await usersHelper.FindUserWithToken(refreshToken);

        if (dbUser == null)
        {
            Console.WriteLine($"User doesn't exist, token {refreshToken.Take(8)}...");
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

        // return Ok(token);
        return Ok(new AuthRefresh()
        {
            RefreshToken = token
        });
    }

    private RefreshToken GenerateRefreshToken()
    {
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            Expires = DateTime.Now.AddMonths(3),
            Created = DateTime.Now
        };

        return refreshToken;
    }

    private void SetRefreshToken(RefreshToken newRefreshToken)
    {
        // var cookieOptions = new CookieOptions
        // {
        //     HttpOnly = true,
        //     Expires = newRefreshToken.Expires
        // };

        // var cookieOptions = new CookieOptions
        // {
        //     Path = "/",
        //     Domain = "localhost",
        //     Expires = DateTime.UtcNow.AddHours(6),
        //     HttpOnly = true,
        //     Secure = true,
        // };

        var cookieOptions = new CookieOptions()
        {
            HttpOnly = true,
            IsEssential = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = "/",
            Domain = Response.HttpContext.Request.Host.Value, //using https://localhost:44340/ here doesn't work
            Expires = DateTimeOffset.Now.AddDays(90),
            MaxAge = TimeSpan.FromDays(90),
        };

        Console.WriteLine($"Setting refreshToken into cookie {newRefreshToken.Token}");
        Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);
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
            expires: DateTime.Now.AddSeconds(20), // seconds for testing
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