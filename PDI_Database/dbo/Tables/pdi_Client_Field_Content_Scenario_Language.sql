CREATE TABLE [dbo].[pdi_Client_Field_Content_Scenario_Language] (
    [Document_Type_ID]     INT              NOT NULL,
    [Client_ID]            INT              NOT NULL,
    [LOB_ID]               INT              NOT NULL,
    [Field_Name]           NVARCHAR (50)    NOT NULL,
    [Field_Description]    NVARCHAR (MAX)   NOT NULL,
    [Scenario_ID]          UNIQUEIDENTIFIER CONSTRAINT [DF_pdi_Client_Field_Content_Scenario_Language_Scenario_ID] DEFAULT (newid()) NOT NULL,
    [Scenario]             NVARCHAR (MAX)   NULL,
    [Scenario_Description] NVARCHAR (MAX)   NOT NULL,
    [en-CA]                NVARCHAR (MAX)   NOT NULL,
    [fr-CA]                NVARCHAR (MAX)   NOT NULL,
    [Last_Updated]         DATETIME         NOT NULL,
    CONSTRAINT [PK_pdi_Client_Field_Content_Scenario_Language_1] PRIMARY KEY CLUSTERED ([Document_Type_ID] ASC, [Client_ID] ASC, [LOB_ID] ASC, [Field_Name] ASC, [Scenario_ID] ASC),
    CONSTRAINT [FK_pdi_Client_Field_Content_Scenario_Language_pdi_Document_Type] FOREIGN KEY ([Document_Type_ID]) REFERENCES [dbo].[pdi_Document_Type] ([Document_Type_ID]),
    CONSTRAINT [FK_pdi_Client_Field_Content_Scenario_Language_pdi_Line_of_Business] FOREIGN KEY ([LOB_ID]) REFERENCES [dbo].[pdi_Line_of_Business] ([LOB_ID]),
    CONSTRAINT [FK_pdi_Client_Field_Content_Scenario_Language_pdi_Publisher_Client] FOREIGN KEY ([Client_ID]) REFERENCES [dbo].[pdi_Publisher_Client] ([Client_ID]),
    CONSTRAINT [FK_pdi_Client_Field_Content_Scenario_Language_pdi_Publisher_Document_Field_Attribute] FOREIGN KEY ([Field_Name]) REFERENCES [dbo].[pdi_Publisher_Document_Field_Attribute] ([Field_Name])
);

