/*
Post-Deployment Script Template							
--------------------------------------------------------------------------------------
 This file contains SQL statements that will be appended to the build script.		
 Use SQLCMD syntax to include a file in the post-deployment script.			
 Example:      :r .\myfile.sql								
 Use SQLCMD syntax to reference a variable in the post-deployment script.		
 Example:      :setvar TableName MyTable							
               SELECT * FROM [$(TableName)]					
--------------------------------------------------------------------------------------
*/
/* Load data in order */
print 'Loading Data Start...';
print 'Loading pdi_Data_Custodian';
:r .\pdi_Data_Custodian.Table.sql
Go
print 'Loading pdi_Publisher_Client';
:r .\pdi_Publisher_Client.Table.sql
Go
print 'Loading pdi_Line_of_Business';
:r .\pdi_Line_of_Business.Table.sql
Go
print 'Loading pdi_Data_Load_Type';
:r .\pdi_Data_Load_Type.Table.sql
Go
print 'Loading pdi_Data_Type';
:r .\pdi_Data_Type.Table.sql
Go
print 'Loading pdi_Document_Type';
:r .\pdi_Document_Type.Table.sql
Go
print 'Loading pdi_Document_Life_Cycle_Status';
:r .\pdi_Document_Life_Cycle_Status.Table.sql
Go
print 'Loading pdi_Global_Text_Language';
:r .\pdi_Global_Text_Language.Table.sql
Go
print 'Loading pdi_Publisher_Document_Field_Attribute';
:r .\pdi_Publisher_Document_Field_Attribute.Table.sql
Go
print 'Loading pdi_Publisher_Document_Templates';
:r .\pdi_Publisher_Document_Templates.Table.sql
Go
print 'Loading pdi_Content_Scenario_Parameters';
:r .\pdi_Content_Scenario_Parameters.Table.sql
Go
print 'Loading pdi_Client_Translation_Language';
:r .\pdi_Client_Translation_Language.Table.sql
Go
print 'Loading pdi_Client_Field_Content_Scenario_Language';
:r .\pdi_Client_Field_Content_Scenario_Language.Table.sql
Go
print 'Loading pdi_Data_Type_Sheet_Index';
:r .\pdi_Data_Type_Sheet_Index.Table.sql
Go
print 'Completed Deploying Default Data';

