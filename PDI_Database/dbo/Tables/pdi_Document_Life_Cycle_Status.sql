CREATE TABLE [dbo].[pdi_Document_Life_Cycle_Status] (
    [FFDocAgeStatusID] INT           NOT NULL,
    [Status]           NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_pdi_Document_Life_Cycle_Status] PRIMARY KEY CLUSTERED ([FFDocAgeStatusID] ASC),
    CONSTRAINT [IX_pdi_Document_Life_Cycle_Status] UNIQUE NONCLUSTERED ([FFDocAgeStatusID] ASC)
);

