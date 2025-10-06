using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Extensions.Logging;
using Nancy.Json;
using Publisher_Data_Operations.Extensions;
using Publisher_Data_Operations.Helper;

namespace Publisher_Data_Operations
{
    public class Orchestration : IDisposable
    {
        DBConnection _dbCon = null;
        DBConnection _dbConPUB = null;
        Processing _proc = null;
        PDIStream _processStream = null;
        PDIStream _templateStream = null;
        Logger _log = null;
        FileIntegrityCheck _fileCheck = null;
        private bool disposedValue;
        private bool _runningLocal = false;
     

        public int FileID { get; private set; }
        public string ErrorMessage { get; private set; }
        public Guid RunID { get; private set; }

        public int RetryCount { get; private set; } // added to handle transient errors and allowing a retry of the same file

        public string NotificationEmailAddress { get; private set; }
        public string FileRunID {
            get
            {
                if (_processStream != null && _processStream.PdiFile != null)
                    return _processStream.PdiFile.FileRunID;
                else
                    return null;
            }
        }

        public bool FileStatus
        {
            get
            {
                if (_fileCheck != null && _fileCheck.validationList != null && _fileCheck.validationList.ExHelper != null)
                    return _processStream.PdiFile.IsValid && _fileCheck.validationList.ExHelper.IsValidData;
                else
                    return _processStream.PdiFile.IsValid;
            }
        }

        public PDIFile GetFile => _processStream != null ? _processStream.PdiFile : null;
        public Logger GetLog => _log;
        public DBConnection GetPDIConnection => _dbCon;

        public ProcessStatusObject GetProcessStatus {
            get
            {
                if (_proc != null)
                    return _proc.ProcStatus;
                else
                    return null;
            }
        }
        public FileDetailsObject GetFileDetails
        {
            get
            {
                if (_processStream != null && _processStream.PdiFile != null)
                    return _processStream.PdiFile.GetFileDetails;
                else
                    return null;
            }
        }
        public Orchestration(object con, object con2 = null)
        {
            _dbCon = new DBConnection(con);
            if (con2 != null)
                _dbConPUB = new DBConnection(con2);
        }

        public Orchestration(object con, object con2, PDIStream processFile)
        {
            _dbCon = new DBConnection(con);
            if (con2 != null)
                _dbConPUB = new DBConnection(con2);

            _processStream = processFile;
            if (_processStream != null && _processStream.PdiFile != null && _processStream.PdiFile.FileID.HasValue)
                FileID = (int)_processStream.PdiFile.FileID;
        }

        public Orchestration(object con, object con2, string fileID)
        {
            _dbCon = new DBConnection(con);
            _dbConPUB = new DBConnection(con2);
            if (int.TryParse(fileID, out int intFileID))
            {
                FileID = intFileID;
                _log = new Logger(_dbCon, FileID);
            }
            else
                FileID = -1;
        }

        [Obsolete("Only used by Program for Testing")]
        public bool ProcessFile(string processFile, string templatePath, int retryCount = 0)
        {
           
            PDIFile pdiFile = new PDIFile(processFile, _dbCon, true);
            _runningLocal = true; // this is the only time that orchestration should import to Publisher during internalProcessFiles
            _processStream = new PDIStream(pdiFile);
            _templateStream = new PDIStream(Path.Combine(templatePath, pdiFile.GetDefaultTemplateName()));

            //bool tempReturn = InternalProcessFile();
            //if (_log != null)
            //    _log.WriteErrorsToDB(); // make sure any remaining errors are logged
            return ProcessFile(_processStream, _templateStream, retryCount);
        }

