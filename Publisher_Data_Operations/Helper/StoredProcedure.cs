using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Publisher_Data_Operations.Helper
{
    class StoredProcedure
    {
        public static string ImportDataSP()
        {
			return @"
-- ========================================================
-- Author:      Scott Konkle
-- Create Date: 03/01/2022
-- Updated Date: 06/16/2022
-- Description: PDI ETL use only, import using #temptable
-- ========================================================
CREATE PROCEDURE [dbo].[sp_pdi_IMPORT_DATA_temp] 
AS
BEGIN
SET NOCOUNT ON;
BEGIN TRY
	DROP TABLE IF EXISTS #ImportData;
	
	--DECLARE @Job_ID int
	--SET @JOB_ID = 759

	DECLARE @documentSectionID int
	DECLARE @documentTemplateID int
	DECLARE @documentFieldID int
	DECLARE @documentID int
	DECLARE @documentCode varchar(50)
	DECLARE @fieldName varchar(50)
	DECLARE @lineOfBusinessID int
	DECLARE @isTextField bit, @isTableField bit, @isChartField bit

		CREATE TABLE #ImportData
	(
		  [DOCUMENT_NUMBER] NVARCHAR(50) NOT NULL
		, [FIELD_NAME] NVARCHAR(50) NOT NULL
		, [BUSINESS_ID] INT NOT NULL
		, [XMLCONTENT] XML NULL
		, [CONTENT] NVARCHAR(MAX) NULL
		, [DATA_TYPE] VARCHAR(50) NULL
		, [IsTextField] BIT NOT NULL
		, [IsTableField] BIT NOT NULL
		, [IsChartField] BIT NOT NULL
		, [LANGUAGE_ID] INT NOT NULL
		, [DOCUMENT_TYPE_ID] INT NOT NULL
		, [DOCUMENT_TEMPLATE_ID] INT NULL
		, [DOCUMENT_ID] INT NULL
		, [DOCUMENT_SECTION_ID] INT NULL
		, [DOCUMENT_FIELD_ID] INT NULL
		, [DOCUMENT_FILENAME_EN] VARCHAR(500) NULL
		, [DOCUMENT_FILENAME_FR] VARCHAR(500) NULL
		, [SORT_ORDER] INT NULL
	)

	INSERT INTO #ImportData
	SELECT PDI.DOCUMENT_NUMBER, PDI.FIELD_NAME, LOB.BUSINESS_ID,
	CASE WHEN IsTextField = 0 THEN CONVERT(xml, PDI.[Content]) ELSE NULL END AS XMLCONTENT,
	CASE WHEN IsTextField = 1 THEN PDI.[Content] ELSE NULL END AS [CONTENT],  
	CASE 
		WHEN IsTextField = 1 THEN 'TEXT' 
		WHEN IsTableField = 1 THEN 'TABLE'
		WHEN IsChartField = 1 THEN 'CHART'
		ELSE 'UNKNOWN'
	END AS DATA_TYPE, IsTextField, IsTableField, IsChartField,
	L.LANGUAGE_ID, DocT.DOCUMENT_TYPE_ID, 
	ISNULL(FI.DOCUMENT_TEMPLATE_ID, DefaultDT.DOCUMENT_TEMPLATE_ID) AS DOCUMENT_TEMPLATE_ID, D.DOCUMENT_ID, 
	ISNULL(FI.DOCUMENT_SECTION_ID, DefaultDS.DOCUMENT_SECTION_ID) AS DOCUMENT_SECTION_ID, FI.DOCUMENT_FIELD_ID,
	PDI.DOCUMENT_FILENAME_EN, PDI.DOCUMENT_FILENAME_FR, PDI.SORT_ORDER
	FROM #pdi_import_source PDI
	INNER JOIN DOCUMENT_TYPE DocT ON DocT.FEED_TYPE_NAME = PDI.Feed_Type_Name AND DocT.IS_ACTIVE = 1
	INNER JOIN LINE_OF_BUSINESS LOB ON LOB.BUSINESS_NAME = PDI.Business_Name AND LOB.LINE_OF_BUSINESS_CODE = PDI.Line_Of_Business_Code
	INNER JOIN [LANGUAGE] L ON L.CULTURE_CODE = PDI.CULTURE_CODE AND L.IS_ACTIVE = 1
	LEFT OUTER JOIN (
		SELECT Field_Name, MIN(DFA.DOCUMENT_FIELD_ID) AS DOCUMENT_FIELD_ID, DFA.DOCUMENT_SECTION_ID, DT.DOCUMENT_TEMPLATE_ID, DT.BUSINESS_ID, DT.DOCUMENT_TYPE_ID 
		FROM [DOCUMENT_FIELD_ATTRIBUTE] DFA
		INNER JOIN [DOCUMENT_SECTION] DS ON DFA.DOCUMENT_SECTION_ID = DS.DOCUMENT_SECTION_ID AND DS.IS_ACTIVE = 1
		INNER JOIN [DOCUMENT_TEMPLATE] DT ON DS.DOCUMENT_TEMPLATE_ID = DT.DOCUMENT_TEMPLATE_ID AND DT.IS_ACTIVE = 1
		WHERE DFA.IS_ACTIVE = 1 
		GROUP BY Field_Name, DFA.DOCUMENT_SECTION_ID, DT.DOCUMENT_TEMPLATE_ID, DT.BUSINESS_ID, DT.DOCUMENT_TYPE_ID) FI ON FI.BUSINESS_ID = LOB.Business_ID AND DocT.DOCUMENT_TYPE_ID = FI.DOCUMENT_TYPE_ID AND FI.FIELD_NAME = PDI.FIELD_NAME
	LEFT OUTER JOIN (
		SELECT DT.DOCUMENT_TEMPLATE_ID, DT.DOCUMENT_TYPE_ID, DT.BUSINESS_ID 
		FROM [DOCUMENT_TEMPLATE] DT 
		INNER JOIN (SELECT DOCUMENT_TYPE_ID, BUSINESS_ID, MIN(SORT_ORDER) minSortOrder FROM [DOCUMENT_TEMPLATE] WHERE IS_ACTIVE = 1 GROUP BY DOCUMENT_TYPE_ID, BUSINESS_ID) MinDT ON DT.DOCUMENT_TYPE_ID = MinDT.DOCUMENT_TYPE_ID AND DT.BUSINESS_ID = MinDT.BUSINESS_ID AND DT.SORT_ORDER = MinDT.minSortOrder
	) DefaultDT ON DefaultDT.DOCUMENT_TYPE_ID = DocT.DOCUMENT_TYPE_ID AND DefaultDT.BUSINESS_ID = LOB.BUSINESS_ID 
	LEFT OUTER JOIN DOCUMENT D ON D.DOCUMENT_TEMPLATE_ID = ISNULL(FI.DOCUMENT_TEMPLATE_ID, DefaultDT.DOCUMENT_TEMPLATE_ID) AND D.DOCUMENT_NUMBER = PDI.DOCUMENT_NUMBER AND D.Business_ID = LOB.Business_ID
	LEFT OUTER JOIN (
		SELECT DS.DOCUMENT_SECTION_ID, DS.DOCUMENT_TEMPLATE_ID 
		FROM [DOCUMENT_SECTION] DS 
		INNER JOIN (SELECT DOCUMENT_TEMPLATE_ID, MIN(SORT_ORDER) minSortOrder FROM [DOCUMENT_SECTION] WHERE IS_ACTIVE = 1 GROUP BY DOCUMENT_TEMPLATE_ID) MinDS ON DS.DOCUMENT_TEMPLATE_ID = MinDS.DOCUMENT_TEMPLATE_ID AND DS.SORT_ORDER = MinDS.minSortOrder
	) DefaultDS ON DefaultDS.DOCUMENT_TEMPLATE_ID = DefaultDT.DOCUMENT_TEMPLATE_ID
--WHERE PDI.Job_ID = @Job_ID -- No longer needed since the temp table only contains the current job

-- it's not possible for LOB to be missing from ETL Imports as the LOB needs to be created first so there is not point in creating it if it's missing <- we can't get here from ETL

	SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; -- when creating new documents don't let any other parallel processes make changes
	BEGIN TRANSACTION;

-- Handle any new documents --
	DECLARE documentDataCursor CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT document_number, BUSINESS_ID, DOCUMENT_TEMPLATE_ID FROM #ImportData WHERE DOCUMENT_ID IS NULL
	OPEN documentDataCursor
	FETCH NEXT FROM documentDataCursor INTO @documentCode, @lineOfBusinessID, @documentTemplateID

	WHILE @@FETCH_STATUS = 0
	BEGIN
		-- create the document, if it doesn't already exist
		EXEC sp_SAVE_DOCUMENT @businessID=@lineOfBusinessID, @documentTemplateID=@documentTemplateID, @documentNameEN=null, @documentNumberEN=@documentCode, @documentNameFR=null, @documentNumberFR=null, @documentFilenameEN=null, @documentFilenameFR=null, @newDocumentID=@documentID out

		UPDATE #ImportData SET DOCUMENT_ID = @documentID WHERE DOCUMENT_NUMBER = @documentCode AND BUSINESS_ID = @lineOfBusinessID AND DOCUMENT_TEMPLATE_ID = @documentTemplateID

		-- get the template ID used by this document (may not be the same as the default template set earlier)	
		-- SK since we are loading both the matched document template and the default template at the start this shouldn't be necessary
		--SELECT @documentTemplateID = document_template_id FROM DOCUMENT WHERE DOCUMENT_ID = @documentID

		FETCH NEXT FROM documentDataCursor INTO @documentCode, @lineOfBusinessID, @documentTemplateID

	END
	CLOSE documentDataCursor
	DEALLOCATE documentDataCursor

	COMMIT TRANSACTION; -- since we set Serializable on the transaction close it as quickly as possible

	-- UPDATE DOCUMENT TABLE (SORT_ORDER, DOCUMENT_FILENAME, DOCUMENT_FILENAME_FR) -- 3 fields are done seperately to make it easier to filter for nulls
	UPDATE D SET D.DOCUMENT_FILENAME = ID.DOCUMENT_FILENAME_EN
	FROM  DOCUMENT D
	INNER JOIN (SELECT DISTINCT DOCUMENT_ID, DOCUMENT_FILENAME_EN FROM #ImportData WHERE DOCUMENT_FILENAME_EN IS NOT NULL) ID ON ID.[DOCUMENT_ID] = D.DOCUMENT_ID
	WHERE ID.DOCUMENT_FILENAME_EN <> D.DOCUMENT_FILENAME OR D.DOCUMENT_FILENAME IS NULL

	UPDATE D SET D.DOCUMENT_FILENAME_FR = ID.DOCUMENT_FILENAME_FR
	FROM  DOCUMENT D
	INNER JOIN (SELECT DISTINCT DOCUMENT_ID, DOCUMENT_FILENAME_FR FROM #ImportData WHERE DOCUMENT_FILENAME_FR IS NOT NULL) ID ON ID.[DOCUMENT_ID] = D.DOCUMENT_ID
	WHERE ID.DOCUMENT_FILENAME_FR <> D.DOCUMENT_FILENAME_FR OR D.DOCUMENT_FILENAME_FR IS NULL

	UPDATE D SET D.SORT_ORDER = ID.SORT_ORDER
	FROM  DOCUMENT D
	INNER JOIN (SELECT DISTINCT DOCUMENT_ID, SORT_ORDER FROM #ImportData WHERE SORT_ORDER IS NOT NULL) ID ON ID.[DOCUMENT_ID] = D.DOCUMENT_ID
	WHERE ID.SORT_ORDER <> D.SORT_ORDER OR D.SORT_ORDER IS NULL

	SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; -- when creating new sections don't let any other parallel processes make changes
	BEGIN TRANSACTION;

	-- Handle any new section --
	DECLARE sectionDataCursor CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT DOCUMENT_TEMPLATE_ID FROM #ImportData WHERE DOCUMENT_SECTION_ID IS NULL
	OPEN sectionDataCursor
	FETCH NEXT FROM sectionDataCursor INTO @documentTemplateID

	WHILE @@FETCH_STATUS = 0
	BEGIN
		-- get the document section ID, create one if it hasn't already
		SELECT TOP 1 @documentSectionID = DOCUMENT_SECTION_ID from DOCUMENT_SECTION WHERE IS_ACTIVE = 1 AND DOCUMENT_TEMPLATE_ID = @documentTemplateID ORDER BY SORT_ORDER
		IF @@ROWCOUNT = 0
		BEGIN
			EXEC sp_SAVE_DOCUMENT_SECTION null,	@documentTemplateID, 'New Section', null, null, @documentSectionID out
			--PRINT 'save document section @documentTemplateID - ' + str(@documentTemplateID)
		END
		UPDATE #ImportData SET DOCUMENT_SECTION_ID = @documentSectionID WHERE DOCUMENT_TEMPLATE_ID = @documentTemplateID AND DOCUMENT_SECTION_ID IS NULL
		--print  'docSectionID - ' + str(@documentSectionID)

		FETCH NEXT FROM sectionDataCursor INTO @documentTemplateID
	END
	CLOSE sectionDataCursor
	DEALLOCATE sectionDataCursor

	COMMIT TRANSACTION;

	SET TRANSACTION ISOLATION LEVEL SERIALIZABLE; -- when creating new fields don't let any other parallel processes make changes - this is actually the most troublesome one
	BEGIN TRANSACTION;

	DECLARE fieldDataCursor CURSOR LOCAL FAST_FORWARD FOR SELECT DISTINCT DOCUMENT_SECTION_ID, DOCUMENT_TEMPLATE_ID, FIELD_NAME, IsTextField, IsTableField, IsChartField FROM #ImportData WHERE DOCUMENT_FIELD_ID IS NULL
	OPEN fieldDataCursor
	FETCH NEXT FROM fieldDataCursor INTO @documentSectionID, @documentTemplateID, @fieldName, @isTextField, @isTableField, @isChartField

	WHILE @@FETCH_STATUS = 0
	BEGIN

	-- save the document field
		exec sp_SAVE_DOCUMENT_FIELD @documentSectionID, @documentTemplateID, @fieldName, null, null, @isTextField, @isTableField, @isChartField, @documentFieldID out
		UPDATE #ImportData SET DOCUMENT_FIELD_ID = @documentFieldID WHERE FIELD_NAME = @fieldName AND Document_Section_ID = @documentSectionID AND DOCUMENT_TEMPLATE_ID = @documentTemplateID
	
		FETCH NEXT FROM fieldDataCursor INTO @documentSectionID, @documentTemplateID, @fieldName, @isTextField, @isTableField, @isChartField
	END
	CLOSE fieldDataCursor
	DEALLOCATE fieldDataCursor

	COMMIT TRANSACTION;

	BEGIN TRANSACTION;

	-- UPDATE DOCUMENT_FIELD_VALUE --
	UPDATE DFV 
	SET CONTENT = ID.[Content],
	XMLCONTENT = ID.XMLCONTENT,
	IS_ACTIVE = 1,
	DATA_TYPE = ID.DATA_TYPE
	FROM #ImportData ID
	INNER JOIN DOCUMENT_FIELD_VALUE DFV 
	ON ID.DOCUMENT_ID=DFV.DOCUMENT_ID
	AND ID.DOCUMENT_FIELD_ID=DFV.DOCUMENT_FIELD_ID
	AND ID.LANGUAGE_ID=DFV.LANGUAGE_ID

	-- INSERT NEW DOCUMENT_FIELD_VALUE 
	INSERT INTO DOCUMENT_FIELD_VALUE (DOCUMENT_ID, DOCUMENT_FIELD_ID, LANGUAGE_ID, IS_ACTIVE, CONTENT, XMLCONTENT, DATA_TYPE) 
	SELECT ID.DOCUMENT_ID, ID.DOCUMENT_FIELD_ID, ID.LANGUAGE_ID, 1, ID.[Content], ID.XMLCONTENT, ID.DATA_TYPE
	FROM #ImportData ID
	LEFT OUTER JOIN DOCUMENT_FIELD_VALUE DFV ON DFV.DOCUMENT_ID = ID.DOCUMENT_ID AND DFV.DOCUMENT_FIELD_ID = ID.DOCUMENT_FIELD_ID AND DFV.LANGUAGE_ID = ID.LANGUAGE_ID
	WHERE DFV.DOCUMENT_ID IS NULL

	-- UPDATE TIME INFORMATION --
	UPDATE DOCUMENT SET LAST_IMPORT_DATE = GETUTCDATE() WHERE DOCUMENT_ID in (SELECT DISTINCT DOCUMENT_ID from #ImportData)
	
	COMMIT TRANSACTION;

	BEGIN TRANSACTION;


	DECLARE @RetryNo Int = 1 , @RetryMaxNo Int = 5;
	WHILE @RetryNo < @RetryMaxNo
	BEGIN
		BEGIN TRY 

        --ALTER TABLE DOCUMENT_FIELD_VALUE_HISTORY NOCHECK CONSTRAINT ALL
-- INSERTING HISTORY TABLE -- This is the most expensive part of the query at 75% of a 1000 record insert
			INSERT INTO DOCUMENT_FIELD_VALUE_HISTORY --WITH (UPDLOCK)
			( DOCUMENT_ID, DOCUMENT_FIELD_ID, IS_TEXT_FIELD, IS_TABLE_FIELD, IS_CHART_FIELD, LANGUAGE_ID, CONTENT, XMLCONTENT) 
			SELECT ID.DOCUMENT_ID, ID.DOCUMENT_FIELD_ID, IsTextField, IsTableField, IsChartField, ID.LANGUAGE_ID, ID.CONTENT, ID.XMLCONTENT
			FROM #ImportData ID 

			SELECT   @RetryNo = @RetryMaxNo;
		END TRY
		BEGIN CATCH
			IF ERROR_NUMBER() IN (-1, -2, 701, 1204, 1205, 1222, 8645, 8651, 30053)
				BEGIN
					SET @RetryNo += 1;
					-- it will wait for 10 seconds to do another attempt
					WAITFOR DELAY '00:00:10';
				END 
			ELSE
				THROW;
		END CATCH
      END 
	--ALTER TABLE DOCUMENT_FIELD_VALUE_HISTORY CHECK CONSTRAINT ALL
	COMMIT TRANSACTION;	


	DROP TABLE IF EXISTS #ImportData

	SELECT 'Complete';

END TRY
BEGIN CATCH
	SELECT ERROR_MESSAGE() AS ErrorMessage;
END CATCH

END
";

		}

		public static string ClearSPByDate()
        {
			return @"
DECLARE @create DateTime
SELECT @create = create_date FROM sys.procedures WHERE [name] = 'sp_pdi_IMPORT_DATA_temp'

IF datediff(day, @create, getdate()) > 0
BEGIN 
	DROP PROCEDURE IF EXISTS [dbo].[sp_pdi_IMPORT_DATA_temp]
	PRINT 'sp_pdi_IMPORT_DATA_temp different create and current dates - deleted'
END";

		}
    }
}
