CREATE TABLE [dbo].[FakeData] (
    [FakeDataID] UNIQUEIDENTIFIER CONSTRAINT [DF_FakeData_FakeDataID] DEFAULT (newid()) NOT NULL,
    [KpiName]    NVARCHAR (50)    NOT NULL,
    [KpiValue]   NVARCHAR (50)    NULL,
    [KpiDate]    DATETIME         NULL,
    [Param1ID]   INT              NULL,
    [Param2ID]   INT              NULL,
    [Param3ID]   INT              NULL,
    [Param4ID]   INT              NULL,
    [Param5ID]   INT              NULL
);

