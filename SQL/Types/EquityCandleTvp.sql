USE [MarketInsight]
GO

-- 1. TVP for EQUITY_CANDLE table
CREATE TYPE dbo.EquityCandleTvp AS TABLE
(
    EquityId         INT            NOT NULL,
    TimeframeId      TINYINT        NOT NULL,
    TsUtc            DATETIME2(0)   NOT NULL,
    [Open]           DECIMAL(19,6)  NOT NULL,
    High             DECIMAL(19,6)  NOT NULL,
    Low              DECIMAL(19,6)  NOT NULL,
    [Close]          DECIMAL(19,6)  NOT NULL,
    Volume           BIGINT         NOT NULL,
    Vwap      	     DECIMAL(19,6)  NULL,
    Ats 	     BIGINT         NULL
);
GO

