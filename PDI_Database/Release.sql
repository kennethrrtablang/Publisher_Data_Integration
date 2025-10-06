
 --DECLARE @TransactionName VARCHAR(20);


 ---- use this template for sql queries -----------

/*

-- added for story: <storyNumber>
-- added by : <name>

-- comment if any

BEGIN TRY  
SET @TransactionName ='19170';
BEGIN TRAN @TransactionName

--..............   your script here .....................
    Insert into [dbo].[pdi_Publisher_Document_Field_Attribute] values
(2, 'F54c', 'New Section', '', 'Structured entities text 2', 1, 0, 0, 'All', 'STATIC2');
  Insert into [dbo].[pdi_Publisher_Document_Field_Attribute] values
(2, 'F18ba', 'New Section', '', 'Interest rate risk summary text', 1, 0, 0, 'All', 'STATIC2');


COMMIT TRAN @TransactionName
PRINT('completed  - '+@TransactionName)
END TRY  
BEGIN CATCH  
PRINT('error in  - '+@TransactionName)
ROLLBACK TRAN @TransactionName
 
END CATCH; 

*/
--  add an empty line of dashes like this after every story changes ---------------------------------------------------------------
