CREATE TABLE [dbo].[AdministratorRole] (
    [AdministratorRoleID] UNIQUEIDENTIFIER CONSTRAINT [DF_AdministratorRole_AdministratorRoleID] DEFAULT (newid()) ROWGUIDCOL NOT NULL,
    [Upn]                 NVARCHAR (50)    NOT NULL
);

