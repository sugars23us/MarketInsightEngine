
USE MarketInsight;
GO

-- Recommended DB options (run once)
ALTER DATABASE CURRENT SET RECOVERY SIMPLE;
ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON;
ALTER DATABASE CURRENT SET ALLOW_SNAPSHOT_ISOLATION ON;
ALTER DATABASE CURRENT SET AUTO_UPDATE_STATISTICS_ASYNC ON;
ALTER DATABASE CURRENT SET PAGE_VERIFY CHECKSUM;
ALTER DATABASE CURRENT SET QUERY_STORE = ON;
ALTER DATABASE CURRENT SET QUERY_STORE (OPERATION_MODE = READ_WRITE);
GO


/* ======================================================================
   1) Lookup tables: EXCHANGE, TIMEFRAME
   ====================================================================== */

IF OBJECT_ID('dbo.EXCHANGE','U') IS NOT NULL DROP TABLE dbo.EXCHANGE;
CREATE TABLE dbo.EXCHANGE (
    ExchangeId   SMALLINT       NOT NULL PRIMARY KEY,
    Code         NVARCHAR(16)   NOT NULL UNIQUE,   -- e.g., NASDAQ, NYSE
    Name         NVARCHAR(128)  NOT NULL
);

IF OBJECT_ID('dbo.TIMEFRAME','U') IS NOT NULL DROP TABLE dbo.TIMEFRAME;
CREATE TABLE dbo.TIMEFRAME (
    TimeframeId  TINYINT        NOT NULL PRIMARY KEY,  -- 1..255
    Code         NVARCHAR(8)    NOT NULL UNIQUE,       -- '1m','5m','15m','1h','1d','1w'
    Minutes      SMALLINT       NOT NULL               -- 1,5,15,60,1440,10080
);

-- Seed common timeframes
MERGE dbo.TIMEFRAME AS t
USING (VALUES
  (1,    N'1m',   1),
  (5,    N'5m',   5),
  (15,   N'15m',  15),
  (60,   N'1h',   60),
  (100,  N'1d',   1440),
  (200,  N'1w',   10080)
) AS s(TimeframeId, Code, Minutes)
ON (t.TimeframeId = s.TimeframeId)
WHEN NOT MATCHED THEN INSERT (TimeframeId, Code, Minutes) VALUES (s.TimeframeId, s.Code, s.Minutes);
GO


/* ======================================================================
   2) Users (multi-tenant ready) and STOCK master
   ====================================================================== */

IF OBJECT_ID('dbo.APP_USER','U') IS NOT NULL DROP TABLE dbo.APP_USER;
CREATE TABLE dbo.APP_USER (
    UserId       INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Email        NVARCHAR(256)  NOT NULL UNIQUE,
    DisplayName  NVARCHAR(128)  NULL,
    CreatedUtc   DATETIME2(0)   NOT NULL DEFAULT (SYSUTCDATETIME())
);

IF OBJECT_ID('dbo.STOCK','U') IS NOT NULL DROP TABLE dbo.STOCK;
CREATE TABLE dbo.STOCK (
    StockId      INT            IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Ticker       NVARCHAR(16)   NOT NULL UNIQUE,     -- e.g., MSFT
    Name         NVARCHAR(128)  NULL,
    ExchangeId   SMALLINT       NULL,
    IsActive     BIT            NOT NULL DEFAULT(1),
    CreatedUtc   DATETIME2(0)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_STOCK_EXCHANGE FOREIGN KEY (ExchangeId) REFERENCES dbo.EXCHANGE(ExchangeId)
);
GO




/* ======================================================================
   3) BAR (OHLCV for ALL timeframes)
      - One row per (StockId, TimeframeId, TsUtc)
      - TsUtc = Polygon bar end (UTC) / official market time
   ====================================================================== */

