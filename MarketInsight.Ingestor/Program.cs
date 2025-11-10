using MarketInsight.Ingestor.AppOptions;
using MarketInsight.Ingestor.Data;
using MarketInsight.Ingestor.Ingestion;
using Microsoft.Extensions.Options;
using Serilog;

var builder = Host.CreateDefaultBuilder(args)
    // .UseSystemd()  // <- remove/comment on Windows
    .UseWindowsService()
    .ConfigureAppConfiguration(cfg =>
    {
        cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .AddEnvironmentVariables();
    })
    .UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration))
    .ConfigureServices((ctx, services) =>
    {
        services.Configure<PolygonOptions>(ctx.Configuration.GetSection("Polygon"));
        services.Configure<DatabaseOptions>(ctx.Configuration.GetSection("Database"));
        services.Configure<IngestionOptions>(ctx.Configuration.GetSection("Ingestion"));

        services.AddSingleton<StockRegistry>();
        services.AddSingleton<SqlBarWriter>();
        services.AddSingleton<PolygonSocketClient>();
        services.AddSingleton<SqlIndicatorWriter>();
        services.AddHostedService<IngestionWorker>();
    });

await builder.Build().RunAsync();