        public void HandleEventMessage(EventGridEvent eventGridEvent)
        {
            if (eventGridEvent.EventType == "CreateCustomerEvent")
            {
                var eventData = new JavaScriptSerializer().Deserialize<CreateCustomerEvent>(eventGridEvent.Data.ToString());

                int publisherCompanyId = 0;
                int pdiclientId = 0;
                try
                {
                    var pubSql = "INSERT INTO [dbo].[COMPANY] " +
                        "([CUSTOMER_CODE],[COMPANY_NAME],[IS_ACTIVE],[FEED_COMPANY_ID],[COMPANY_NAME_FR]) VALUES " +
                        "(@companyId,@customerCode,@companyName,1,@feedId,@companyName);" +
                        "SELECT SCOPE_IDENTITY();";

                    object returnObj = _dbConPUB.ExecuteScalar(pubSql, new Dictionary<string, object>(3) {
                    { "@customerCode", eventData.CustomerCode },
                    { "@companyName", eventData.CustomerName },
                    { "@feedId", 0 }
                     });

                    if (returnObj != null)
                    {
                        int.TryParse(returnObj.ToString(), out publisherCompanyId);
                    }


                    if (publisherCompanyId != 0)
                    {
                        var pdiSql1 = "INSERT INTO [dbo].[pdi_Publisher_Client]" +
                                "([Custodian_ID],[Company_ID],[Client_Code],[Company_Name],[Notification_Email_Address])" +
                                "VALUES (2 ,@companyId ,@clientCode ,@companyName ,@email)\r\n" +
                                "SELECT SCOPE_IDENTITY();";

                        object returnObjPdi = _dbCon.ExecuteScalar(pdiSql1, new Dictionary<string, object>(4) {
                    { "@companyId", publisherCompanyId },
                    { "@clientCode", eventData.CustomerCode },
                    { "@companyName",eventData.CustomerName },
                    { "@email", eventData.Email }
                     });

                        if (returnObjPdi != null)
                        {
                            int.TryParse(returnObjPdi.ToString(), out pdiclientId);
                        }
                    }

                    if(pdiclientId != 0 && publisherCompanyId!=0)
                    {
                          var pdiSql2 = "INSERT INTO [dbo].[pdi_Line_of_Business]" +
                           " ([Client_ID],[Business_ID],[LOB_Code],[LOB_Description],[GROUP_CODE],[SUB_GROUP_CODE])" +
                           " VALUES (@clientId,@businessId,@lobCode,'@description',@groupCode,@groupCode)";

                         _dbCon.ExecuteNonQuery(pdiSql2, out int rowCount, new Dictionary<string, object>(5) {
                           { "@clientId", pdiclientId },
                           { "@businessId", eventData.BusinessId }, // notify to IDP for inclusion
                           { "@lobCode", "NLOB" },
                           { "@description", "Default line of business" },
                           { "@groupCode", eventData.CustomerName }
                         });

                        var pubSql2 = "update [dbo].[COMPANY]  SET [FEED_COMPANY_ID] = @feedId where [COMPANY_ID] = @companyId";

                        _dbConPUB.ExecuteNonQuery(pubSql2,out int rowCount2 , new Dictionary<string, object>(2) {
                    { "@feedId", pdiclientId },
                    { "@companyId", publisherCompanyId }
                     });

                    }
                }
                catch(Exception ex)
                {

                }

            }
        }

        public bool ProcessFile(PDIStream processStream, PDIStream templateStream, int retryCount, ILogger logger = null)
        {
            RetryCount = retryCount;
            if (retryCount > 0)
                FileID = processStream.PdiFile.Retry(RetryCount);
            else
                FileID = processStream.PdiFile.ProcessAfterLoadOnly(); //InsertFileReceipt(processStream.PdiFile.OnlyFileName); ////(processStream.PdiFile.FileID.HasValue) ? (int)processStream.PdiFile.FileID : -1; 

            _processStream = processStream;
            _templateStream = templateStream;

            if (FileID > 0)
            {
                //if (!loadOnly && !processStream.PdiFile.DataID.HasValue)
                //    _processStream.PdiFile = new PDIFile(FileID, _dbCon, null, loadOnly, _log);
                return InternalProcessFile(logger);
            }
            return false;
        }

