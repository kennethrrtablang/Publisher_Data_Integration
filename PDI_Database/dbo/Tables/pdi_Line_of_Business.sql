CREATE TABLE [dbo].[pdi_Line_of_Business] (
    [LOB_ID]          INT          IDENTITY (1, 1) NOT NULL,
    [Client_ID]       INT          NOT NULL,
    [Business_ID]     INT          NOT NULL,
    [LOB_Code]        VARCHAR (50) NOT NULL,
    [LOB_Description] VARCHAR (50) NOT NULL,
    [GROUP_CODE]      VARCHAR (50) NOT NULL,
    [SUB_GROUP_CODE]  VARCHAR (50) NOT NULL,
    CONSTRAINT [PK_Line_of_Business] PRIMARY KEY CLUSTERED ([LOB_ID] ASC),
    CONSTRAINT [FK_Line_of_Business_Publisher_Client] FOREIGN KEY ([Client_ID]) REFERENCES [dbo].[pdi_Publisher_Client] ([Client_ID])
);

