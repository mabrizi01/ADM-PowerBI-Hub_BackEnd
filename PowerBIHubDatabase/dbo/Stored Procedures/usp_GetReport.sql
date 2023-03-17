

-- =============================================
-- Author:      <Author, , Name>
-- Create Date: <Create Date, , >
-- Description: <Description, , >
-- =============================================
CREATE PROCEDURE [dbo].[usp_GetReport]
(
    -- Add the parameters for the stored procedure here
    @powerBIReportID as uniqueidentifier
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON
	
    -- Insert statements for procedure here
    SELECT 
		pr.*, 
		prv.Upn
	from dbo.PowerBIReportVisibility prv
		inner join dbo.PowerBIReport pr
			on prv.[PowerBIReportID] = pr.[PowerBIReportID]
		inner join [dbo].[ReportIdentity] ri
			on prv.[ReportIdentityID] = ri.[ReportIdentityID]
	WHERE 
		pr.PowerBIReportID = @powerBIReportID
		
	order by pr.name
END