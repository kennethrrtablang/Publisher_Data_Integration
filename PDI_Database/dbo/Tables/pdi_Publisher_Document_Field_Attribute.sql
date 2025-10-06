CREATE TABLE [dbo].[pdi_Publisher_Document_Field_Attribute] (
    [Document_Type_ID]    INT            NOT NULL,
    [Field_Name]          NVARCHAR (50)  NOT NULL,
    [Document_Section]    NVARCHAR (50)  NOT NULL,
    [Document_SubSection] NVARCHAR (50)  NULL,
    [Field_Description]   NVARCHAR (150) NOT NULL,
    [isTextField]         BIT            NOT NULL,
    [isTableField]        BIT            NOT NULL,
    [isChartField]        BIT            NOT NULL,
    [Cycle_Type]          NVARCHAR (50)  NOT NULL,
    [Load_Type]           NVARCHAR (50)  NOT NULL,
    CONSTRAINT [PK_pdi_Publisher_Document_Field_Attribute_1] PRIMARY KEY CLUSTERED ([Field_Name] ASC),
    CONSTRAINT [FK_pdi_Publisher_Document_Field_Attribute_pdi_Data_Load_Type] FOREIGN KEY ([Load_Type]) REFERENCES [dbo].[pdi_Data_Load_Type] ([Load_Type]),
    CONSTRAINT [FK_pdi_Publisher_Document_Field_Attribute_pdi_Document_Type] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_pdi_Publisher_Document_Field_Attribute]
    ON [dbo].[pdi_Publisher_Document_Field_Attribute]([Field_Name] ASC);

