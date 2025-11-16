
-- select * from v_LatestIndicators_Min1

select * from dbo.v_Indicators_Min1_All
where ticker = 'TSLA'
order by tsutc desc


select * from dbo.v_Indicators_Min1_All
where ticker = 'RZLV'
order by tsutc desc




