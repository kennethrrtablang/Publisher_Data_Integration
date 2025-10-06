CREATE TABLE [dbo].[pdi_Processing_Queue_Log] (
    [Job_ID]            INT            IDENTITY (1, 1) NOT NULL,
    [Data_ID]           INT            NOT NULL,
    [Client_ID]         INT            NOT NULL,
    [LOB_ID]            INT            NOT NULL,
    [Data_Type_ID]      INT            NOT NULL,
    [Document_Type_ID]  INT            NOT NULL,
    [Job_Status]        VARCHAR (50)   NULL,
    [Job_Start]         DATETIME       NULL,
    [Extract_End]       DATETIME       NULL,
    [Transform_End]     DATETIME       NULL,
    [Load_End]          DATETIME       NULL,
    [Import_End]        DATETIME       NULL,
    [Validation_End]    DATETIME       NULL,
    [Process_Source]    NVARCHAR (255) NULL,
    [FilingReferenceID] NVARCHAR (50)  NULL,
    CONSTRAINT [PK_Processing_Queue_Log] PRIMARY KEY CLUSTERED ([Job_ID] ASC),
    CONSTRAINT [FK_Processing_Queue_Log_Data_Type] FOREIGN KEY ([Data_Type_ID]) REFERENCES [dbo].[pdi_Data_Type] ([Data_Type_ID]),
    CONSTRAINT [FK_Processing_Queue_Log_Document_Type] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID]),
    CONSTRAINT [FK_Processing_Queue_Log_File_Log] FOREIGN KEY ([Data_ID]) REFERENCES [dbo].[pdi_File_Log] ([Data_ID]),
    CONSTRAINT [FK_Processing_Queue_Log_Line_of_Business] FOREIGN KEY ([LOB_ID]) REFERENCES [dbo].[pdi_Line_of_Business] ([LOB_ID]),
    CONSTRAINT [FK_Processing_Queue_Log_Publisher_Client] FOREIGN KEY ([Client_ID]) REFERENCES [dbo].[pdi_Publisher_Client] ([Client_ID])
);