IF OBJECT_ID('dbo.BAR','U') IS NOT NULL DROP TABLE dbo.BAR;
CREATE TABLE dbo.BAR (
    StockId       INT            NOT NULL,
    TimeframeId   TINYINT        NOT NULL,             -- FK to TIMEFRAME
    TsUtc         DATETIME2(0)   NOT NULL,             -- bar close time (UTC)
    [Open]          DECIMAL(19,6)  NOT NULL,
    High          DECIMAL(19,6)  NOT NULL,
    Low           DECIMAL(19,6)  NOT NULL,
    [Close]         DECIMAL(19,6)  NOT NULL,
    Volume        BIGINT         NOT NULL,
    Vwap          DECIMAL(19,6)  NULL,
    TradeCount    INT            NULL,
    TradingDate   AS CONVERT(date, TsUtc) PERSISTED,   -- handy for day grouping
    InsertedUtc   DATETIME2(0)   NOT NULL DEFAULT (SYSUTCDATETIME()), -- ops metric
    CONSTRAINT PK_BAR PRIMARY KEY CLUSTERED (StockId, TimeframeId, TsUtc),
    CONSTRAINT FK_BAR_STOCK      FOREIGN KEY (StockId)     REFERENCES dbo.STOCK(StockId),
    CONSTRAINT FK_BAR_TIMEFRAME  FOREIGN KEY (TimeframeId) REFERENCES dbo.TIMEFRAME(TimeframeId),
    CONSTRAINT CK_BAR_PriceNonNeg CHECK ([Open] >= 0 AND High >= 0 AND Low >= 0 AND [Close] >= 0),
    CONSTRAINT CK_BAR_VolumeNonNeg CHECK (Volume >= 0)
);

-- Fast "show me day’s intraday candles"
CREATE INDEX IX_BAR_Day_Symbol
    ON dbo.BAR (TradingDate, StockId, TimeframeId, TsUtc);

-- Fast "latest N bars" per symbol/timeframe
CREATE INDEX IX_BAR_Symbol_Tf_TsDesc
    ON dbo.BAR (StockId, TimeframeId, TsUtc DESC);

-- Optional: filtered index for hot 1m path (TimeframeId = 1)
CREATE INDEX IX_BAR_1m_Symbol_TsDesc
    ON dbo.BAR (StockId, TsUtc DESC)
    WHERE TimeframeId = 1;
GO


/* ======================================================================
   4) INDICATOR_VALUE (generic indicators for ANY timeframe)
      - One row per (StockId, TimeframeId, TsUtc, MetricCode, Period)
      - Composite FK ensures indicator belongs to an existing bar
   ====================================================================== */

IF OBJECT_ID('dbo.INDICATOR_VALUE','U') IS NOT NULL DROP TABLE dbo.INDICATOR_VALUE;
CREATE TABLE dbo.INDICATOR_VALUE (
    StockId      INT            NOT NULL,
    TimeframeId  TINYINT        NOT NULL,
    TsUtc        DATETIME2(0)   NOT NULL,             -- align to BAR.TsUtc
    MetricCode   NVARCHAR(24)   NOT NULL,             -- 'SMA','EMA','RSI','MACD','VWAP',...
    Period       SMALLINT       NOT NULL,                 -- e.g., 20 for SMA20; NULL if N/A
    ParamsJson   NVARCHAR(256)  NULL,                 -- e.g., {"fast":12,"slow":26,"signal":9}
    Value        DECIMAL(19,8)  NOT NULL,
    UpdatedUtc   DATETIME2(0)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_INDICATOR_VALUE PRIMARY KEY CLUSTERED(StockId, TimeframeId, TsUtc, MetricCode, Period),
    CONSTRAINT FK_IV_STOCK     FOREIGN KEY (StockId)     REFERENCES dbo.STOCK(StockId),
    CONSTRAINT FK_IV_TIMEFRAME FOREIGN KEY (TimeframeId) REFERENCES dbo.TIMEFRAME(TimeframeId)
);

-- FK to the BAR row for the same (StockId, TimeframeId, TsUtc)
-- (You may temporarily disable this during large backfills if needed.)
ALTER TABLE dbo.INDICATOR_VALUE
ADD CONSTRAINT FK_IV_BAR
FOREIGN KEY (StockId, TimeframeId, TsUtc)
REFERENCES dbo.BAR (StockId, TimeframeId, TsUtc);


