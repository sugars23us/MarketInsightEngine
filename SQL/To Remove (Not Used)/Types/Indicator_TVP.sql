USE [MarketInsight]
GO

/****** Object:  UserDefinedTableType [dbo].[IndicatorValueTvp]    Script Date: 18/11/2025 08:30:46 ******/
CREATE TYPE [dbo].[IndicatorValueTvp] AS TABLE(
	[StockId] [int] NOT NULL,
	[TimeframeId] [smallint] NOT NULL,
	[TsUtc] [datetime2](0) NOT NULL,
	[MetricCode] [nvarchar](32) NOT NULL,
	[Period] [smallint] NOT NULL,
	[ParamsJson] [nvarchar](max) NULL,
	[Value] [decimal](19, 8) NOT NULL
)
GO


