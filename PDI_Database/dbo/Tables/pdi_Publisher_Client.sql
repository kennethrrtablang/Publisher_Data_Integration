CREATE TABLE [dbo].[pdi_Publisher_Client] (
    [Client_ID]                  INT            IDENTITY (1000, 1) NOT NULL,
    [Custodian_ID]               INT            NOT NULL,
    [Company_ID]                 INT            NOT NULL,
    [Client_Code]                NVARCHAR (50)  NOT NULL,
    [Company_Name]               NVARCHAR (50)  NOT NULL,
    [Notification_Email_Address] NVARCHAR (150) NULL,
    CONSTRAINT [PK_Publisher_Client] PRIMARY KEY CLUSTERED ([Client_ID] ASC),
    CONSTRAINT [FK_Publisher_Client_Data_Custodian] FOREIGN KEY ([Custodian_ID]) REFERENCES [dbo].[pdi_Data_Custodian] ([Custodian_ID])
);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_pdi_Publisher_Client]
    ON [dbo].[pdi_Publisher_Client]([Company_ID] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_pdi_Publisher_Client_1]
    ON [dbo].[pdi_Publisher_Client]([Client_Code] ASC);


GO
EXECUTE sp_addextendedproperty @name = N'sys_data_classification_recommendation_disabled', @value = 1, @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'pdi_Publisher_Client', @level2type = N'COLUMN', @level2name = N'Notification_Email_Address';

