
-- =============================================
-- Author:      Konkle, Scott
-- Create Date: 9/15/2021
-- Description: Return a pivoted staging for a specific job.
-- =============================================

CREATE PROCEDURE  [dbo].[sp_DataStagingPivot]	
(
	@Job_ID  int
)
AS
BEGIN
	DROP TABLE IF EXISTS #TempDS; 

	CREATE TABLE #TempDS (
		[Job_ID] [int] NOT NULL,
		[Code] [VARCHAR](50) NOT NULL,
		[Item_Name] [VARCHAR](255) NOT NULL,
		[Value] [NVARCHAR](max) NULL,
		PRIMARY KEY CLUSTERED (
			[Job_ID] ASC,
			[Code] ASC,
			[Item_Name] ASC
		) 
	); 

	INSERT INTO #TempDS 
	SELECT * FROM (
		SELECT DISTINCT 
			pDS.Job_ID,
			pDS.Code,
			pDSfc.Item_Name,
			pDSfc.Value 
		FROM pdi_Data_Staging pDS 
		INNER JOIN pdi_Data_Staging pDSfc ON pDSfc.Job_ID = pDS.Job_ID AND pDSfc.Code = pDS.[Value] 
		WHERE pDS.Job_ID = @Job_ID AND pDS.Item_Name = 'FundCode' 
		UNION 
		SELECT 
			Job_ID,
			Code,
			Item_Name,
			Value 
		FROM pdi_Data_Staging 
		WHERE Job_ID = @Job_ID 
		AND CODE NOT IN (
			SELECT DISTINCT [Value] 
			FROM pdi_Data_Staging WHERE Job_ID = @Job_ID AND Item_Name = 'FundCode'
		) AND Sheet_Name NOT IN ('Distributions', 'NAVPU - MER', 'Number of Investments')
	) T;
	
	DECLARE @DynamicColumns AS VARCHAR(max) 
	DECLARE @FinalTableStruct AS NVARCHAR(max) 
	SELECT @DynamicColumns = COALESCE(@DynamicColumns + ', ', '') + QUOTENAME(Item_Name) 
	FROM (
		SELECT DISTINCT Item_Name 
		FROM pdi_Data_Staging 
		WHERE Job_ID = @Job_ID
	) AS FieldList 
	SET @FinalTableStruct = 'SELECT Data_ID, Client_ID, LOB_ID, Data_Type_ID, Document_Type_ID, I.* FROM pdi_Processing_Queue_Log pPQL INNER JOIN (SELECT Job_ID, Code, ' + @DynamicColumns + ' FROM #TempDS DS PIVOT ( max( Value ) for Item_Name in (' + @DynamicColumns + ') ) p ) I ON pPQL.Job_ID = I.Job_ID WHERE pPQL.Job_ID = ' + CONVERT(VARCHAR(15), @Job_ID); 
	
	EXECUTE(@FinalTableStruct); 
	DROP TABLE IF EXISTS #TempDS;
END
