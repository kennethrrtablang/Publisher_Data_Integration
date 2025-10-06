CREATE TABLE [dbo].[pdi_Transformed_Data] (
    [Job_ID]           INT           NOT NULL,
    [Client_ID]        INT           NOT NULL,
    [Company_ID]       INT           NOT NULL,
    [LOB_ID]           INT           NOT NULL,
    [Document_Type_ID] INT           NOT NULL,
    [Document_Type]    NVARCHAR (50) NOT NULL,
    [Document_Number]  NVARCHAR (50) NOT NULL,
    [Field_Name]       NVARCHAR (50) NOT NULL,
    [Culture_Code]     NVARCHAR (50) NOT NULL,
    [Content]          NVARCHAR (MAX) NOT NULL,
    [isTextField]      BIT           NOT NULL,
    [isTableField]     BIT           NOT NULL,
    [isChartField]     BIT           NOT NULL,
    [Timestamp]        DATETIME      NOT NULL,
    [Feed_Type_Name]   VARCHAR (50)  NULL,
    CONSTRAINT [FK_pdi_Transformed_Data_pdi_Document_Type] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID]),
    CONSTRAINT [FK_pdi_Transformed_Data_pdi_Document_Type1] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID]),
    CONSTRAINT [FK_pdi_Transformed_Data_pdi_Line_of_Business] FOREIGN KEY ([LOB_ID]) REFERENCES [dbo].[pdi_Line_of_Business] ([LOB_ID]),
    CONSTRAINT [FK_pdi_Transformed_Data_pdi_Processing_Queue_Log] FOREIGN KEY ([Job_ID]) REFERENCES [dbo].[pdi_Processing_Queue_Log] ([Job_ID]),
    CONSTRAINT [FK_pdi_Transformed_Data_pdi_Publisher_Client] FOREIGN KEY ([Client_ID]) REFERENCES [dbo].[pdi_Publisher_Client] ([Client_ID]),
    CONSTRAINT [FK_pdi_Transformed_Data_pdi_Publisher_Client1] FOREIGN KEY ([Company_ID]) REFERENCES [dbo].[pdi_Publisher_Client] ([Company_ID])
);

