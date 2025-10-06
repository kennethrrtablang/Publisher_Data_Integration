CREATE TABLE [dbo].[pdi_Document_Type] (
    [Document_Type_ID]   INT          NOT NULL,
    [Document_Type]      VARCHAR (50) NOT NULL,
    [Document_Type_Name] VARCHAR (50) NOT NULL,
    [Feed_Type_Name]     VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Document_Type] PRIMARY KEY CLUSTERED ([Document_Type_ID] ASC)
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_pdi_Document_Type]
    ON [dbo].[pdi_Document_Type]([Document_Type] ASC);

