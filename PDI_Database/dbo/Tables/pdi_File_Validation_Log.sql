CREATE TABLE [dbo].[pdi_File_Validation_Log] (
    [Message_ID]         INT              IDENTITY (-2147483648, 1) NOT NULL,
    [File_ID]            INT              NULL,
    [Batch_ID]           INT              NULL,
    [Run_ID]             UNIQUEIDENTIFIER NULL,
    [Validation_Message] NVARCHAR (MAX)   NOT NULL,
    [Timestamp]          DATETIME         DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [PK_pdi_Log] PRIMARY KEY CLUSTERED ([Message_ID] ASC),
    CONSTRAINT [pdi_Log_At_Least_One_Not_Null] CHECK (((case when [File_ID] IS NULL then (0) else (1) end+case when [Batch_ID] IS NULL then (0) else (1) end)+case when [Run_ID] IS NULL then (0) else (1) end)>(0)),
    CONSTRAINT [FK_pdi_Log_pdi_Client_Batch_Receipt_Log] FOREIGN KEY ([Batch_ID]) REFERENCES [dbo].[pdi_Client_Batch_Receipt_Log] ([Batch_ID]),
    CONSTRAINT [FK_pdi_Log_pdi_File_Receipt_Log] FOREIGN KEY ([File_ID]) REFERENCES [dbo].[pdi_File_Receipt_Log] ([FIle_ID])
);