-- optimized “latest per metric” index
CREATE INDEX IX_IV_Latest_ByMetric
ON dbo.INDICATOR_VALUE (StockId, TimeframeId, MetricCode, TsUtc DESC)
INCLUDE (Period, Value, UpdatedUtc, ParamsJson);
GO


CREATE UNIQUE INDEX UQ_INDICATOR_VALUE_FinalKey
  ON dbo.INDICATOR_VALUE (StockId, TimeframeId, TsUtc, MetricCode);
GO



-- optimized for time-series by stock (range on TsUtc)
CREATE INDEX IX_IV_Min1_ByStockTime
ON dbo.INDICATOR_VALUE (StockId, TimeframeId, TsUtc DESC, MetricCode)
INCLUDE (Period, Value)
-- Optional filtered variant if you mostly query minute data:
WHERE TimeframeId = 1




/* ======================================================================
   5) DAILY_STATS / rollups (for unusual volume baselines, ATR, etc.)
   ====================================================================== */

IF OBJECT_ID('dbo.DAILY_STATS','U') IS NOT NULL DROP TABLE dbo.DAILY_STATS;
CREATE TABLE dbo.DAILY_STATS (
    StockId        INT            NOT NULL,
    TradeDate      DATE           NOT NULL,
    ADV            BIGINT         NULL,                -- avg daily volume
    VolProfileJson NVARCHAR(MAX)  NULL,                -- mean/std per minute-of-day
    ATR            DECIMAL(19,6)  NULL,
    UpdatedUtc     DATETIME2(0)   NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT PK_DAILY_STATS PRIMARY KEY CLUSTERED (StockId, TradeDate),
    CONSTRAINT FK_DS_STOCK FOREIGN KEY (StockId) REFERENCES dbo.STOCK(StockId)
);
GO


/* ======================================================================
   6) Rules, alert events, notifications
   ====================================================================== */

IF OBJECT_ID('dbo.ALERT_RULE','U') IS NOT NULL DROP TABLE dbo.ALERT_RULE;
CREATE TABLE dbo.ALERT_RULE (
    RuleId       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UserId       INT              NOT NULL,
    StockId      INT              NULL,               -- null = multi-target via ExprJson
    Name         NVARCHAR(128)    NOT NULL,
    ExprJson     NVARCHAR(MAX)    NOT NULL,           -- JSON DSL for rule
    IsEnabled    BIT              NOT NULL DEFAULT(1),
    CreatedUtc   DATETIME2(0)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    UpdatedUtc   DATETIME2(0)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_RULE_USER  FOREIGN KEY (UserId)  REFERENCES dbo.APP_USER(UserId),
    CONSTRAINT FK_RULE_STOCK FOREIGN KEY (StockId) REFERENCES dbo.STOCK(StockId)
);

-- Examples for ExprJson:
-- {"type":"cross","left":{"metric":"close","tf":"1d"},"right":{"metric":"SMA","period":20,"tf":"1d"},"dir":"above"}
-- {"type":"threshold","metric":"price","tf":"1m","op":">=","value":250.00}

IF OBJECT_ID('dbo.ALERT_EVENT','U') IS NOT NULL DROP TABLE dbo.ALERT_EVENT;
CREATE TABLE dbo.ALERT_EVENT (
    AlertId      UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    RuleId       UNIQUEIDENTIFIER NOT NULL,
    StockId      INT              NOT NULL,
    TriggerTsUtc DATETIME2(0)     NOT NULL,        -- bar/event time that triggered
    State        TINYINT          NOT NULL,        -- 1=Triggered, 2=Recovered (extend as needed)
    Strength     DECIMAL(9,4)     NULL,
    PayloadJson  NVARCHAR(MAX)    NULL,            -- snapshot of values used
    DedupeKey    NVARCHAR(256)    NOT NULL,        -- e.g., "MSFT|1d|crossUpSMA20|2025-10-24"
    CreatedUtc   DATETIME2(0)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT UQ_ALERT_EVENT_Dedupe UNIQUE (DedupeKey),
    CONSTRAINT FK_AE_RULE  FOREIGN KEY (RuleId)  REFERENCES dbo.ALERT_RULE(RuleId),
    CONSTRAINT FK_AE_STOCK FOREIGN KEY (StockId) REFERENCES dbo.STOCK(StockId)
);

