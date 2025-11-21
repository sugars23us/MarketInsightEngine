-- 2. EQUITY master table
IF OBJECT_ID('dbo.EQUITY','U') IS NOT NULL DROP TABLE dbo.EQUITY;
CREATE TABLE dbo.EQUITY (
    EquityId       INT            IDENTITY(1,1) PRIMARY KEY,
    Ticker         NVARCHAR(16)   NOT NULL UNIQUE,
    Name           NVARCHAR(100)  NULL,
    Exchange       NVARCHAR(20)   NULL,
    Sector         NVARCHAR(100)  NULL,
    Industry       NVARCHAR(100)  NULL,
    MarketCap      DECIMAL(20,2)  NULL,
    FloatShares    BIGINT         NULL,
    AvgVolume3M    BIGINT         NULL,
    InsertedUtc    DATETIME2(0)   NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedUtc     DATETIME2(0)   NULL
);
GO




INSERT INTO dbo.EQUITY (Ticker, Name, Exchange, FloatShares, MarketCap)
VALUES	
('SPY', 'SPDR S&P 500 ETF Trust',	   'NYSE',	  980000000,    543000000000),  -- ~980M float, ~$543B market cap        
('QQQ',  'Invesco QQQ Trust',        'NASDAQ',    615000000,    318000000000),   -- ~615M float
('AAPL', 'Apple Inc.',               'NASDAQ',    15080000000,  4100000000000),  -- 15.08B float
('AMD',  'Advanced Micro Devices',   'NASDAQ',    1610000000,   334000000000),   -- 1.61B float
('MSFT', 'Microsoft Corp.',          'NASDAQ',    7420000000,   3500000000000),  -- 7.42B float
('AMZN', 'Amazon.com Inc.',          'NASDAQ',    10400000000,  2300000000000),  -- ~10.4B float
('QCOM', 'Qualcomm Inc.',            'NASDAQ',    1110000000,   177000000000),
('PYPL', 'PayPal Holdings',          'NASDAQ',    1090000000,   64000000000),
('TSLA', 'Tesla Inc.',               'NASDAQ',    3180000000,   1260000000000),  -- 3.18B float
('PLTR', 'Palantir Technologies',    'NYSE',      2100000000,   35000000000),
('NVDA', 'NVIDIA Corp.',             'NASDAQ',    24600000000,  4500000000000),  -- ~24.6B float
('META', 'Meta Platforms',           'NASDAQ',    2200000000,   1500000000000),
('GOOG', 'Alphabet Inc. Class C',    'NASDAQ',    12300000000,  2500000000000),  -- combined GOOG/GOOGL
('SOFI', 'SoFi Technologies',        'NASDAQ',    960000000,    23000000000),
('MSTR', 'Strategy Inc.',            'NASDAQ',    180000000,    31000000000),    -- MicroStrategy (Bitcoin holder)
('SOUN', 'SoundHound AI',            'NASDAQ',    280000000,    3200000000),
('AVGO', 'Broadcom Inc.',            'NASDAQ',    4650000000,   800000000000),
('IONQ', 'IonQ Inc.',                'NYSE',      210000000,    8800000000);

GO
