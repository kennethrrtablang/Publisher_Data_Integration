CREATE TABLE [dbo].[pdi_Client_Translation_Language_Missing_Log] (
    [Missing_ID]       UNIQUEIDENTIFIER NOT NULL,
    [Client_ID]        INT              NOT NULL,
    [LOB_ID]           INT              NOT NULL,
    [Document_Type_ID] INT              NOT NULL,
    [en-CA]            NVARCHAR (MAX)   COLLATE SQL_Latin1_General_CP1_CS_AS NULL,
    [Last_Updated]     DATETIME         CONSTRAINT [DF_pdi_Client_Translation_Language_Missing_Log_Last_Updated] DEFAULT (getutcdate()) NOT NULL,
    PRIMARY KEY CLUSTERED ([Missing_ID] ASC)
);