        private bool InternalProcessFile(ILogger logger) //int fileID, 
        {
            if (FileID < 0 || _processStream is null || _processStream.PdiFile is null)
                return false;

            if (_log is null)
                _log = new Logger(_dbCon, _processStream.PdiFile);
            else
                _log.UpdateParams(_processStream.PdiFile);

            _log.logger = logger;

            //FileID = fileID;
            int jobID = -1;


            //processStream.PdiFileName = new PDIFileName(fileID, _dbCon, loadOnly, _log);
            NotificationEmailAddress = LoadNotifiactionEmail();

            if (_processStream.PdiFile.IsValid && _processStream.PdiFile.IsValidFileName)
            {
                try {

                    jobID = _processStream.PdiFile.JobID.HasValue ? (int)_processStream.PdiFile.JobID : -1;
                    _proc = new Processing((int)_processStream.PdiFile.JobID, _dbCon);
                    if (_processStream.SourceStream is null || _processStream.SourceStream.Length == 0)
                    {
                        Logger.AddError(_log, $"Failed to load source file {_processStream.PdiFile.OnlyFileName}");
                        _proc.SetProcessingQueue(ProcessingStage.Error);
                        return false;
                    }
                        
                    if (RetryCount > 0 && !Cleanup(jobID)) // changed order so cleanup isn't tried unless RetryCount > 0
                    {
                        Logger.AddError(_log, "Failed to clear database for retry - aborting import");
                        _proc.SetProcessingQueue(ProcessingStage.Error);
                        return false;
                    }                                          
                    _proc.SetProcessingQueue(ProcessingStage.Job_Start);
                    
                    if (_processStream.PdiFile.GetDataType != DataTypeID.FSMRFP) // FSMRFP type no validation (validate static)
                    {
                        //_proc.SetProcessingQueue(ProcessingStage.Validation_Start);
                        _fileCheck = new FileIntegrityCheck(_processStream, _templateStream, _dbCon, _log); //ICOM_FTI_NLOB_STATIC_ETF_20210210_101722_1.xlsm"

                        if (!_fileCheck.FileCheck())
                        {
                            _proc.SetProcessingQueue(ProcessingStage.Validation_End);
                            _proc.SetProcessingQueue(ProcessingStage.Validation);
                            return false;
                        }
                        _proc.SetProcessingQueue(ProcessingStage.Validation_End);
                    }
                    Extract ex = new Extract(_processStream, _dbCon, _log, -1);
                    if (!ex.RunExtract())
                    {
                        _proc.SetProcessingQueue(ProcessingStage.Extract_End);
                        _proc.SetProcessingQueue(ProcessingStage.Error);
                        return false;
                    }

                    _proc.SetProcessingQueue(ProcessingStage.Extract_End);
                    DocumentProcessing dp = new DocumentProcessing(_dbCon, _log);
                    if (!dp.processStaging(_processStream.PdiFile))
                    {
                        _proc.SetProcessingQueue(ProcessingStage.Error);
                        return false;
                    }
                    Transform tran = new Transform(_processStream.PdiFile, _dbCon, _log,_dbConPUB);
                    if (!tran.RunTransform())
                    {
                        _proc.SetProcessingQueue(ProcessingStage.Transform_End);
                        _proc.SetProcessingQueue(ProcessingStage.Error);
                        return false;
                    }
                    _proc.SetProcessingQueue(ProcessingStage.Transform_End);

                    if ((_processStream.PdiFile.GetDataType == DataTypeID.FSMRFP) && !tran.GetTransTable.ValidateXML(_log))
                    {
                        _proc.SetProcessingQueue(ProcessingStage.Error);
                        return false;
                    }

                    if (_runningLocal && _dbConPUB != null) // bug18742 - check if we are running locally as that is the only time we should be importing here
                    {
                        if (!PublisherImport(jobID))
                            return false;
                    }
                    else
                        _proc.SetProcessingQueue(ProcessingStage.Import_Ready);
                }
                catch (Exception e)
                {
                    LogError("Orchestration encountered an error: " + e.Message + Environment.NewLine + e.StackTrace, logger);
                    return false;
                }
                return true;
            }
            return true;
        }

        public bool PublisherImport(int jobID, ILogger logger = null)
        {
            if (_dbConPUB != null)
            {
                if (_proc is null)
                    _proc = new Processing(jobID, _dbCon);

                if (!CheckImportSP())
                    LogError($"Unable to check existing IMPORT DATA stored procedure or failure on create {_dbConPUB.LastError ?? string.Empty}", logger);

                DBConnection newPubCon = new DBConnection(_dbConPUB.GetSqlConnection()); // get a new DB Connection that uses an SQLConnection Object since we need to maintain the connection for the temporary table
                if (!LoadTempTable(newPubCon, jobID))
                {
                    _proc.SetProcessingQueue(ProcessingStage.Load_End);
                    _proc.SetProcessingQueue(ProcessingStage.Error);
                    return false;
                }
                _proc.SetProcessingQueue(ProcessingStage.Load_End);
                if (!ImportTempTable(newPubCon))
                {
                    _proc.SetProcessingQueue(ProcessingStage.Import_End);
                    _proc.SetProcessingQueue(ProcessingStage.Error);
                    return false;
                }
                newPubCon.DisposeConnection();
                newPubCon = null;

                if (_processStream.PdiFile.GetDataType == DataTypeID.STATIC) // Changed to STATIC
                    SetDocumentName();

                _proc.SetProcessingQueue(ProcessingStage.Import_End);
                _proc.SetProcessingQueue(ProcessingStage.Complete);
            }
            return true;
        }

