using MarketInsight.Application.Engine;
using MarketInsight.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MarketInsight.Application.Interfaces
{
    public interface IEquityCandleSource
    {
        IAsyncEnumerable<EquityCandle> ReadAllAsync(CancellationToken cancellationToken);
    }
}
