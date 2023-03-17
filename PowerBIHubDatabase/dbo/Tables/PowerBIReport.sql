CREATE TABLE [dbo].[PowerBIReport] (
    [PowerBIReportID] UNIQUEIDENTIFIER CONSTRAINT [DF_PowerBIReport_PowerBIReportID] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [TenantID]        UNIQUEIDENTIFIER NULL,
    [WorkspaceID]     UNIQUEIDENTIFIER NULL,
    [ReportID]        UNIQUEIDENTIFIER CONSTRAINT [DF_PowerBIReport_ReportID] DEFAULT (newid()) NULL,
    [DatasetID]       UNIQUEIDENTIFIER NULL,
    [RLS]             BIT              CONSTRAINT [DF_PowerBIReport_RLS] DEFAULT ((0)) NULL,
    [CreationDate]    DATETIME         NULL,
    [Name]            NVARCHAR (50)    NULL,
    [Author]          NVARCHAR (50)    NULL,
    [Description]     NVARCHAR (100)   NULL,
    [Param1]          NVARCHAR (50)    NULL,
    [Param2]          NVARCHAR (50)    NULL,
    [Param3]          NVARCHAR (50)    NULL,
    [Param4]          NVARCHAR (50)    NULL,
    [Param5]          NVARCHAR (50)    NULL
);



