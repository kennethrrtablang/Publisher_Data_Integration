CREATE TABLE [dbo].[pdi_Data_Staging_STATIC_Translation_Language] (
    [ID]     INT            IDENTITY (1, 1) NOT NULL,
    [Job_ID] INT            NOT NULL,
    [en-CA]  NVARCHAR (MAX) COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [fr-CA]  NVARCHAR (MAX) NOT NULL,
    CONSTRAINT [PK_pdi_Data_Staging_STATIC_Translation_Language] PRIMARY KEY CLUSTERED ([ID] ASC),
    CONSTRAINT [FK_pdi_Data_Staging_STATIC_Translation_Language_pdi_Processing_Queue_Log] FOREIGN KEY ([Job_ID]) REFERENCES [dbo].[pdi_Processing_Queue_Log] ([Job_ID])
);

