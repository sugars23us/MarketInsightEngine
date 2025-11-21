using MarketInsight.Application.Engine;
using MarketInsight.Application.Indicators;
using MarketInsight.Application.Interfaces;
using MarketInsight.Application.Services;
using MarketInsight.Infrastructure.Logging;
using MarketInsight.Infrastructure.Persistence;
using MarketInsight.Infrastructure.Streaming;
using MarketInsight.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var hostBuilder = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration))
    .ConfigureServices((ctx, services) =>
    {
        var config = ctx.Configuration;

        // 1. Options
        services.Configure<DatabaseOptions>(config.GetSection(DatabaseOptions.SectionName));
        services.Configure<PolygonOptions>(config.GetSection(PolygonOptions.SectionName));

        // 2. Logging
        services.AddMarketInsightLogging(config);

        // 3. DbContext factory (THIS REGISTER IDbContextFactory<MarketDbContext>)
        services.AddDbContextFactory<MarketDbContext>((sp, options) =>
        {
            var dbOpt = sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            options.UseSqlServer(dbOpt.ConnectionString);
        });

        // 5. Application services (now singleton-safe)
        services.AddSingleton<IEquityRegistry, EquityRegistry>();
        services.AddSingleton<IEquityIndicatorSink, SqlEquityIndicatorWriter>();
        services.AddSingleton<IEquityCandleSink, SqlEquityCandleWriter>();
        services.AddSingleton<IEquityCandleSource, PolygonSocketClient>();

        // 6. Calculators
        services.AddSingleton<IIndicatorCalculator, AtsCalculator>();
        services.AddSingleton<IIndicatorCalculator, FlowCalculator>();
        services.AddSingleton<IIndicatorCalculator, MomentumCalculator>();

        // 7. Engine + Worker
        services.AddSingleton<IndicatorEngine>();
        services.AddHostedService<IngestionWorker>();
    });

        var host = hostBuilder.Build();

        await host.RunAsync();
    }
}