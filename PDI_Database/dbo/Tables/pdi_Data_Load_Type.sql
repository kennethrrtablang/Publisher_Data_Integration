CREATE TABLE [dbo].[pdi_Data_Load_Type] (
    [Load_Type]   NVARCHAR (50)  NOT NULL,
    [Description] NVARCHAR (150) NOT NULL,
    [Data_Set]    NVARCHAR (50)  NOT NULL,
    CONSTRAINT [PK_pdi_Data_Load_Type] PRIMARY KEY CLUSTERED ([Load_Type] ASC)
);

