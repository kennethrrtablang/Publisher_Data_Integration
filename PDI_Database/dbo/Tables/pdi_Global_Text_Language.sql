CREATE TABLE [dbo].[pdi_Global_Text_Language] (
    [ID]          INT            IDENTITY (1, 1) NOT NULL,
    [Scenario]    NVARCHAR (MAX) NOT NULL,
    [Description] NVARCHAR (MAX) NULL,
    [en-CA]       NVARCHAR (MAX) NOT NULL,
    [fr-CA]       NVARCHAR (MAX) NOT NULL, 
    CONSTRAINT [PK_pdi_Global_Text_Language] PRIMARY KEY ([ID])
);

