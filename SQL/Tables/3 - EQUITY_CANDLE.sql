-- 3. EQUITY_CANDLE — intraday
IF OBJECT_ID('dbo.EQUITY_CANDLE','U') IS NOT NULL DROP TABLE dbo.EQUITY_CANDLE;
CREATE TABLE dbo.EQUITY_CANDLE (
    EquityId         INT            NOT NULL,
    TimeframeId      TINYINT        NOT NULL,
    TsUtc            DATETIME2(0)   NOT NULL,
    [Open]           DECIMAL(19,6)  NOT NULL,
    High             DECIMAL(19,6)  NOT NULL,
    Low              DECIMAL(19,6)  NOT NULL,
    [Close]          DECIMAL(19,6)  NOT NULL,
    Volume           BIGINT         NOT NULL,
    Vwap      	     DECIMAL(19,6)  NULL,
    Ats 	     BIGINT         NULL,
    TradingDate      AS CONVERT(date, TsUtc) PERSISTED,
    InsertedUtc      DATETIME2(0)   NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_EQUITY_CANDLE PRIMARY KEY CLUSTERED (EquityId, TimeframeId, TsUtc),
    CONSTRAINT FK_EQUITY_CANDLE_EQUITY FOREIGN KEY (EquityId) REFERENCES dbo.EQUITY(EquityId),
    CONSTRAINT FK_EQUITY_CANDLE_TIMEFRAME FOREIGN KEY (TimeframeId) REFERENCES dbo.TIMEFRAME(TimeframeId)
);
GO


CREATE INDEX IX_EQUITY_CANDLE_Day_Symbol     ON dbo.EQUITY_CANDLE (TradingDate, EquityId, TimeframeId, TsUtc);
CREATE INDEX IX_EQUITY_CANDLE_Latest         ON dbo.EQUITY_CANDLE (EquityId, TimeframeId, TsUtc DESC);
CREATE INDEX IX_EQUITY_CANDLE_1m_Latest      ON dbo.EQUITY_CANDLE (EquityId, TsUtc DESC) WHERE TimeframeId = 1;
GO