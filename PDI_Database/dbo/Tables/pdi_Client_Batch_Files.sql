CREATE TABLE [dbo].[pdi_Client_Batch_Files] (
    [Batch_ID]  INT            NOT NULL,
    [File_Name] NVARCHAR (260) NOT NULL,
    [Extracted] BIT            DEFAULT ((0)) NULL,
    [File_ID]   INT            NULL,
    CONSTRAINT [FK_pdi_Client_Batch_Files_pdi_Client_Batch_Receipt_Log] FOREIGN KEY ([Batch_ID]) REFERENCES [dbo].[pdi_Client_Batch_Receipt_Log] ([Batch_ID]),
    CONSTRAINT [FK_pdi_Client_Batch_Files_pdi_File_Receipt_Log] FOREIGN KEY ([File_ID]) REFERENCES [dbo].[pdi_File_Receipt_Log] ([FIle_ID])
);

