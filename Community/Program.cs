using System.Security.Claims;
using System.Text;
using Asp.Versioning;
using Community.Repository;
using Community.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NLog;
using NLog.Web;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("NLog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    var vaultUrl = Environment.GetEnvironmentVariable("Vault__Url")
               ?? throw new Exception("Vault__Url mangler");
    var vaultToken = Environment.GetEnvironmentVariable("Vault__Token")
                     ?? throw new Exception("Vault__Token mangler");

    var vaultClient = new VaultClient(
        new VaultClientSettings(vaultUrl, new TokenAuthMethodInfo(vaultToken)));

    IDictionary<string, object> secrets = null!;

    for (int i = 0; i < 10; i++)
    {
        try
        {
            var vaultSecrets = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
                path: "communityservice",
                mountPoint: "secret");

            secrets = vaultSecrets.Data.Data;
            break;
        }
        catch
        {
            logger.Warn("Vault not ready, retrying in 3 seconds... attempt {0}/10", i + 1);
            await Task.Delay(3000);
        }
    }

    if (secrets == null)
        throw new Exception("Could not connect to Vault after 10 attempts");

    var mongoConnectionString = secrets["Mongo__ConnectionString"].ToString()!;
    var mongoDatabaseName = secrets["Mongo__DatabaseName"].ToString()!;
    var jwtSecret = secrets["Jwt__Secret"].ToString()!;
    var jwtIssuer = secrets["Jwt__Issuer"].ToString()!;
    var jwtAudience = secrets["Jwt__Audience"].ToString()!;

    builder.Services.AddControllers();
    builder.Services.AddRazorPages();
    builder.Services.AddHttpClient();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddMemoryCache();

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token =
                        context.Request.Cookies["JwtToken"]
                        ?? context.Request.Cookies["jwt"]
                        ?? context.Request.Cookies["access_token"];

                    if (!string.IsNullOrWhiteSpace(token))
                        context.Token = token;

                    return Task.CompletedTask;
                },

                OnTokenValidated = context =>
                {
                    if (context.Principal?.Identity is not ClaimsIdentity identity)
                        return Task.CompletedTask;

                    var role = context.Request.Cookies["role"];
                    var username = context.Request.Cookies["username"];
                    var memberId = context.Request.Cookies["memberId"];

                    if (!string.IsNullOrWhiteSpace(role) &&
                        !identity.HasClaim(c => c.Type == "role"))
                    {
                        identity.AddClaim(new Claim("role", role));
                    }

                    if (!string.IsNullOrWhiteSpace(role) &&
                        !identity.HasClaim(c => c.Type == ClaimTypes.Role))
                    {
                        identity.AddClaim(new Claim(ClaimTypes.Role, role));
                    }

                    if (!string.IsNullOrWhiteSpace(username) &&
                        !identity.HasClaim(c => c.Type == "username"))
                    {
                        identity.AddClaim(new Claim("username", username));
                    }

                    if (!string.IsNullOrWhiteSpace(memberId) &&
                        !identity.HasClaim(c => c.Type == "memberId"))
                    {
                        identity.AddClaim(new Claim("memberId", memberId));
                    }

                    return Task.CompletedTask;
                }
            };

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSecret)),

                RoleClaimType = "role",
                NameClaimType = "username"
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddSingleton<IMongoClient>(_ =>
        new MongoClient(mongoConnectionString));

    builder.Services.AddScoped<IMongoDatabase>(provider =>
    {
        var client = provider.GetRequiredService<IMongoClient>();
        return client.GetDatabase(mongoDatabaseName);
    });

    builder.Services.AddScoped<ICommunityRepository, CommunityRepository>();
    builder.Services.AddScoped<ICommunityService, CommunityService>();

    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapRazorPages();

    app.Run();
}
catch (Exception ex)
{
    logger.Error(ex, "Application stopped because of exception");
    throw;
}
finally
{
    LogManager.Shutdown();
}