        public bool LoadStoredProcedure(int jobID, string spName = "sp_LoadTransformedData")
        {
            if (!_dbCon.ExecuteNonQuery(spName, out int rows, new Dictionary<string, object>(1) { { "@jobID", jobID } }, 300, true))
            {
                Logger.AddError(_log, _dbCon.LastError);
                return false;
            }
            return true;    
        }

        public bool ImportStoredProcedure()
        {
            return ImportStoredProcedure(_processStream.PdiFile);
        }

        public bool ImportStoredProcedure(PDIFile pdiFile)
        {
            if (pdiFile.JobID.HasValue && pdiFile.CompanyID.HasValue && pdiFile.DocumentTypeID.HasValue)
                return ImportStoredProcedure((int)pdiFile.JobID, (int)pdiFile.CompanyID, (int)pdiFile.DocumentTypeID);
            else
            {
                LogError($"Unable to import to Publisher one or more of JobID ({pdiFile.JobID}), CompanyID ({pdiFile.CompanyID}), or DocumentTypeID ({pdiFile.DocumentTypeID}) is null");
                return false;
            }
        }

        /// <summary>
        /// This is the Import process that the Azure Storage Queue uses - in this case there won't be any streams available but we'll want to deal with the import start and stop details.
        /// </summary>
        /// <param name="jobID"></param>
        /// <param name="companyID"></param>
        /// <param name="docTypeID"></param>
        /// <returns></returns>
        public bool ImportStoredProcedure(string jobID, string companyID, string docTypeID)
        {
            if (int.TryParse(jobID, out int job) && int.TryParse(companyID, out int company) && int.TryParse(docTypeID, out int docType))
            {
                _proc = new Processing(job, _dbCon);
                //_proc.SetProcessingQueue(ProcessingStage.Import_Start);
                if (ImportStoredProcedure(job, company, docType))
                {
                    _proc.SetProcessingQueue(ProcessingStage.Import_End);
                    return true;
                }
                else
                    LogError($"Failed to import to Publisher for Job_ID ({jobID}) - see other logged errors for details");

                if (_log != null)
                    _log.WriteErrorsToDB();

                return false;
            }
            else
                LogError($"Unable to import to Publisher one or more of Job_ID ({jobID}), Company_ID ({companyID}), or DocumentType_ID ({docTypeID}) is not an integer");

            if (_log != null)
                _log.WriteErrorsToDB();

            return false;
        }

        public bool ImportStoredProcedure(int jobID, int companyID, int docTypeID)
        {
            if (_dbConPUB != null)
            { //sp_pdi_IMPORT_DATA_v2
                var cmdResult = _dbConPUB.ExecuteScalar("sp_pdi_IMPORT_DATA", new Dictionary<string, object>(3) {
                    { "@Job_ID", jobID },
                    { "@companyID", companyID },
                    { "@documentTypeID", docTypeID }
                }, 0, true);

                if (cmdResult.ToString() != "Complete")
                {
                    LogError("Import to Publisher failed: " + _dbConPUB.LastError);
                    return false;
                }
            }
            return true;
        }

        public bool CheckImportSP()
        {
            var cmdResult = _dbConPUB.ExecuteScalar("SELECT COUNT(*) AS TheCount FROM sys.objects WHERE type = 'P' AND name = 'sp_pdi_IMPORT_DATA_temp'");
            if (int.TryParse(cmdResult.ToString(), out int rows) && rows < 1)
            {
                if (_dbConPUB.ExecuteNonQuery(StoredProcedure.ImportDataSP(), out rows))
                    return true; // stored procedure was missing but was recreated successfully
            }
            else
                return true; // stored procedure exists
            return false;
        }

        /// <summary>
        /// Executes the database command to delete the import from temp db if the creation date isn't today - exclusively for the staging environment to replace the production import stored procedure
        /// </summary>
        /// <returns></returns>
        public void ClearImportSP()
        {
            var cmdResult = _dbConPUB.ExecuteNonQuery(StoredProcedure.ClearSPByDate(), out int rows);
            if (!cmdResult)
                Logger.AddError(_log, "SP01: There was an issue clearing the staging sp_pdi_IMPORT_DATA_temp stored procedure");
            if (rows > 0)
                Logger.AddError(_log, $"SP02: The {_dbConPUB.GetDatabase} Import SP was outdated and deleted - this is a notification message only");
        }

