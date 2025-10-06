CREATE TABLE [dbo].[pdi_Content_Scenario_Parameters] (
    [Document_Type_ID] INT            NOT NULL,
    [Field_Name]       NVARCHAR (50)  NOT NULL,
    [StaticToken]      NVARCHAR (150) NOT NULL,
    [Description]      NVARCHAR (500) NULL,
    CONSTRAINT [PK_pdi_Content_Scenario_Parameters] PRIMARY KEY CLUSTERED ([Document_Type_ID] ASC, [Field_Name] ASC, [StaticToken] ASC)
);

