USE [MarketInsight]
GO

/****** Object:  UserDefinedTableType [dbo].[BarUpsertTvp]    Script Date: 18/11/2025 08:14:49 ******/
CREATE TYPE [dbo].[BarUpsertTvp] AS TABLE(
	[StockId] [int] NOT NULL,
	[TimeframeId] [tinyint] NOT NULL,
	[TsUtc] [datetime2](0) NOT NULL,
	[Open] [decimal](19, 6) NOT NULL,
	[High] [decimal](19, 6) NOT NULL,
	[Low] [decimal](19, 6) NOT NULL,
	[Close] [decimal](19, 6) NOT NULL,
	[Volume] [bigint] NOT NULL,
	[Vwap] [decimal](19, 6) NULL,
	[TradeCount] [int] NULL,
	PRIMARY KEY CLUSTERED 
(
	[StockId] ASC,
	[TimeframeId] ASC,
	[TsUtc] ASC
)WITH (IGNORE_DUP_KEY = OFF)
)
GO


