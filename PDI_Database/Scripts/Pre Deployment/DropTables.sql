



/****** Object:  Table [dbo].[pdi_IMPORT_tblTableData]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_IMPORT_tblTableData]
GO
/****** Object:  Table [dbo].[pdi_IMPORT_tblParagraphText]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_IMPORT_tblParagraphText]
GO
/****** Object:  Table [dbo].[pdi_IMPORT_tblGraphData]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_IMPORT_tblGraphData]
GO
/****** Object:  Table [dbo].[pdi_IMPORT_tblFundCoGroupFunds]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_IMPORT_tblFundCoGroupFunds]
GO
/****** Object:  Table [dbo].[pdi_Data_Staging_STATIC_Translation_Language]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Data_Staging_STATIC_Translation_Language]
GO
/****** Object:  Table [dbo].[pdi_Data_Staging_STATIC_Field_Update]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Data_Staging_STATIC_Field_Update]
GO
/****** Object:  Table [dbo].[pdi_Data_Staging_STATIC_Content_Scenario]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Data_Staging_STATIC_Content_Scenario]
GO
/****** Object:  Table [dbo].[pdi_Data_Staging]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Data_Staging]
GO
/****** Object:  Table [dbo].[pdi_Transformed_Data]    Script Date: 2/28/2022 2:53:27 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Transformed_Data]
GO

/****** Object:  Table [dbo].[pdi_File_Validation_Log]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_File_Validation_Log]
GO
/****** Object:  Table [dbo].[pdi_Client_Billable_Activity]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Client_Billable_Activity]
GO
/****** Object:  Table [dbo].[pdi_Client_Batch_Files]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Client_Batch_Files]
GO
/****** Object:  Table [dbo].[pdi_Client_Batch_Receipt_Log]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Client_Batch_Receipt_Log]
GO


/****** Object:  Table [dbo].[pdi_Publisher_Documents]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Publisher_Documents]
GO
/****** Object:  Table [dbo].[pdi_Publisher_Document_Templates]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Publisher_Document_Templates]
GO

/****** Object:  Table [dbo].[pdi_Client_Translation_Language_Missing_Log_Details]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Client_Translation_Language_Missing_Log_Details]
GO
/****** Object:  Table [dbo].[pdi_Client_Translation_Language_Missing_Log]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Client_Translation_Language_Missing_Log]
GO
/****** Object:  Table [dbo].[pdi_Client_Translation_Language]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Client_Translation_Language]
GO
/****** Object:  Table [dbo].[pdi_Client_Field_Content_Scenario_Language]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Client_Field_Content_Scenario_Language]
GO

/****** Object:  Table [dbo].[pdi_Processing_Queue_Log]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Processing_Queue_Log]
GO
/****** Object:  Table [dbo].[pdi_File_Log]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_File_Log]
GO
/****** Object:  Table [dbo].[pdi_File_Receipt_Log]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_File_Receipt_Log]
GO

/****** Object:  Table [dbo].[pdi_Data_Type_Sheet_Index]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Data_Type_Sheet_Index]
GO
/****** Object:  Table [dbo].[pdi_Global_Text_Language]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Global_Text_Language]
GO
/****** Object:  Table [dbo].[pdi_Publisher_Document_Field_Attribute]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Publisher_Document_Field_Attribute]
GO

/****** Object:  Table [dbo].[pdi_Line_of_Business]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Line_of_Business]
GO

/****** Object:  Table [dbo].[pdi_Document_Life_Cycle_Status]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Document_Life_Cycle_Status]
GO
/****** Object:  Table [dbo].[pdi_Document_Type]    Script Date: 2/28/2022 2:53:28 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Document_Type]
GO

/****** Object:  Table [dbo].[pdi_Content_Scenario_Parameters]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Content_Scenario_Parameters]
GO

/****** Object:  Table [dbo].[pdi_Data_Type]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Data_Type]
GO
/****** Object:  Table [dbo].[pdi_Data_Load_Type]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Data_Load_Type]
GO
/****** Object:  Table [dbo].[pdi_Publisher_Client]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Publisher_Client]
GO
/****** Object:  Table [dbo].[pdi_Data_Custodian]    Script Date: 2/28/2022 2:53:29 PM ******/
DROP TABLE IF EXISTS [dbo].[pdi_Data_Custodian]
GO

