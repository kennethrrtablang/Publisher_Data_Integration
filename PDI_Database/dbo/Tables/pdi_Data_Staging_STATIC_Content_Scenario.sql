CREATE TABLE [dbo].[pdi_Data_Staging_STATIC_Content_Scenario] (
    [Job_ID]               INT              NOT NULL,
    [STATIC_Sheet_Name]    VARCHAR (100)    NOT NULL,
    [Field_Name]           NVARCHAR (50)    NOT NULL,
    [Scenario]             NVARCHAR (MAX)   NOT NULL,
    [Scenario_Description] NVARCHAR (MAX)   NOT NULL,
    [en-CA]                NVARCHAR (MAX)   NULL,
    [fr-CA]                NVARCHAR (MAX)   NULL,
    [Field_Description]    NVARCHAR (MAX)   NULL,
    [Scenario_ID]          UNIQUEIDENTIFIER CONSTRAINT [DF_pdi_Data_Staging_STATIC_Content_Scenario_Scenario_ID] DEFAULT (newid()) NOT NULL,
    CONSTRAINT [PK_pdi_Data_Staging_STATIC_Content_Scenario] PRIMARY KEY CLUSTERED ([Job_ID] ASC, [Field_Name] ASC, [Scenario_ID] ASC),
    CONSTRAINT [FK_pdi_Data_Staging_STATIC_Content_Scenario_pdi_Processing_Queue_Log] FOREIGN KEY ([Job_ID]) REFERENCES [dbo].[pdi_Processing_Queue_Log] ([Job_ID])
);