        public bool LoadTempTable(DBConnection dbConPub, int jobID, bool keepTable = false) // keepTable for testing import table
        {
            string tempTable = keepTable ? "pdi_import_source_"+ jobID.ToString() : "#pdi_import_source"; // + jobID.ToString();
            string sql = $"DROP TABLE IF EXISTS {tempTable}; CREATE TABLE {tempTable} ([Business_Name] [varchar](500) NOT NULL, [Line_Of_Business_Code] [varchar](50) NOT NULL, [Feed_Type_Name] [varchar](50) NOT NULL, [Document_Number] [nvarchar](50) NOT NULL, [Field_Name] [nvarchar](50) NOT NULL, [Culture_Code] [nvarchar](50) NOT NULL, [Content] [nvarchar](max) NOT NULL, [isTextField] [bit] NOT NULL,  [isTableField] [bit] NOT NULL,  [isChartField] [bit] NOT NULL, [Document_FileName_EN] [varchar](500) NULL, [Document_FileName_FR] [varchar](500) NULL, [Sort_Order] [int] NULL, [IsActiveStatus] [bit] NULL )";
            if (!dbConPub.ExecuteNonQuery(sql, out int rows, null, -1, false, false))
            {
                Logger.AddError(_log, "Unable to create temp table - " + _dbCon.LastError);
                return false;
            }
            using (System.Data.DataTable dt = new System.Data.DataTable($"temp_import_{jobID}"))
            {
                string docUpdates = "NULL as Document_FileName_EN, NULL as Document_FileName_FR, NULL as Sort_Order";
                if (_processStream.PdiFile.GetDataType == DataTypeID.STATIC) // if a STATIC file is running then allow the document names and sort order to update
                    docUpdates = "PD.Document_FileName_EN, PD.Document_FileName_FR, PD.Sort_Order";

                if (!_dbCon.LoadDataTable($"SELECT LOB.Group_Code AS Business_Name, LOB.SUB_GROUP_CODE AS Line_Of_Business_Code, DT.Feed_Type_Name, TD.Document_Number, TD.Field_Name, TD.Culture_Code, TD.[Content], TD.isTextField, TD.isTableField, TD.isChartField, PD.IsActiveStatus, {docUpdates} FROM pdi_Transformed_Data AS TD INNER JOIN pdi_Processing_Queue_Log AS PQL ON TD.Job_ID = PQL.Job_ID INNER JOIN pdi_Line_of_Business AS LOB ON PQL.LOB_ID = LOB.LOB_ID INNER JOIN pdi_Document_Type AS DT ON PQL.Document_Type_ID = DT.Document_Type_ID INNER JOIN pdi_Publisher_Client AS PC ON PQL.Client_ID = PC.Client_ID LEFT OUTER JOIN pdi_Publisher_Documents AS PD ON PQL.LOB_ID = PD.LOB_ID AND PD.Document_Type_ID = PQL.Document_Type_ID AND PD.Client_ID = PQL.Client_ID AND PD.Document_Number = TD.Document_Number WHERE TD.Job_ID = @jobID AND (LTRIM(RTRIM(TD.Field_Name)) <> '') AND TD.[Content] IS NOT NULL ORDER BY TD.Document_Number, TD.Field_Name, TD.Culture_Code;",
                    new Dictionary<string, object>(1) { { "@jobID", jobID } }, dt))
                {
                    Logger.AddError(_log, $"Failed to retrieve temporary data for Job_ID {jobID} - {_dbCon.LastError}");
                    return false;
                }
                if (!dbConPub.BulkCopy(tempTable, dt, false, false))
                {
                    Logger.AddError(_log, $"Failed to BulkCopy temporary data for Job_ID {jobID} - {dbConPub.LastError}");
                    return false;
                }
            }
            return true;
        }

        public bool ImportTempTable(DBConnection dbConPub)
        {
            if (dbConPub != null)
            { 
                var cmdResult = dbConPub.ExecuteScalar("sp_pdi_IMPORT_DATA_temp", null, 0, true);

                if (cmdResult.ToString() != "Complete")
                {
                    LogError($"Import to Publisher from temp table failed: { dbConPub.LastError ?? cmdResult.ToString()}");
                    return false;
                }
            }
            return true;
        }

