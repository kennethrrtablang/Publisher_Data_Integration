CREATE TABLE [dbo].[pdi_File_Receipt_Log] (
    [FIle_ID]                INT              IDENTITY (1, 1) NOT NULL,
    [File_Name]              NVARCHAR (250)   NOT NULL,
    [IsValidFileName]        BIT              NULL,
    [File_Receipt_Timestamp] DATETIME         CONSTRAINT [DF_pdi_File_Receipt_Log_File_Reciept_Timestamp] DEFAULT (getutcdate()) NOT NULL,
    [FileRunID]              UNIQUEIDENTIFIER NULL,
    CONSTRAINT [PK_File_Receipt_Log] PRIMARY KEY CLUSTERED ([FIle_ID] ASC)
);