CREATE INDEX IX_AE_Symbol_Time ON dbo.ALERT_EVENT (StockId, TriggerTsUtc DESC);

IF OBJECT_ID('dbo.NOTIFICATION','U') IS NOT NULL DROP TABLE dbo.NOTIFICATION;
CREATE TABLE dbo.NOTIFICATION (
    NotificationId  BIGINT           IDENTITY(1,1) NOT NULL PRIMARY KEY,
    AlertId         UNIQUEIDENTIFIER NOT NULL,
    UserId          INT              NOT NULL,
    Channel         TINYINT          NOT NULL,     -- 1=Telegram, 2=Email, 3=Webhook, ...
    Status          TINYINT          NOT NULL,     -- 0=Pending, 1=Sent, 2=Failed
    Attempts        INT              NOT NULL DEFAULT(0),
    LastAttemptUtc  DATETIME2(0)     NULL,
    Payload         NVARCHAR(MAX)    NULL,
    CreatedUtc      DATETIME2(0)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    CONSTRAINT FK_N_ALERT FOREIGN KEY (AlertId) REFERENCES dbo.ALERT_EVENT(AlertId),
    CONSTRAINT FK_N_USER  FOREIGN KEY (UserId)  REFERENCES dbo.APP_USER(UserId)
);

CREATE INDEX IX_NOTIFICATION_Status ON dbo.NOTIFICATION (Status, CreatedUtc);
GO


/* ======================================================================
   7) Simple per-user price watches (threshold alerts)
   ====================================================================== */

IF OBJECT_ID('dbo.PRICE_WATCH','U') IS NOT NULL DROP TABLE dbo.PRICE_WATCH;
CREATE TABLE dbo.PRICE_WATCH (
    WatchId        UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UserId         INT              NOT NULL,
    StockId        INT              NOT NULL,
    Direction      TINYINT          NOT NULL,      -- 1=CrossAbove, 2=CrossBelow, 3=>=, 4=<=
    Threshold      DECIMAL(19,6)    NOT NULL,
    TimeframeId    TINYINT          NOT NULL,      -- aligns with TIMEFRAME
    IsEnabled      BIT              NOT NULL DEFAULT(1),
    CreatedUtc     DATETIME2(0)     NOT NULL DEFAULT (SYSUTCDATETIME()),
    LastTriggeredUtc DATETIME2(0)   NULL,
    CONSTRAINT FK_PW_USER      FOREIGN KEY (UserId)      REFERENCES dbo.APP_USER(UserId),
    CONSTRAINT FK_PW_STOCK     FOREIGN KEY (StockId)     REFERENCES dbo.STOCK(StockId),
    CONSTRAINT FK_PW_TIMEFRAME FOREIGN KEY (TimeframeId) REFERENCES dbo.TIMEFRAME(TimeframeId)
);
GO


/* ======================================================================
   8) Convenience seeds (optional)
   ====================================================================== */

-- Add some exchanges
IF NOT EXISTS (SELECT 1 FROM dbo.EXCHANGE WHERE ExchangeId = 1)
INSERT dbo.EXCHANGE (ExchangeId, Code, Name)
VALUES (1,'NASDAQ','NASDAQ'), (2,'NYSE','New York Stock Exchange');

-- Add yourself as a user example
IF NOT EXISTS (SELECT 1 FROM dbo.APP_USER WHERE Email = 'thomas.sioungaris@gmail.com')
INSERT dbo.APP_USER (Email, DisplayName) VALUES ('thomas.sioungaris@gmail.com','Thomas');

-- Add a few stocks
IF NOT EXISTS (SELECT 1 FROM dbo.STOCK WHERE Ticker='MSFT')
INSERT dbo.STOCK (Ticker, Name, ExchangeId) VALUES
('MSFT','Microsoft',1), ('AAPL','Apple',1), ('TSLA','Tesla',1);
GO
