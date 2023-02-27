-- =============================================
-- Author:      <Author, , Name>
-- Create Date: <Create Date, , >
-- Description: <Description, , >
-- =============================================
CREATE PROCEDURE usp_IsAdmin
(
    -- Add the parameters for the stored procedure here
    @Upn as nvarchar(50),
	@Bit BIT OUTPUT
)
AS
BEGIN
    -- SET NOCOUNT ON added to prevent extra result sets from
    -- interfering with SELECT statements.
    SET NOCOUNT ON

    -- Insert statements for procedure here
    declare @Rows as int
	
	SELECT @Rows = count(*) 
	from [dbo].[AdministratorRole] ar
	where ar.Upn = @Upn

	if (@Rows > 0)
		set @Bit = 1
	else
		set @Bit = 0

END