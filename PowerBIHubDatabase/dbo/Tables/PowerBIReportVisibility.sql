CREATE TABLE [dbo].[PowerBIReportVisibility] (
    [PowerBIReportVisibilityID] UNIQUEIDENTIFIER CONSTRAINT [DF_Table_1_AppUserID] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [PowerBIReportID]           UNIQUEIDENTIFIER NOT NULL,
    [ReportIdentityID]          UNIQUEIDENTIFIER NOT NULL,
    [Upn]                       NVARCHAR (50)    NULL
);

