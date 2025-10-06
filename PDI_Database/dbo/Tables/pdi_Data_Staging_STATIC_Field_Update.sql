CREATE TABLE [dbo].[pdi_Data_Staging_STATIC_Field_Update] (
    [Job_ID]     INT           NOT NULL,
    [Field_Name] NVARCHAR (50) NOT NULL,
    CONSTRAINT [PK_pdi_Data_Staging_STATIC_Field_Update] PRIMARY KEY CLUSTERED ([Job_ID] ASC, [Field_Name] ASC),
    CONSTRAINT [FK_pdi_Data_Staging_STATIC_Field_Update_pdi_Processing_Queue_Log] FOREIGN KEY ([Job_ID]) REFERENCES [dbo].[pdi_Processing_Queue_Log] ([Job_ID])
);

