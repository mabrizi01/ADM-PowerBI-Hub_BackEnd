CREATE TABLE [dbo].[FakeDataToRole] (
    [FakeDataToRoleID] UNIQUEIDENTIFIER CONSTRAINT [DF_FakeDataToRole_FakeDataToRoleID] DEFAULT (newid()) NOT NULL,
    [FakeDataID]       UNIQUEIDENTIFIER NOT NULL,
    [RoleID]           INT              NOT NULL
);

