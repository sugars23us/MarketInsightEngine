
-- 1. TIMEFRAME lookup
IF OBJECT_ID('dbo.TIMEFRAME', 'U') IS NOT NULL DROP TABLE dbo.TIMEFRAME;
CREATE TABLE dbo.TIMEFRAME (
    TimeframeId   TINYINT       NOT NULL PRIMARY KEY,
    Name          NVARCHAR(20)  NOT NULL UNIQUE,
    Minutes       INT           NOT NULL
);

INSERT INTO dbo.TIMEFRAME (TimeframeId, Name, Minutes) VALUES
(1, '1min',     1),      -- 1 minute
(2, '5min',     5),      -- 5 minutes
(3, '15min',   15),      -- 15 minutes
(4, '1h',      60),      -- 1 hour
(5, '1d',    1440),      -- 1 day
(6, '1w',   10080),      -- 1 week
(7, '1mon', 43200);      -- 1 month 
GO
