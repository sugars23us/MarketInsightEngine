-- 4. EQUITY_CANDLE_DAILY
IF OBJECT_ID('dbo.EQUITY_CANDLE_DAILY','U') IS NOT NULL DROP TABLE dbo.EQUITY_CANDLE_DAILY;
CREATE TABLE dbo.EQUITY_CANDLE_DAILY (
    EquityId         INT            NOT NULL,
    TradingDate      DATE           NOT NULL,
    [Open]           DECIMAL(19,6)  NOT NULL,
    High             DECIMAL(19,6)  NOT NULL,
    Low              DECIMAL(19,6)  NOT NULL,
    [Close]          DECIMAL(19,6)  NOT NULL,
    Volume           BIGINT         NOT NULL,
    Vwap      	     DECIMAL(19,6)  NULL,
    Ats		     BIGINT         NULL,
    InsertedUtc      DATETIME2(0)   NOT NULL DEFAULT SYSUTCDATETIME(),

    CONSTRAINT PK_EQUITY_CANDLE_DAILY PRIMARY KEY (EquityId, TradingDate),
    CONSTRAINT FK_EQUITY_CANDLE_DAILY_EQUITY FOREIGN KEY (EquityId) REFERENCES dbo.EQUITY(EquityId)
);
GO

-- BEST index for daily data
CREATE INDEX IX_EQUITY_CANDLE_DAILY_Latest 
    ON dbo.EQUITY_CANDLE_DAILY (EquityId, TradingDate DESC);
GO