USE [MarketInsight]
GO

/****** Object:  UserDefinedTableType [dbo].[IndicatorUpsertTvp]    Script Date: 18/11/2025 08:15:35 ******/
CREATE TYPE [dbo].[IndicatorUpsertTvp] AS TABLE(
	[StockId] [int] NOT NULL,
	[TimeframeId] [tinyint] NOT NULL,
	[TsUtc] [datetime2](0) NOT NULL,
	[MetricCode] [nvarchar](24) NOT NULL,
	[Period] [smallint] NOT NULL,
	[ParamsJson] [nvarchar](256) NULL,
	[Value] [decimal](19, 8) NOT NULL,
	PRIMARY KEY CLUSTERED 
(
	[StockId] ASC,
	[TimeframeId] ASC,
	[TsUtc] ASC,
	[MetricCode] ASC,
	[Period] ASC
)WITH (IGNORE_DUP_KEY = OFF)
)
GO


