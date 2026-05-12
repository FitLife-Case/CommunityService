using Asp.Versioning;
using Community.Repository;
using Community.Service;
using MongoDB.Driver;
using NLog;
using NLog.Web;

var logger = LogManager.Setup()
    .LoadConfigurationFromFile("NLog.config")
    .GetCurrentClassLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();

    builder.Services.AddControllers();

    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddSwaggerGen();

    builder.Services.AddMemoryCache();

    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);

        options.AssumeDefaultVersionWhenUnspecified = true;

        options.ReportApiVersions = true;
    });

    builder.Services.AddSingleton<IMongoClient>(_ =>
        new MongoClient(
            builder.Configuration["Mongo:ConnectionString"]));

    builder.Services.AddScoped<IMongoDatabase>(provider =>
    {
        var client = provider.GetRequiredService<IMongoClient>();

        return client.GetDatabase(
            builder.Configuration["Mongo:DatabaseName"]);
    });

    builder.Services.AddScoped<ICommunityRepository, CommunityRepository>();

    builder.Services.AddScoped<ICommunityService, CommunityService>();

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();

        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseAuthorization();

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