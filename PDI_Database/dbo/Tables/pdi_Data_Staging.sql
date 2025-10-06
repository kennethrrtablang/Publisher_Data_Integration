CREATE TABLE [dbo].[pdi_Data_Staging] (
    [Job_ID]        INT            NOT NULL,
    [Code]          VARCHAR (50)   NOT NULL,
    [Sheet_Name]    VARCHAR (50)   NOT NULL,
    [Item_Name]     VARCHAR (255)  NOT NULL,
    [Row_Number]    INT            DEFAULT ((0)) NOT NULL,
    [Column_Number] INT            DEFAULT ((0)) NOT NULL,
    [Value]         NVARCHAR (MAX) NULL,
    CONSTRAINT [PK_pdi_Data_Staging] PRIMARY KEY CLUSTERED ([Job_ID] ASC, [Code] ASC, [Sheet_Name] ASC, [Item_Name] ASC, [Row_Number] ASC, [Column_Number] ASC),
    CONSTRAINT [FK_pdi_Data_Staging_pdi_Processing_Queue_Log] FOREIGN KEY ([Job_ID]) REFERENCES [dbo].[pdi_Processing_Queue_Log] ([Job_ID])
);

