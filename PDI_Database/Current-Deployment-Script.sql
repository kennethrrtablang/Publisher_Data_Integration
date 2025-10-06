/*
Configured to insert foreign keys in order and to not insert existing using SELECT EXCEPT
*/
SET NUMERIC_ROUNDABORT OFF
GO
SET XACT_ABORT, ANSI_PADDING, ANSI_WARNINGS, CONCAT_NULL_YIELDS_NULL, ARITHABORT, QUOTED_IDENTIFIER, ANSI_NULLS ON
GO
/*Pointer used for text / image updates. This might not be needed, but is declared here just in case*/
DECLARE @pv binary(16)
BEGIN TRANSACTION



-- BUG19135
INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA01', 'Short form month 01 for iA BNY', 'Jan.', 'janv.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA02', 'Short form month 02 for iA BNY', 'Feb.', 'févr.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA03', 'Short form month 03 for iA BNY', 'Mar.', 'mars.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA04', 'Short form month 04 for iA BNY', 'Apr.', 'avril.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA05', 'Short form month 05 for iA BNY', 'May.', 'mars.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA06', 'Short form month 06 for iA BNY', 'Jun.', 'juin.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA07', 'Short form month 07 for iA BNY', 'Jul.', 'juil.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA08', 'Short form month 08 for iA BNY', 'Aug.', 'août.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA09', 'Short form month 09 for iA BNY', 'Sep.', 'sept.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA10', 'Short form month 10 for iA BNY', 'Oct.', 'oct.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA11', 'Short form month 11 for iA BNY', 'Nov.', 'nov.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

INSERT INTO pdi_Global_Text_Language ([Scenario] ,[Description] ,[en-CA] ,[fr-CA])
SELECT 'iA12', 'Short form month 12 for iA BNY', 'Dec.', 'déc.'
EXCEPT SELECT [Scenario] ,[Description] ,[en-CA] ,[fr-CA] FROM pdi_Global_Text_Language

COMMIT TRANSACTION
