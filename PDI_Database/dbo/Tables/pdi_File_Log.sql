CREATE TABLE [dbo].[pdi_File_Log] (
    [Data_ID]            INT            IDENTITY (1, 1) NOT NULL,
    [File_ID]            INT            NOT NULL,
    [Data_Custodian]     NVARCHAR (250) NOT NULL,
    [Publisher_Company]  NVARCHAR (250) NOT NULL,
    [Line_of_Business]   NVARCHAR (250) NOT NULL,
    [Data_Type]          NCHAR (10)     NOT NULL,
    [Document_Type]      NCHAR (10)     NOT NULL,
    [File_Creation_Date] NCHAR (10)     NOT NULL,
    [File_Timestamp]     NCHAR (10)     NOT NULL,
    [File_Version]       NCHAR (10)     NOT NULL,
    [IsValidDataFile]    BIT            NULL,
    [Number_of_Records]  INT            NULL,
    [Code]               NVARCHAR (50)  NULL,
    CONSTRAINT [PK_File_Log] PRIMARY KEY CLUSTERED ([Data_ID] ASC),
    CONSTRAINT [FK_File_Log_File_Receipt_Log] FOREIGN KEY ([File_ID]) REFERENCES [dbo].[pdi_File_Receipt_Log] ([FIle_ID])
);

