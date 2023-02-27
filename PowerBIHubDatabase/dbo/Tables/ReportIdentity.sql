CREATE TABLE [dbo].[ReportIdentity] (
    [ReportIdentityID] UNIQUEIDENTIFIER CONSTRAINT [DF_ReportIdentity_ReportIdentityID] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [TenantDomain]     NVARCHAR (50)    NULL,
    [Name]             NVARCHAR (50)    NULL,
    [TenantID]         UNIQUEIDENTIFIER NULL,
    [ClientID]         UNIQUEIDENTIFIER NULL,
    [Secret]           NVARCHAR (50)    NULL
);

