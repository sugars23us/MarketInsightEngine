USE [MarketInsight]
GO

/****** Object:  UserDefinedTableType [dbo].[BarTvp]    Script Date: 18/11/2025 08:29:47 ******/
CREATE TYPE [dbo].[BarTvp] AS TABLE(
	[StockId] [int] NOT NULL,
	[TimeframeId] [smallint] NOT NULL,
	[TsUtc] [datetime2](0) NOT NULL,
	[Open] [decimal](19, 6) NOT NULL,
	[High] [decimal](19, 6) NOT NULL,
	[Low] [decimal](19, 6) NOT NULL,
	[Close] [decimal](19, 6) NOT NULL,
	[Volume] [bigint] NOT NULL,
	[Vwap] [decimal](19, 6) NOT NULL,
	[TradeCount] [int] NULL
)
GO


