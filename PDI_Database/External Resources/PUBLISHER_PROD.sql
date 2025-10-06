CREATE EXTERNAL DATA SOURCE [PUBLISHER_PROD]
    WITH (
    TYPE = RDBMS,
    LOCATION = N'publisher.database.windows.net',
    DATABASE_NAME = N'PUBLISHER_PROD',
    CREDENTIAL = [AppCredential]
    );

