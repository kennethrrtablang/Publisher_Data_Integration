CREATE TABLE [dbo].[pdi_Client_Translation_Language] (
    [ID]               INT            IDENTITY (1, 1) NOT NULL,
    [Client_ID]        INT            NOT NULL,
    [LOB_ID]           INT            NOT NULL,
    [Document_Type_ID] INT            NOT NULL,
    [en-CA]            NVARCHAR (MAX) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [fr-CA]            NVARCHAR (MAX) NOT NULL,
    [Last_Updated]     DATETIME       NOT NULL,
    CONSTRAINT [PK_pdi_Client_Translation_Language] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_pdi_Client_Translation_language_pdi_Document_Type] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID]),
    CONSTRAINT [FK_pdi_Client_Translation_language_pdi_Line_of_Business] FOREIGN KEY ([LOB_ID]) REFERENCES [dbo].[pdi_Line_of_Business] ([LOB_ID]),
    CONSTRAINT [FK_pdi_Client_Translation_language_pdi_Publisher_Client] FOREIGN KEY ([Client_ID]) REFERENCES [dbo].[pdi_Publisher_Client] ([Client_ID])
);


GO
CREATE NONCLUSTERED INDEX [IX_pdi_Client_Translation_language]
    ON [dbo].[pdi_Client_Translation_Language]([Client_ID] ASC);

