﻿
-- =============================================
-- Author:      <Author, , Name>
-- Create Date: <Create Date, , >
-- Description: <Description, , >
-- =============================================
CREATE PROCEDURE [dbo].[usp_GetReportIdentity]
(
    -- Add the parameters for the stored procedure here
     @upnCaller as nvarchar(50),
	 @upnRequested as nvarchar(50),
	 @powerBIReportID as uniqueidentifier
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

    -- Insert statements for procedure here
	declare @Upn as nvarchar(50)
	declare @IsAdmin as bit

	exec dbo.usp_IsAdmin @upnCaller, @IsAdmin output
	
	if @IsAdmin = 1
		set @Upn = @upnRequested
	else
		set @Upn = @upnCaller

    SELECT 
		pr.*, 
		prv.Upn,
		ri.[ClientID], ri.[Secret]
	from dbo.PowerBIReportVisibility prv
		inner join dbo.PowerBIReport pr
			on prv.[PowerBIReportID] = pr.[PowerBIReportID]
		inner join [dbo].[ReportIdentity] ri
			on prv.[ReportIdentityID] = ri.[ReportIdentityID]
	WHERE prv.Upn = @upn
		and pr.[PowerBIReportID] = @powerBIReportID
		
END