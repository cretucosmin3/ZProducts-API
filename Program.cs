// Globals
global using ProductAPI.Services.UserService;
global using System.Runtime.Serialization;
global using MongoDB.Bson;
global using MongoDB.Bson.Serialization.Attributes;

// Locals
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Filters;
using System.Text;
using ProductAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddHttpContextAccessor();
builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));

builder.Services.AddSwaggerGen();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });

    options.OperationFilter<SecurityRequirementsOperationFilter>();
});

var Configuration = builder.Configuration;
var SecretKey = builder.Configuration.GetSection("Jwt:SecretKey").Value;
var Issuer = builder.Configuration.GetSection("Jwt:Issuer").Value;
var Audience = builder.Configuration.GetSection("Jwt:Audience").Value;

Console.WriteLine("::Config::");
Console.WriteLine($"SecretKey   : {SecretKey}");
Console.WriteLine($"Issuer      : {Issuer}");
Console.WriteLine($"Audience    : {Audience}");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                .GetBytes(SecretKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

// builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//     .AddJwtBearer(options =>
//     {
//         options.RequireHttpsMetadata = false;
//         options.SaveToken = true;
//         options.TokenValidationParameters = new TokenValidationParameters
//         {
//             ValidateIssuer = true,
//             ValidateAudience = true,
//             ValidateLifetime = true,
//             ValidateIssuerSigningKey = true,
//             ValidIssuer = Issuer,
//             ValidAudience = Audience,
//             IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey)),
//             ClockSkew = TimeSpan.Zero
//         };
//     });

builder.Services.AddCors(options => options.AddPolicy(name: "NgOrigins",
    policy =>
    {
        policy.WithOrigins("http://localhost:7056").AllowAnyMethod().AllowAnyHeader();
    }));

// builder.Services.Configure<CookiePolicyOptions>(options =>
//     {
//         options.CheckConsentNeeded = context => false;
//         options.Secure = CookieSecurePolicy.Always;
//         options.MinimumSameSitePolicy = SameSiteMode.Lax;
//     });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseDeveloperExceptionPage();
    app.UseCors(options => options
        // .WithOrigins("http://localhost:3000")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials());
    app.UseCookiePolicy();
}

app.UseCors("NgOrigins");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();