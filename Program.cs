// Globals
global using ProductAPI.Services.UserService;
global using System.Runtime.Serialization;
global using MongoDB.Bson;
global using MongoDB.Bson.Serialization.Attributes;

// Locals
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ProductAPI.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using ProductAPI.SinglarRHubs;
using Microsoft.AspNetCore.ResponseCompression;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IDatabaseService, DatabaseService>();
        builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));
        builder.Services.AddSignalR();

        builder.Services.AddResponseCompression(opts =>
        {
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                new[] { "application/octet-stream" });
        });

        // builder.Services.AddSwaggerGen();
        // builder.Services.AddSwaggerGen(options =>
        // {
        //     options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        //     {
        //         Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        //         In = ParameterLocation.Header,
        //         Name = "Authorization",
        //         Type = SecuritySchemeType.ApiKey
        //     });

        //     options.OperationFilter<SecurityRequirementsOperationFilter>();
        // });

        var Configuration = builder.Configuration;
        var SecretKey = builder.Configuration.GetSection("Jwt:SecretKey").Value;
        var Issuer = builder.Configuration.GetSection("Jwt:Issuer").Value;
        var Audience = builder.Configuration.GetSection("Jwt:Audience").Value;

        // Console.WriteLine("::Config::");
        // Console.WriteLine($"SecretKey   : {SecretKey}");
        // Console.WriteLine($"Issuer      : {Issuer}");
        // Console.WriteLine($"Audience    : {Audience}");

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8
                        .GetBytes(SecretKey)),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                };
            });

        builder.Services.AddCors(options => options.AddPolicy(name: "NgOrigins",
            policy =>
            {
                // policy.WithOrigins("https://localhost:7058").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
                policy.WithOrigins("https://localhost:7058").AllowAnyMethod().AllowAnyHeader().AllowCredentials();
            }));

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
        });

        builder.Services.Configure<SecurityStampValidatorOptions>(
            o => o.ValidationInterval = TimeSpan.FromDays(365)
        );

        builder.Services.AddDataProtection()
                        .PersistKeysToFileSystem(new System.IO.DirectoryInfo("cookie-storage"))
                        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

        builder.Services.AddCookiePolicy(options =>
        {
            options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
            options.CheckConsentNeeded = context => false;
            options.Secure = CookieSecurePolicy.Always;
            options.MinimumSameSitePolicy = SameSiteMode.Lax;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            // app.UseSwagger();
            // app.UseSwaggerUI();

            // app.UseDeveloperExceptionPage();
            // app.UseCookiePolicy();
        }

        app.UseResponseCompression();
        app.UseCors("NgOrigins");
        app.UseCookiePolicy();
        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHub<ProgressHub>("/progress-hub");
        app.Run();
    }
}