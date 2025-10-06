/****** Object:  Table [dbo].[view_pdi_Fund_Profile_Data]    Script Date: 2/28/2022 2:53:27 PM ******/
IF (SELECT object_id FROM sys.external_tables where name = 'view_pdi_Fund_Profile_Data') IS NOT NULL
BEGIN
    DROP EXTERNAL TABLE [view_pdi_Fund_Profile_Data];
END
GO

IF (SELECT data_source_id FROM sys.external_data_sources where name = 'PUBLISHER_PROD') IS NOT NULL
BEGIN
    DROP EXTERNAL DATA SOURCE [PUBLISHER_PROD];
END
GO