CREATE TABLE [dbo].[pdi_Client_Billable_Activity] (
    [ID]                  INT            IDENTITY (1, 1) NOT NULL,
    [Custodian_ID]        INT            NOT NULL,
    [Data_Custodian_Name] NVARCHAR (250) NOT NULL,
    [Client_ID]           INT            NOT NULL,
    [Company_Name]        NCHAR (50)     NOT NULL,
    [Document_Type_ID]    INT            NOT NULL,
    [Document_Type_Name]  VARCHAR (50)   NOT NULL,
    [Document_Count]      INT            NOT NULL,
    [Filing_Date]         NVARCHAR (50)  NOT NULL,
    [Is_Billable]         INT            NOT NULL,
    CONSTRAINT [PK_pdi_Client_Billable_Activity_1] PRIMARY KEY CLUSTERED ([ID] ASC)
);

