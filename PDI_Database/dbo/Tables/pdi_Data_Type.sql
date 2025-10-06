CREATE TABLE [dbo].[pdi_Data_Type] (
    [Data_Type_ID]   INT          IDENTITY (1, 1) NOT NULL,
    [Data_Type_Name] VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Data_Type] PRIMARY KEY CLUSTERED ([Data_Type_ID] ASC)
);

