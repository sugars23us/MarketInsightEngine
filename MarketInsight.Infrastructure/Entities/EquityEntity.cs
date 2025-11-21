using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketInsight.Infrastructure.Entities
{
    public class EquityEntity
    {
        public int EquityId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string? Name { get; set; }
        public string? Exchange { get; set; }
        public long? FloatShares { get; set; }
        public long? AvgVolume3M { get; set; }
        public decimal? MarketCap { get; set; }
        public DateTime? UpdatedUtc { get; set; }
    }
}
