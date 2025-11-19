using System;
using System.Threading.Tasks;
using MarketInsight.Application.Engine;
using MarketInsight.Application.Indicators;
using MarketInsight.Application.Services;
using MarketInsight.Infrastructure.Logging;
using MarketInsight.Infrastructure.Persistence;
using MarketInsight.Infrastructure.Services;
using MarketInsight.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

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
        services.AddSingleton<IStockRegistry, StockRegistryEf>();
        services.AddSingleton<IIndicatorSink, SqlIndicatorWriter>();
        services.AddSingleton<IBarSink, SqlBarWriter>();
        services.AddSingleton<IMarketBarSource, PolygonSocketClient>();

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
