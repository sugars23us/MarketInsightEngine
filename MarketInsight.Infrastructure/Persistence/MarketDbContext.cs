// MarketInsight.Infrastructure/Persistence/MarketDbContext.cs
using MarketInsight.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace MarketInsight.Infrastructure.Persistence
{
    public sealed class MarketDbContext : DbContext
    {
        public MarketDbContext(DbContextOptions<MarketDbContext> options)
            : base(options)
        { }

        // Master equity table
        public DbSet<EquityEntity> Equities => Set<EquityEntity>();

        // Intraday candles
        //public DbSet<EquityCandleEntity> EquityCandles => Set<EquityCandleEntity>();

        //// Daily candles (optional but recommended)
        //public DbSet<EquityCandleDailyEntity> EquityCandleDailies => Set<EquityCandleDailyEntity>();

        //// Indicators
        //public DbSet<EquityIndicatorEntity> EquityIndicators => Set<EquityIndicatorEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // EQUITY table (was STOCK)
            modelBuilder.Entity<EquityEntity>(entity =>
            {
                entity.ToTable("EQUITY", "dbo");
                entity.HasKey(e => e.EquityId);

                entity.Property(e => e.EquityId).HasColumnName("EquityId");
                entity.Property(e => e.Ticker).HasColumnName("Ticker").HasMaxLength(16).IsRequired();
                entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(100);
                entity.Property(e => e.Exchange).HasColumnName("Exchange").HasMaxLength(20);
                entity.Property(e => e.FloatShares).HasColumnName("FloatShares");
                entity.Property(e => e.AvgVolume3M).HasColumnName("AvgVolume3M");
                entity.Property(e => e.MarketCap).HasColumnName("MarketCap");
                entity.Property(e => e.UpdatedUtc).HasColumnName("UpdatedUtc");
            });

            // Add other entity configurations here when ready (EquityCandle, etc.)
            // For now, EF will use conventions — works perfectly
        }
    }
}