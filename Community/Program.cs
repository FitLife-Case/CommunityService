using System.Text;
using Asp.Versioning;
using Community.Repository;
using Community.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NLog;
using NLog.Web;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("NLog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Logging
    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    // Controllers + Swagger
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Cache
    builder.Services.AddMemoryCache();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
    });

    // JWT Authentication
    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],

                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(
                        builder.Configuration["Jwt:Secret"]!))
            };
        });

    // Authorization
    builder.Services.AddAuthorization();

    // MongoDB
    builder.Services.AddSingleton<IMongoClient>(_ =>
        new MongoClient(
            builder.Configuration["Mongo:ConnectionString"]));

    builder.Services.AddScoped<IMongoDatabase>(provider =>
    {
        var client = provider.GetRequiredService<IMongoClient>();

        return client.GetDatabase(
            builder.Configuration["Mongo:DatabaseName"]);
    });

    // Dependency Injection
    builder.Services.AddScoped<ICommunityRepository, CommunityRepository>();
    builder.Services.AddScoped<ICommunityService, CommunityService>();

    var app = builder.Build();

    // Swagger slået til i både Development og Docker/Production
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseHttpsRedirection();

    // Auth middleware
    app.UseAuthentication();
    app.UseAuthorization();

    // Controllers
    app.MapControllers();

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