        public bool SetDocumentName()
        {
            using (System.Data.DataTable dt = new System.Data.DataTable($"Template"))
            {
                if (!_dbCon.LoadDataTable("SELECT Document_Name_Field, Template_Code, Template_layout_Name FROM pdi_Publisher_Document_Templates WHERE Document_Name_Field IS NOT NULL AND Client_ID = @clientID AND Document_Type_ID = @docTypeID;",
                new Dictionary<string, object>(2) {
                    { "@clientID", _processStream.PdiFile.ClientID } ,
                    { "@docTypeID", _processStream.PdiFile.DocumentTypeID }
                }, dt))
                {
                    Logger.AddError(_log, $"Unable to retrieve document name field for {_processStream.PdiFile.ClientName} with documents of type {_processStream.PdiFile.DocumentType} - {_dbCon.LastError}");
                    return false;
                }
                if (dt.Rows.Count != 1)
                    return false;

                if (!_dbConPUB.ExecuteNonQuery("sp_UPDATE_DOCUMENT_NAME", out int rows, new Dictionary<string, object>(3) {
                        { "@fieldName", dt.Rows[0]["Document_Name_Field"] },
                        { "@documentTemplateCode", dt.Rows[0]["Template_Code"] },
                        { "@documentTemplateLayoutName", dt.Rows[0]["Template_layout_Name"] }
                    }, 0, true))
                {
                    Logger.AddError(_log, $"Unable to update PUBLISHER document name for {_processStream.PdiFile.ClientName} with documents of type {_processStream.PdiFile.DocumentType} - {_dbConPUB.LastError}");
                    return false;
                }
            }
            return true;
        }

        public bool Cleanup(int jobID)
        {
            if (!_dbCon.ExecuteNonQuery("DELETE FROM [pdi_Data_Staging] WHERE Job_ID=@jobID; DELETE FROM [pdi_Data_Staging_STATIC_Translation_Language] WHERE Job_ID=@jobID; DELETE FROM [pdi_Data_Staging_STATIC_Content_Scenario] WHERE Job_ID=@jobID; DELETE FROM [pdi_Data_Staging_STATIC_Field_Update] WHERE Job_ID=@jobID; DELETE FROM [pdi_Transformed_Data] WHERE Job_ID=@jobID; DELETE FROM [pdi_File_Validation_Log] WHERE File_ID=@fileID; DELETE FROM [pdi_Client_Translation_Language_Missing_Log_Details] WHERE Job_ID=@jobID; UPDATE pdi_Processing_Queue_LOG SET Job_Status = null, Job_Start = null, Extract_End = null, Transform_End = null, Load_End = null, Import_End = null, Validation_End = null, Process_Source = null WHERE Job_ID = @jobID;", out int rows, new Dictionary<string, object>(2) {
                { "@jobID", jobID },
                { "@fileID", FileID }
            }))
            {
                Logger.AddError(_log, "Unable to clean database - " + _dbCon.LastError);
                return false;
            }
                

            return true;
        }

        public string LoadNotifiactionEmail()
        {
            if (NotificationEmailAddress.IsNaOrBlank())
                if (_dbCon != null && _processStream != null && _processStream.PdiFile != null && _processStream.PdiFile.ClientID.HasValue)
                    NotificationEmailAddress =  _dbCon.ExecuteScalar("SELECT Notification_Email_Address FROM [pdi_Publisher_Client] WHERE Client_ID = @clientID", new Dictionary<string, object>(1) { { "@clientID", _processStream.PdiFile.ClientID } }).ToString();

            return NotificationEmailAddress;
        }
        private void LogError(string errorMessage, ILogger logger = null)
        {
            ErrorMessage = errorMessage;
            if (_log is null && _dbCon != null && _processStream != null && _processStream.PdiFile != null && _processStream.PdiFile.FileID.HasValue)
                _log = new Logger(_dbCon, _processStream.PdiFile);
            if (_log != null)
            {
                _log.AddError(errorMessage);
                _log.WriteErrorsToDB();
            }
            else
            {
                if (logger != null)
                    logger.LogError(errorMessage);
                else
                    Console.WriteLine("Log Missing - Error: " + errorMessage);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (_log != null)
                        _log.Dispose();
                }
                _dbCon = null;
                _dbConPUB = null;
                _proc = null;
                _fileCheck = null;

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Orchestration()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }

    public class CreateCustomerEvent
    {
        public string CustomerName { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerCodes { get; set; }
        public string AllowedProductCodes { get; set; }
        public string AllowedApplicationCodes { get; set; }
        public string CountryCode { get; set; }
        public int BusinessId { get; set; }
        public string Email { get; set; }
    }
}
