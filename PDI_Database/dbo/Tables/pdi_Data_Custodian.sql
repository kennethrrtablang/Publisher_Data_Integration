CREATE TABLE [dbo].[pdi_Data_Custodian] (
    [Custodian_ID]        INT            IDENTITY (1, 1) NOT NULL,
    [Data_Custodian_Name] NVARCHAR (250) NOT NULL,
    CONSTRAINT [PK_Data_Custodian] PRIMARY KEY CLUSTERED ([Custodian_ID] ASC)
);

