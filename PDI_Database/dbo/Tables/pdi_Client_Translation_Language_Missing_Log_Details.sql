CREATE TABLE [dbo].[pdi_Client_Translation_Language_Missing_Log_Details] (
    [Missing_ID]      UNIQUEIDENTIFIER NOT NULL,
    [Job_ID]          INT              NOT NULL,
    [Document_Number] NVARCHAR (50)    NOT NULL,
    [Field_Name]      NVARCHAR (50)    NOT NULL,
    [Last_Updated]    DATETIME         DEFAULT (getutcdate()) NOT NULL,
    CONSTRAINT [FK_pdi_Client_Translation_Language_Missing_Log_Details_pdi_Client_Translation_Language_Missing_Log] FOREIGN KEY ([Missing_ID]) REFERENCES [dbo].[pdi_Client_Translation_Language_Missing_Log] ([Missing_ID]),
    CONSTRAINT [FK_pdi_Client_Translation_Language_Missing_Log_Details_pdi_Processing_Queue_Log] FOREIGN KEY ([Job_ID]) REFERENCES [dbo].[pdi_Processing_Queue_Log] ([Job_ID]),
    CONSTRAINT [FK_pdi_Client_Translation_Language_Missing_Log_Details_pdi_Publisher_Document_Field_Attribute] FOREIGN KEY ([Field_Name]) REFERENCES [dbo].[pdi_Publisher_Document_Field_Attribute] ([Field_Name])
);

