CREATE TABLE [dbo].[pdi_Client_Batch_Receipt_Log] (
    [Batch_ID]               INT            IDENTITY (1, 1) NOT NULL,
    [File_Name]              NVARCHAR (260) NOT NULL,
    [File_Receipt_Timestamp] DATETIME       CONSTRAINT [DF_pdi_Client_Batch_Receipt_Log_File_Receipt_Timestamp] DEFAULT (getutcdate()) NOT NULL,
    [Batch_Created_Timestamp] DATETIME NOT NULL,
    CONSTRAINT [PK_pdi_Client_Batch_Receipt_Log] PRIMARY KEY CLUSTERED ([Batch_ID] ASC)
);

