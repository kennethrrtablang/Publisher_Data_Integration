CREATE TABLE [dbo].[pdi_Publisher_Document_Templates] (
    [ID]                   INT           IDENTITY (1, 1) NOT NULL,
    [Client_ID]            INT           NOT NULL,
    [Document_Type_ID]     INT           NOT NULL,
    [Document_Template_ID] INT           NOT NULL,
    [Document_Temp_Name]   VARCHAR (500) NOT NULL,
    [Template_layout_Name] VARCHAR (20)  NOT NULL,
    [Template_Code]        VARCHAR (50)  NOT NULL,
    [Document_Name_Field]  NVARCHAR (50) NULL,
    [IS_Active]            BIT           NOT NULL,
    CONSTRAINT [PK_Publisher_Document_Templates] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_pdi_Publisher_Document_Templates_pdi_Document_Type] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID]),
    CONSTRAINT [FK_pdi_Publisher_Document_Templates_pdi_Publisher_Document_Field_Attribute] FOREIGN KEY ([Document_Name_Field]) REFERENCES [dbo].[pdi_Publisher_Document_Field_Attribute] ([Field_Name]),
    CONSTRAINT [FK_Publisher_Document_Templates_Publisher_Client] FOREIGN KEY ([Client_ID]) REFERENCES [dbo].[pdi_Publisher_Client] ([Client_ID]),
    CONSTRAINT [FK_Publisher_Document_Templates_Publisher_Document_Templates] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID])
);

