USE [MarketInsight]
GO

-- 2. TVP for EQUITY_INDICATOR table
CREATE TYPE dbo.EquityIndicatorTvp AS TABLE
(
    EquityId     INT            NOT NULL,
    TimeframeId  TINYINT        NOT NULL,
    TsUtc        DATETIME2(0)   NOT NULL,   -- required for PK
    MetricCode   NVARCHAR(32)   NOT NULL,
    Period       SMALLINT       NOT NULL,
    Value        DECIMAL(19,8)  NOT NULL,
    ParamsJson   NVARCHAR(MAX)  NULL
    -- TradingDate removed — now computed automatically from TsUtc
);
GO


