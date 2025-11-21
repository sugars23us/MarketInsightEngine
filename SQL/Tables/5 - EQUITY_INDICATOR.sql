-- 5. EQUITY_INDICATOR — one table for all timeframes
IF OBJECT_ID('dbo.EQUITY_INDICATOR','U') IS NOT NULL DROP TABLE dbo.EQUITY_INDICATOR;
CREATE TABLE dbo.EQUITY_INDICATOR (
    EquityId     INT            NOT NULL,
    TimeframeId  TINYINT        NOT NULL,
    TsUtc        DATETIME2(0)   NOT NULL,
    TradingDate  AS CONVERT(date, TsUtc) PERSISTED,  -- computed, not stored
    MetricCode   NVARCHAR(32)   NOT NULL,
    Period       SMALLINT       NOT NULL,
    Value        DECIMAL(19,8)  NOT NULL,
    ParamsJson   NVARCHAR(MAX)  NULL,
    InsertedUtc  DATETIME2(0)   NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_EQUITY_INDICATOR PRIMARY KEY (EquityId, TimeframeId, TsUtc, MetricCode, Period),
    CONSTRAINT FK_EQUITY_INDICATOR_EQUITY FOREIGN KEY (EquityId) REFERENCES dbo.EQUITY(EquityId),
    CONSTRAINT FK_EQUITY_INDICATOR_TIMEFRAME FOREIGN KEY (TimeframeId) REFERENCES dbo.TIMEFRAME(TimeframeId)
);
GO

-- Critical indexes
CREATE INDEX IX_EQUITY_INDICATOR_LatestDaily 
    ON dbo.EQUITY_INDICATOR (TimeframeId, MetricCode, Period, TradingDate DESC)
    INCLUDE (EquityId, Value, TsUtc)
    WHERE TimeframeId >= 5;  -- daily+

CREATE INDEX IX_EQUITY_INDICATOR_Intraday 
    ON dbo.EQUITY_INDICATOR (EquityId, TimeframeId, TsUtc DESC);
GO