using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketInsight.Infrastructure.Persistence
{
    public sealed class StockEntity
    {
        public int StockId { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public short? Exchange { get; set; }
        public long? FloatShares { get; set; }
        public long? Adv63 { get; set; }
    }
}
