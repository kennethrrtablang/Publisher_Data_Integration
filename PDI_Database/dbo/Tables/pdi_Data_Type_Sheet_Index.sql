CREATE TABLE [dbo].[pdi_Data_Type_Sheet_Index] (
    [Client_ID]                 INT             NOT NULL,
    [Data_Type_ID]              INT             NOT NULL,
    [Document_Type_ID]          INT             NOT NULL,
    [Sheet_Name]                VARCHAR (50)    NOT NULL,
    [Field_Name]                NVARCHAR (4000) NOT NULL,
    [First_Data_Row]            INT             NULL,
    [Has_RowType]               BIT             NULL,
    [Clear_Unused_Fields]       BIT             NULL,
    [Keep_Blank_Columns]        BIT             NULL,
    [Keep_Blank_Rows]           BIT             NULL,
    [Minimum_Output_Columns]    INT             NULL,
    [Disable_Format_Extraction] BIT             NULL,
    CONSTRAINT [PK_pdi_Data_Type_Sheet_Index] PRIMARY KEY CLUSTERED ([Client_ID] ASC, [Data_Type_ID] ASC, [Document_Type_ID] ASC, [Sheet_Name] ASC),
    CONSTRAINT [FK_pdi_Data_Type_Sheet_Index_pdi_Data_Type] FOREIGN KEY ([Data_Type_ID]) REFERENCES [dbo].[pdi_Data_Type] ([Data_Type_ID]),
    CONSTRAINT [FK_pdi_Data_Type_Sheet_Index_pdi_Document_Type] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID]),
    CONSTRAINT [FK_pdi_Data_Type_Sheet_Index_pdi_Publisher_Client] FOREIGN KEY ([Client_ID]) REFERENCES [dbo].[pdi_Publisher_Client] ([Client_ID])
);

