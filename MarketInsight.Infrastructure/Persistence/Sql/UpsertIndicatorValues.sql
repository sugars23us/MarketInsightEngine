-- Idempotent create, then replace body (works on SQL Server)
IF OBJECT_ID(N'dbo.UpsertIndicatorValues', N'P') IS NULL
BEGIN
    EXEC('CREATE PROCEDURE dbo.UpsertIndicatorValues AS SET NOCOUNT ON;');
END;
GO

CREATE OR ALTER PROCEDURE dbo.UpsertIndicatorValues
    @Rows dbo.IndicatorValueTvp READONLY
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH src AS (
        SELECT *
        FROM @Rows
        WHERE MetricCode IS NOT NULL
          AND Period IS NOT NULL
          AND TimeframeId IS NOT NULL
    )
    MERGE dbo.INDICATOR_VALUE WITH (HOLDLOCK) AS t
    USING src AS s
       ON t.StockId     = s.StockId
      AND t.TimeframeId = s.TimeframeId
      AND t.TsUtc       = s.TsUtc
      AND t.MetricCode  = s.MetricCode
      AND t.Period      = s.Period
    WHEN MATCHED THEN
      UPDATE SET
         t.Value      = s.Value,
         t.ParamsJson = s.ParamsJson,
         t.UpdatedUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED THEN
      INSERT (StockId,TimeframeId,TsUtc,MetricCode,Period,ParamsJson,Value,UpdatedUtc)
      VALUES (s.StockId,s.TimeframeId,s.TsUtc,s.MetricCode,s.Period,s.ParamsJson,s.Value,SYSUTCDATETIME());
END
GO
