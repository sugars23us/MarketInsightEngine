using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MarketInsight.Infrastructure.Persistence
{
    public sealed class MarketDbContext : DbContext
    {
        public MarketDbContext(DbContextOptions<MarketDbContext> options) : base(options) { }
        public DbSet<StockEntity> Stocks => Set<StockEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<StockEntity>(builder =>
            {
                builder.ToTable("STOCK", "dbo");
                builder.HasKey(x => x.StockId);

                builder.Property(x => x.StockId).HasColumnName("StockId");
                builder.Property(x => x.Ticker).HasColumnName("Ticker");
                builder.Property(x => x.Exchange).HasColumnName("ExchangeId");
                builder.Property(x => x.FloatShares).HasColumnName("FloatShares");
                builder.Property(x => x.Adv63).HasColumnName("Adv63");
            });
        }
    }

    public sealed class StockEntity
    {
        public int StockId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string? Exchange { get; set; }
        public long? FloatShares { get; set; }
        public long? Adv63 { get; set; }
    }
}
