using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Publisher_Data_Operations.Extensions;

namespace Publisher_Data_Operations.Helper
{

    public class FileDetailsObject
    {
        public string DataCustodian { get; set; }
        public string DataCustodianID { get; set; }
        public string CompanyName { get; set; }
        public string CompanyID { get; set; }
        public string DocumentType { get; set; }
        public string DocumentTypeID { get; set; }
        public string DataType { get; set; }
        public string DataTypeID { get; set; }
        public DateTime? CreationDateTime { get; set; } 
        public string Version { get; set; }
        public string Code { get; set; }
        public string Note { get; set; }
        public bool FileNameIsValid { get; set; }
        public bool FileIsValid { get; set; }

        public FileDetailsObject()
        {

        }

        public FileDetailsObject(PDIFile pdiFile)
        {
            SetValues(pdiFile);
        }

        public void SetValues(PDIFile pdiFile)
        {
            if (pdiFile is null)
                throw new ArgumentNullException("A PDIFile is required", "pdiFile");
            DataCustodian = pdiFile.DataCustodian;
            DataCustodianID = pdiFile.DataCustodianID.ToString();
            CompanyName = pdiFile.ClientName;
            CompanyID = pdiFile.CompanyID.ToString();
            DocumentType = pdiFile.DocumentType;
            DocumentTypeID = pdiFile.DocumentTypeID.ToString();
            DataType = pdiFile.DataType;
            DataTypeID = pdiFile.DataTypeID.ToString();
            CreationDateTime = pdiFile.CreationDateTime;
            Version = pdiFile.Version;
            Code = pdiFile.Code;
            Note = pdiFile.Note;
            FileNameIsValid = pdiFile.IsValidFileName;
            FileIsValid = pdiFile.IsValid;
        }
    }


    public class PDIFile
    {
        public const char FILE_DELIMITER = '_';
        private int _fileID = -1;
        private bool _loadOnly = false;
        private Logger _log = null;
        private Processing _proc = null;
        private Dictionary<string, string> _parameters = null;


        //private string[] _fileNameArray = null;
        private string _fileName = null;
        private Dictionary<string, int?> IdValues = new Dictionary<string, int?> { { "Custodian_ID", null }, { "Client_ID", null }, { "Company_ID", null }, { "LOB_ID", null }, { "Data_Type_ID", null }, { "Document_Type_ID", null }, { "File_ID", null } };
        private DBConnection dbConn = null;
        //private string[] columnNames = new string[] { "Custodian_ID", "Client_ID", "Company_ID", "LOB_ID", "Data_Type_ID", "Document_Type_ID", "File_ID" }; // list of column names we are getting from the database


        public FileDetailsObject GetFileDetails => new FileDetailsObject(this);

        public int? DataID { get; private set; } // => (IdValues is null) ? null : IdValues.ContainsKey("Data_ID") ? IdValues["Data_ID"] : -1; //
        public int? JobID { get; set; }
        public int? BatchID { get; set; }
        public string FileRunID { get; set; }
        public int? FileID
        {
            get
            {
                if (_fileID > 0)
                    return _fileID;
                else
                    return null;
            }
            private set
            {
                if (value.HasValue)
                {
                    _fileID = (int)value;
                    if (_log != null)
                        _log.FileID = _fileID;
                }
                else
                    _fileID = -1;
            }
        }// : IdValues["File_ID"];
        public string DataCustodian { get; private set; } //=> (_fileNameArray is null) ? null : _fileNameArray[0];
        public int? DataCustodianID => (IdValues is null) ? null : IdValues["Custodian_ID"];
        public string ClientName { get; private set; } //=> (_fileNameArray is null) ? null : _fileNameArray[1];
        public int? ClientID => (IdValues is null) ? null : IdValues["Client_ID"];
        public int? CompanyID => (IdValues is null) ? null : IdValues["Company_ID"];
        public string LOB { get; private set; } //=> (_fileNameArray is null) ? null : _fileNameArray[2];
        public string Code { get; private set; }
        public int? LOBID => (IdValues is null) ? null : IdValues["LOB_ID"];
        public string DataType { get; private set; } //=> (_fileNameArray is null) ? null : _fileNameArray[3];
        public int? DataTypeID => (IdValues is null) ? null : IdValues["Data_Type_ID"];
        public string DocumentType { get; private set; } //=> (_fileNameArray is null) ? null : _fileNameArray[4];
        public int? DocumentTypeID => (IdValues is null) ? null : IdValues["Document_Type_ID"];

        public string CreationDate { get; private set; } //(_fileNameArray is null) ? null : _fileNameArray[5]; 
        public string CreationTime { get; private set; } //=> (_fileNameArray is null) ? null : _fileNameArray[6];
        public string Version { get; private set; } //=> (_fileNameArray is null) ? null : _fileNameArray[7];
        public string Note { get; private set; }
        //public string FileVersion { get; private set; } //=> (_fileNameArray is null) ? null : _fileNameArray[7];
        public DateTime? CreationDateTime
        {
            get
            {
                if (DateTime.TryParseExact(CreationDate + " " + CreationTime, "yyyyMMdd HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime theDate))
                    return theDate;
                return null;
            }
        }
        
        public string Extension { get => (OnlyFileName != null) ? Path.GetExtension(OnlyFileName) : null; }
        public bool IsValid { get; private set; } = false;
        public bool IsValidFileName
        {
            get
            {
                //if (!IsValid)
                //    return IsValid;
                if (!DataCustodianID.HasValue)
                    return false;
                if (!ClientID.HasValue)
                    return false;
                if (!LOBID.HasValue)
                    return false;
                if (!DataTypeID.HasValue)
                    return false;
                if (!DocumentTypeID.HasValue)
                    return false;
                if (!CreationDateTime.HasValue)
                    return false;
                if (Version.Length < 1 || !int.TryParse(Version, out int _))
                    return false;
                if (GetDataType == Extensions.DataTypeID.FSMRFP && (Code is null || Code.Length < 1) && (GetDocumentType == Extensions.DocumentTypeID.FS || GetDocumentType == Extensions.DocumentTypeID.MRFP || GetDocumentType == Extensions.DocumentTypeID.QPD)) // code is required for FSMRFP FS QPD and MRFP documents
                    return false;
                return true;
            }
        }
        public string ErrorMessage { get; private set; } //= null;
        public string FullPath { get; private set; } //= null;
        public string FileNameWithoutExtension { get => Path.GetFileNameWithoutExtension(OnlyFileName); }
        public string OnlyFileName 
        {
            get => _fileName;
            set 
            {
                //Use the setter to parse the filename and do some initial validation - TODO: the problem with using the setter is that the File_ID hasn't been determined yet so any SetErrors are not captured - we should add to the file receipt log first and then parse and update isvalid on the receipt log
                IsValid = true; //reset if we're passed a filename again
                FullPath = (value != null && value.Length > 0) ? Path.GetFullPath(value) : string.Empty;
                _fileName = Path.GetFileName(value);
                if (_fileName != null && Extension.Length > 0)
                {
                    string[] tempArray = Path.GetFileNameWithoutExtension(_fileName).ToUpper().Split(FILE_DELIMITER);
                    if (tempArray.Length < 8 || tempArray.Length > 10) // a valid file name will have 8 to 10 sections
                    {
                        if (!value.Contains("TEMPLATE")) // don't report an error on template files
                            SetError($"Invalid number of sections in {_fileName}, expected 8 to 10 but found {tempArray.Length}.");
                    }                      
                    else
                    {
                        int index = 0;
                        DataCustodian = tempArray[index];
                        index++;
                        ClientName = tempArray[index];
                        index++;
                        LOB = tempArray[index];
                        index++;
                        if ((tempArray.Length == 9 && int.TryParse(tempArray[tempArray.Length -1], out int _)) || tempArray.Length == 10) // Handles the new FS/MRFP format that includes a unique "Document Number" - But with the optional note we now need to find the version to determine if there is a note or not
                        {
                            Code = tempArray[index];
                            index++;
                        }
                        DataType = tempArray[index];
                        index++;
                        DocumentType = tempArray[index];
                        index++;
                        CreationDate = tempArray[index];
                        index++;
                        CreationTime = tempArray[index];
                        index++;
                        Version = tempArray[index];
                        index++;
                        if (index < tempArray.Length)
                            Note = tempArray[index];

                        if (CreationDateTime is null) // checks both the date and time for validity
                            SetError($"Unable to parse Creation Date in {_fileName}, with a Date and Time of {CreationDate} {CreationTime}.");
                        else if (!int.TryParse(Version, out int _))
                            SetError($"Unable to determine Version in {_fileName}, with a Version value of {Version}.");
                        else if (Extension.ToLower() != ".xlsx" && Extension.ToLower() != ".zip")
                            SetError($"Invalid Extension found in {_fileName}, with an Extension value of {Extension}.");
                        else if (GetDataType == Extensions.DataTypeID.FSMRFP && (Code is null || Code.Length < 1) && GetDocumentType != Extensions.DocumentTypeID.FSMRFP) // code is required for FSMRFP documents except when the document type is also FSMRFP (zip file)
                            SetError("Code is required when Submitting FSMRFP Files for FS or MRFP");

                    } 
                }
                else
                    SetError($"Unable to determine extension of {_fileName}.");
            }
        }

        /// <summary>
        /// Return the enum DataTypeID that matches the DataType
        /// </summary>
        public DataTypeID? GetDataType
        {
            get
            {
                if (Enum.TryParse<DataTypeID>(DataType, out DataTypeID _dataType))
                    return _dataType;
                else
                    return null;
            }
        }

        /// <summary>
        /// Return the enum DocumentTypeID that matches the DocumentType
        /// </summary>
        public DocumentTypeID? GetDocumentType
        {
            get
            {
                if (Enum.TryParse<DocumentTypeID>(DocumentType, out DocumentTypeID _documentType))
                    return _documentType;
                else
                    return null;
            }
        }

        public Dictionary<string, string> GetAllParameters()
        {
            if (_parameters is null)
            {
                if (JobID.HasValue && JobID > 0)
                    LoadParameters((int)JobID);
                else if (FileID.HasValue && FileID > 0)
                    _parameters = LoadFileParameters();
            }

            return _parameters;
        }

        public bool IsValidParameters(int jobID = -1)
        {
            if (_parameters is null)
                LoadParameters(jobID);

            if (_parameters != null && _parameters.ContainsKey("IsValidFileName") && _parameters.ContainsKey("IsValidDataFile"))
                return _parameters["IsValidFileName"].ToBool() && _parameters["IsValidDataFile"].ToBool();

            return false;
        }
 
        /// <summary>
        /// Create a new FileName instance based on the provided fileName - validate during construction
        /// </summary>
        /// <param name="fileName">The FileName to parse and validate</param>
        /// <param name="conn">Optional DBConnection</param>
        public PDIFile(string fileName, object conn = null, bool loadOnly = false, int fileID = -1, Logger log = null)
        {
            
            _fileID = fileID;
            _loadOnly = loadOnly;

            if (conn != null)
            {
                if (conn.GetType() == typeof(DBConnection))
                    dbConn = (DBConnection)conn;
                else
                    dbConn = new DBConnection(conn);
            }

            _log = log;
            if (_log is null)
                _log = new Logger(dbConn, this);
            OnlyFileName = fileName;  // does initial validation in setter - if a path is given remove it
            Process();
        }


        public PDIFile(int fileID, object conn, string processPath = null, bool loadOnly = false, Logger log = null)
        {
            _fileID = fileID;
            _loadOnly = loadOnly;

            
            if (conn.GetType() == typeof(DBConnection))
                dbConn = (DBConnection)conn;
            else
                dbConn = new DBConnection(conn);

            _log = log;
            if (_log is null)
                _log = new Logger(dbConn, this);

            if (processPath != null)
                OnlyFileName = Path.Combine(processPath, GetFileName());
            else
                OnlyFileName = GetFileName();

            Process();
        }

        public PDIFile(string fileName)
        {
            _loadOnly = true;
            OnlyFileName = fileName;
        }

        public int ProcessAfterLoadOnly()
        {
            _loadOnly = false;
            JobID = -1;
            Process(); // in this case we are going to re
            return _fileID;
        }

        private void Process()
        {
            InsertReceiptLog(); // add the file to the receipt log - will not be added if running in loadOnly

            if (IsValid)
            {
                if (GetDataType is null)
                    SetError($"Unable to determine valid Data Type for {OnlyFileName} from {DataType}.");
                else if (GetDocumentType is null)
                    SetError($"Unable to determine valid Document Type for {OnlyFileName} from {DocumentType}.");
                else if (dbConn != null) // no point in checking the database if validation has already failed or we don't have a connection
                {
                    //if (_loadOnly)
                    //    IdValues.Add("Data_ID", null);
                    if (!LoadIDs() && FileID.HasValue && FileID >= 0 && !_loadOnly) // don't log the error from LoadID if we don't have a valid FileID or are loadOnly
                        SetError($"Unable to load at least one ID from database for {OnlyFileName}, first null value was from {IdValues.FirstOrDefault(x => x.Value is null).Key}.");
                }
            }
            else
            {
                //try to load the IDs anyway
                if (dbConn != null)
                {
                    LoadIDs();
                }
            }

            if (!_loadOnly && dbConn != null)
            {
                SetReceiptLog(); //update the receipt log if valid or not 

                if (IsValid) //still valid then create a File_Log (File Log should be invalid if the file already exists)
                {
                    InsertFileLog(); // This will determine if the file is a duplicate - we aren't creating a file_log for duplicates so only update pdi_File_Log after the next is_valid check
                    if (IsValid)
                    {
                        InsertProcessing();
                        if (IsValid)
                        {
                            // email team pending
                        }
                        else
                        {
                            // email client     
                        }
                        if (DataID.HasValue && DataID >= 0)
                            SetFileLog();
                    }
                    
                }

                // send email if invalid
                else if (OnlyFileName != null && OnlyFileName.Length > 0)
                    OnlyFileName = OnlyFileName; // Now that we have a FileID but an invalid file reprocess the file name so we can capture the errors - this might be the third time the errors are displayed but the first time we can actually keep them.    
            }
            else if (dbConn != null && !JobID.HasValue)
                JobID = GetJobID();

            if (_log != null)
                _log.WriteErrorsToDB();
        }

        /// <summary>
        /// If a transient or other error occurred and we are rerunning the import file don't treat it as a duplicate and fill in any missing data
        /// </summary>
        /// <param name="retryCount"></param>
        internal int Retry(int retryCount)
        {
            if (dbConn is null)
                return -1;

            SetBatchFileID(); // in case the file ID was created but the batch wasn't (if the file id is not created then the SetReceiptLog will create it and set the batch file ID.
            SetReceiptLog(); // will both create the File Receipt Log record and update it 
            
            if (IsValidFileName && FileID.HasValue)
            {
                //if (!DataID.HasValue && IsValid)
                    SetFileLog(); // will both create the File Log entry and update it
                
                if (!JobID.HasValue && IsValid)
                    InsertProcessing();
            }
            else if (FileID.HasValue && FileID > -1) // we have an invalid file name on a reprocess so we need to recreate the error messages by loading the filename again.
                OnlyFileName = OnlyFileName;

            return _fileID;       
        }

        /// <summary>
        /// Return the ID for the passed columnName (if it exist)
        /// </summary>
        /// <param name="columnName">The column name to find in the ID Dictionary</param>
        /// <returns></returns>
        public int? IDValues(string columnName)
        {
            if (IdValues.TryGetValue(columnName, out int? id))
                return id;

            return null;
        }

        /// <summary>
        /// Load the associated IDs from the database
        /// </summary>
        /// <returns>True if all of the IDs were loaded</returns>
        private bool LoadIDs()
        {
            if (dbConn is null || DataCustodian is null || ClientName is null || LOB is null || DocumentType is null || DataType is null)
                return false;

            string sql = "DECLARE @dataID INTEGER; DECLARE @fileID INTEGER; SELECT @dataID = Data_ID, @fileID = [File_ID] FROM [pdi_File_Log] WHERE Data_Custodian = @dataCustodian AND Publisher_Company = @client AND Line_of_Business = @LOB AND (Code IS Null OR Code = @Code) AND Data_Type = @dataType AND Document_Type = @documentType AND File_Creation_Date = @File_Creation_Date AND File_Timestamp = @File_Timestamp AND File_Version = @File_Version; SELECT CASE WHEN @dataID IS NULL THEN (Select DISTINCT MAX([File_ID]) FROM [pdi_File_Receipt_Log] WHERE [File_Name] = @fileName) ELSE @fileID END AS [File_ID], @dataID As [Data_ID],(SELECT [Job_ID] FROM [pdi_Processing_Queue_Log] WHERE [Data_ID] = @dataID) As [Job_ID],(Select DC.[Custodian_ID] FROM [pdi_Data_Custodian] DC INNER JOIN [pdi_Publisher_Client] PC ON PC.Custodian_ID = DC.Custodian_ID WHERE [Data_Custodian_Name] = @dataCustodian AND PC.Client_Code = @client) AS [Custodian_ID],(Select [Client_ID] FROM [pdi_Publisher_Client] WHERE Client_Code = @client) AS [Client_ID],(Select [Company_ID] FROM [pdi_Publisher_Client] WHERE Client_Code = @client) AS [Company_ID], (SELECT [LOB_ID] FROM [pdi_Line_of_Business] LB INNER JOIN [pdi_Publisher_Client] PC ON LB.[Client_ID] = PC.[Client_ID] WHERE [LOB_Code] = @lob AND PC.[Client_Code] = @client) AS [LOB_ID],(SELECT [Data_Type_ID] FROM [pdi_Data_Type] WHERE [Data_Type_Name] = @dataType) AS [Data_Type_ID],(SELECT [Document_Type_ID] FROM [pdi_Document_Type] WHERE [Document_Type] = @documentType) AS [Document_Type_ID], (SELECT MAX(Batch_ID) FROM [pdi_Client_Batch_Files] WHERE [File_Name] = @fileName AND Extracted = 1) AS Batch_ID;";

            if (FileID != null && FileID > 0)
                sql = "SELECT pFRL.[File_ID], pFL.[Data_ID], pDC.[Custodian_ID], pPC.[Client_ID], [Company_ID], [LOB_ID], [Data_Type_ID], pDocT.[Document_Type_ID], (SELECT MAX(Batch_ID) FROM [pdi_Client_Batch_Files] WHERE [File_Name] = @fileName AND Extracted = 1 AND [File_ID] IS NULL) AS Batch_ID FROM [pdi_File_Receipt_Log] pFRL LEFT OUTER JOIN [pdi_File_Log] pFL ON pFL.[File_ID] = pFRL.[File_ID] FULL OUTER JOIN [pdi_Publisher_Client] pPC ON pPC.Client_Code = @client FULL OUTER JOIN[pdi_Data_Custodian] pDC ON pDC.[Data_Custodian_Name] = @dataCustodian AND pPC.Custodian_ID = pDC.Custodian_ID FULL OUTER JOIN [pdi_Document_Type] pDocT ON pDocT.Document_Type = @documentType FULL OUTER JOIN [pdi_Data_Type] pDataT on pDataT.Data_Type_Name = @dataType FULL OUTER JOIN [pdi_Line_of_Business] pLoB ON pLoB.LOB_Code = @lob AND pLob.Client_ID = pPC.Client_ID WHERE pFRL.[File_ID] = @File_ID";


            DataTable dt = new DataTable("LoadIDs");
            dbConn.LoadDataTable(sql, new Dictionary<string, object>(11){
                { "@dataCustodian", DataCustodian },
                { "@client", ClientName },
                { "@lob", LOB },
                { "@documentType", DocumentType },
                { "@Code", Code },
                { "@dataType", DataType },
                { "@fileName", OnlyFileName },
                { "@File_ID", FileID },
                { "@File_Creation_Date", CreationDate },
                { "@File_Timestamp", CreationTime },
                { "@File_Version", Version }
            }, dt);

            if (dt.Rows.Count == 1)
            {
                foreach (string keyValue in IdValues.Keys.ToList())
                {
                    if (dt.Columns.Contains(keyValue) && dt.Rows[0][keyValue] != DBNull.Value)
                    {
                        if (int.TryParse(dt.Rows[0][keyValue].ToString(), out int parsed))
                            IdValues[keyValue] = parsed;
                    }
                    else IdValues[keyValue] = null;
                }
                if (dt.Columns.Contains("File_ID") && dt.Rows[0]["File_ID"] != DBNull.Value && int.TryParse(dt.Rows[0]["File_ID"].ToString(), out int parsedFileID))
                    FileID = parsedFileID;
                if (dt.Columns.Contains("Data_ID") && dt.Rows[0]["Data_ID"] != DBNull.Value && int.TryParse(dt.Rows[0]["Data_ID"].ToString(), out int parsedDataID))
                    DataID = parsedDataID;
                if (dt.Columns.Contains("Batch_ID") && dt.Rows[0]["Batch_ID"] != DBNull.Value && int.TryParse(dt.Rows[0]["Batch_ID"].ToString(), out int parsedBatchID))
                    BatchID = parsedBatchID;
            }
            return !IdValues.ContainsValue(null);
        }

        public bool SetBatchFileID()
        {
            if (BatchID >= 0 && FileID >= 0)
            {
                if (!dbConn.ExecuteNonQuery("UPDATE [pdi_Client_Batch_Files] SET [File_ID] = @fileID WHERE [File_Name] = @fileName AND [Batch_ID] = @batchID AND [File_ID] IS NULL;", out int rows, new Dictionary<string, object>(3) {
                        { "@fileName", OnlyFileName },
                        { "@fileID", FileID },
                        { "@batchID", BatchID }
                    }) || rows > 1)
                {
                    SetError($"Unable to update Client Batch Files for {OnlyFileName} with File_ID {FileID} and Batch_ID {BatchID} - Error: {(rows == 1 ? dbConn.LastError : "Updated rows greater than 1 : " + rows.ToString())}");
                }
                else
                    return true;
            }
            return false;
        }

        public int InsertReceiptLog()
        {
            if (dbConn != null && !_loadOnly) //_fileID < 0 && 
            {
                if (FileRunID is null || FileRunID.Length < 1)
                    FileRunID = Guid.NewGuid().ToString();
                var cmdResult = dbConn.ExecuteScalar("INSERT INTO [pdi_File_Receipt_Log] ([File_Name],[FileRunID]) OUTPUT Inserted.File_ID VALUES (@fileName, @fileRunID)", new Dictionary<string, object>(2) {
                    { "@fileName", OnlyFileName },
                    {"@fileRunID", FileRunID }
                });

                if (int.TryParse(cmdResult.ToString(), out int tempID))
                {
                    FileID = tempID;
                    SetBatchFileID();
                }
                else
                {
                    SetError($"Unable to insert new File Receipt Log record in database for {OnlyFileName}, insert returned {cmdResult}");
                    FileID = -1;
                } 
            }
            return _fileID;
        }

        private int SetReceiptLog()
        {
          
            if (dbConn is null)
                return -1;

            if (_fileID < 0)
                InsertReceiptLog();

            if (_fileID < 0)
            {
                SetError($"No existing Receipt Log found for {OnlyFileName}, submit as new (RetryCount=0)");
                return -1;
            }

            // for retry we should update the file receipt time stamp
            if (!dbConn.ExecuteNonQuery("UPDATE [pdi_File_Receipt_Log] SET IsValidFileName = @isValid, File_Receipt_Timestamp = GETUTCDATE() WHERE File_ID = @fileID;", out int rows, new Dictionary<string, object>(2) {
                { "@isValid", IsValid },
                { "@fileID", FileID }
            }))
                SetError($"Error setting Receipt Log for {OnlyFileName}, Error was:  {dbConn.LastError}");


            else if (rows != 1)
                SetError($"Unable to update File Receipt Log in database for {OnlyFileName}, tried to set 1 record but set {rows}");

            return rows;
        }

        private int SetFileLog()
        {
            if (dbConn is null)
                return -1;

            if (!DataID.HasValue && IsValid)
                InsertFileLog();

            int rows = 0;
            if (DataID.HasValue && !dbConn.ExecuteNonQuery("UPDATE [pdi_File_Log] SET IsValidDataFile = @isValid WHERE Data_ID = @dataID AND File_ID = @fileID", out rows, new Dictionary<string, object>(3) {
                { "@isValid", IsValid },
                { "@dataID", DataID },
                { "@fileID", FileID }
            }))
                SetError($"Error setting IsValidDataFile for {OnlyFileName}, Error was:  {dbConn.LastError}");
            else if (rows != 1)
                SetError($"Unable to update File Log in database for {OnlyFileName}, tried to set 1 record but set {rows}");

            return rows;
        }

        private void InsertFileLog()
        {
            if (dbConn is null)
                return;

            Dictionary<string, object> queryParams = new Dictionary<string, object>(9) {
                    { "@Data_Custodian", DataCustodian },
                    { "@Publisher_Company", ClientName },
                    { "@LOB", LOB },
                    { "@Data_Type", DataType },
                    { "@Document_Type", DocumentType },
                    { "@File_Creation_Date", CreationDate },
                    { "@File_Timestamp", CreationTime },
                    { "@File_Version", Version },
                    { "@Code", Code }
                };

            if (!DataID.HasValue) // Already loaded by LoadIDs?
            {
                DataID = (int?)dbConn.ExecuteScalar("SELECT Data_ID FROM [pdi_File_Log] WHERE Data_Custodian = @Data_Custodian AND Publisher_Company = @Publisher_Company AND Line_of_Business = @LOB AND Code = @Code AND Data_Type = @Data_Type AND Document_Type = @Document_Type AND File_Creation_Date = @File_Creation_Date AND File_Timestamp = @File_Timestamp AND File_Version = @File_Version;", queryParams);
            }
           
            if (DataID.HasValue) // Check again - it should be NULL or we have a duplicate
            {
                SetError($"File {OnlyFileName} is a duplicate and already exists in File Log with Data_ID {DataID}"); // setting the error will cause the validation flag to be set to false on the newly created pdi_File_Log
                if (_proc != null)
                    _proc.SetProcessingQueue(ProcessingStage.Duplicate);
            }
            else // Create the File Log record
            {
                queryParams.Add("@File_ID", FileID);
                DataID = (int?)dbConn.ExecuteScalar("INSERT Into pdi_File_Log (File_ID, Data_Custodian, Publisher_Company, Line_of_Business, Code, Data_Type, Document_Type, File_Creation_Date, File_Timestamp, File_Version) OUTPUT Inserted.Data_ID Values (@File_ID, @Data_Custodian, @Publisher_Company, @LOB, @Code, @Data_Type, @Document_Type, @File_Creation_Date, @File_Timestamp, @File_Version);", queryParams);

                if (!DataID.HasValue) // if we still don't have an ID then something went wrong
                {
                    SetError($"File {OnlyFileName} Could not be inserted into File Log:  {dbConn.LastError}"); // setting the error will cause the validation flag to be set to false on the newly created pdi_File_Log
                    if (_proc != null)
                        _proc.SetProcessingQueue(ProcessingStage.Error);
                }
            }               
        }

        private void InsertProcessing()
        {
            if (dbConn is null || !IsValid) // don't create a processing queue if the file is not valid
                return;
            if (_proc is null)
                Processing.InsertProcessing(this, dbConn, out string _);
            else
                _proc.InsertProcessing(this);

            if (JobID < 0)
                SetError($"Unable to insert new Processing Queue Log record in database for {OnlyFileName}, insert returned {JobID}");
        }

        public string GetFileName()
        {
            if (dbConn is null)
                return null;

            return (string)dbConn.ExecuteScalar("SELECT File_Name FROM pdi_File_Receipt_Log WHERE File_ID = @fileID", new Dictionary<string, object>(1) { { "@fileID", FileID  } });
        }

        private int? GetJobID()
        {
            if (dbConn is null)
                return null;

            if (DataID.HasValue && DataID > -1 && IsValid)
                return (int?)dbConn.ExecuteScalar("SELECT Job_ID FROM pdi_Processing_Queue_Log WHERE Data_ID = @dataID", new Dictionary<string, object>(1) { { "@dataID", DataID } });
            
            if (FileID.HasValue && FileID > -1 && IsValid)
                return (int?)dbConn.ExecuteScalar("SELECT Job_ID FROM pdi_Processing_Queue_Log PQL INNER JOIN pdi_File_Log FL ON PQL.Data_ID = FL.Data_ID WHERE File_ID = @fileID", new Dictionary<string, object>(1) { { "@fileID", FileID } });
            
            return null;
        }

        public void LoadParameters(int jobID = -1)
        {
            if (jobID > -1)
                _parameters = LoadParameters(jobID, dbConn);
            else if (JobID.HasValue)
                _parameters = LoadParameters((int)JobID, dbConn);

            //_parameters = LoadParameters(jobID > -1 ? jobID : (int)JobID, dbConn);
        }

        public static Dictionary<string, string> LoadParameters(int jobID, DBConnection dbInternal)
        {
            if (dbInternal is null)
                return null;

            DataTable dt = new DataTable("Parameters");
            if (dbInternal.LoadDataTable("SELECT * FROM [pdi_Processing_Queue_Log] PQL INNER JOIN [pdi_File_Log] FL ON PQL.Data_ID = FL.Data_ID INNER JOIN [pdi_File_Receipt_Log] FRL ON FL.File_ID = FRL.File_ID INNER JOIN [pdi_Publisher_Client] PC ON PQL.Client_ID = PC.Client_ID INNER JOIN [pdi_Data_Custodian] DC ON PC.Custodian_ID = DC.Custodian_ID INNER JOIN [pdi_Document_Type] DT ON PQL.Document_Type_ID = DT.Document_Type_ID LEFT OUTER JOIN (SELECT MAX(BRL.Batch_ID) AS Batch_ID, MAX(Batch_Created_Timestamp) AS Batch_Created_Timestamp, BF.File_ID FROM [pdi_Client_Batch_Receipt_Log] BRL INNER JOIN [pdi_Client_Batch_Files] BF ON BRL.Batch_ID = BF.Batch_ID GROUP BY BF.File_ID) Batch ON Batch.File_ID = FRL.File_ID WHERE PQL.Job_ID = @jobID", new Dictionary<string, object>(1) { { "@jobID", jobID } }, dt))
            {
                if (dt.Rows.Count == 1)
                    return dt.Rows[0].GetDataRowDictionaryLocal(); //Table.Columns.Cast<DataColumn>().GroupBy(p => p.ColumnName).ToDictionary(col => col.Key, col => dt.Rows[0][col.Key].ToString());
            }
           
            return null;
        }

        public Dictionary<string, string> LoadFileParameters()
        {
            if (dbConn is null)
                return null;

            DataTable dt = new DataTable("Parameters");
            if (dbConn.LoadDataTable("SELECT COALESCE(PQL.Job_STATUS, 'Failed') AS Job_Status,COALESCE(Batch.Batch_Created_Timestamp, Batch.File_Receipt_Timestamp, FRL.File_Receipt_Timestamp) AS Batch_Created_Timestamp, COALESCE(Job_Start, FRL.File_Receipt_Timestamp, Batch.File_Receipt_Timestamp) AS Job_Start, COALESCE(Import_End, Load_End, Transform_End, Extract_End, Job_Start, FRL.File_Receipt_Timestamp, Batch.File_Receipt_Timestamp) AS Import_End, COALESCE(Number_of_Records, 1) As Number_of_Records, Code, COALESCE(FRL.File_Name, Batch.File_Name) AS File_Name, IsValidFileName, IsValidDataFile, PUBClient.*, DocType.* FROM [pdi_File_Receipt_Log] FRL LEFT OUTER JOIN [pdi_File_Log] FL ON FRL.File_ID = FL.File_ID LEFT OUTER JOIN [pdi_Processing_Queue_Log] PQL ON PQL.Data_ID = FL.Data_ID LEFT OUTER JOIN (SELECT MAX(BRL.Batch_ID) AS Batch_ID, MAX(Batch_Created_Timestamp) AS Batch_Created_Timestamp, MAX(File_Receipt_Timestamp) AS File_Receipt_Timestamp, BF.File_Name, BF.File_ID FROM [pdi_Client_Batch_Receipt_Log] BRL INNER JOIN [pdi_Client_Batch_Files] BF ON BRL.Batch_ID = BF.Batch_ID GROUP BY BF.File_Name, BF.File_ID) Batch ON Batch.File_ID = FRL.File_ID CROSS JOIN (SELECT Client_Code, Company_Name, Notification_Email_Address, Data_Custodian_Name FROM [pdi_Publisher_Client] PC INNER JOIN [pdi_Data_Custodian] DC ON PC.Custodian_ID = DC.Custodian_ID WHERE PC.Client_ID = @clientID) AS PUBClient CROSS JOIN (SELECT * FROM [pdi_Document_Type] DT WHERE DT.Document_Type_ID = @docTypeID) AS DocType WHERE FRL.File_ID = @fileID", new Dictionary<string, object>(3) { 
                { "@fileID", FileID },
                { "@clientID", ClientID },
                { "@docTypeID", DocumentTypeID }
            }, dt))
            {
                if (dt.Rows.Count == 1)
                    return dt.Rows[0].GetDataRowDictionaryLocal(); //Table.Columns.Cast<DataColumn>().GroupBy(p => p.ColumnName).ToDictionary(col => col.Key, col => dt.Rows[0][col.Key].ToString());
            }
            Dictionary<string, string> tParams = new Dictionary<string, string>(3);
            tParams.Add("Job_Status", "Error");
            tParams.Add("File_Name", OnlyFileName );
            tParams.Add("Notification_Email_Address", "pdi_support@investorcom.com" );

            return null;
        }

        public int CountValidationErrors()
        {
            if (dbConn != null && FileID.HasValue && FileID > -1)
            {
                var cmdResult = dbConn.ExecuteScalar("SELECT COUNT(Validation_Message) AS MessageCount FROM [pdi_File_Validation_Log] WHERE File_ID  = @fileID", new Dictionary<string, object>(1) { { "@fileID", FileID } });
                if (int.TryParse(cmdResult.ToString(), out int tempID))
                    return tempID;
                else
                    Logger.AddError(_log, $"Unable to determine count for File ID {FileID}, select returned {dbConn.LastError ?? cmdResult}");
            }
            return -1;
        }

        public Stream GetValidationErrorsCSV(int fileID = -1)
        {
            if (dbConn is null)
                return null;

            DataTable dt = new DataTable("ValidationErrors");
            MemoryStream ms = new MemoryStream();
            if (dbConn.LoadDataTable("SELECT Validation_Message FROM [dbo].[pdi_File_Validation_Log] WHERE File_ID = @fileID", new Dictionary<string, object>(1) { { "@fileID", (fileID > -1 ? fileID : FileID) } }, dt))
            {
                using (var writer = new StreamWriter(ms, Encoding.UTF8, 1000, true))
                {
                    writer.Write(dt.ToCSV());
                    writer.Flush();
                }
                if (ms.CanSeek)
                    ms.Position = 0;              
            }
            return ms;
        }

        public int CountMissingFrench()
        {
            if (dbConn != null && JobID.HasValue && JobID > -1)
            {
                var cmdResult = dbConn.ExecuteScalar("SELECT COUNT(ML.[en-CA]) AS MissingCount FROM pdi_Client_Translation_Language_Missing_Log_Details MLG INNER JOIN pdi_Client_Translation_Language_Missing_Log ML ON MLG.Missing_ID = ML.Missing_ID INNER JOIN pdi_Line_of_Business LB on LB.LOB_ID = ML.LOB_ID LEFT OUTER JOIN pdi_Client_Translation_Language TL ON TL.Client_ID = ML.Client_ID AND TL.Document_Type_ID = ML.Document_Type_ID AND TL.LOB_ID = ML.LOB_ID AND TL.[en-CA] = ML.[en-CA] WHERE MLG.Job_ID = @jobID AND TL.[en-CA] IS NULL", new Dictionary<string, object>(1) { { "@jobID", JobID } });
                if (int.TryParse(cmdResult.ToString(), out int tempID))
                    return tempID;
                else
                    Logger.AddError(_log, $"Unable to determine missing French count for Job ID {JobID}, select returned {dbConn.LastError ?? cmdResult}");
            }
            return -1;
        }

        public Stream GetMissingFrenchCSV()
        {
            MemoryStream ms = new MemoryStream();
            if (dbConn != null && JobID.HasValue && JobID > -1)
            {
                DataTable dt = new DataTable("MissingFrench");
                if (dbConn.LoadDataTable("SELECT DISTINCT LB.LOB_Code as [Line of Business], ML.[en-CA] as [English], 'N/A' as [French] FROM pdi_Client_Translation_Language_Missing_Log_Details MLG INNER JOIN pdi_Client_Translation_Language_Missing_Log ML ON MLG.Missing_ID = ML.Missing_ID INNER JOIN pdi_Line_of_Business LB on LB.LOB_ID = ML.LOB_ID LEFT OUTER JOIN pdi_Client_Translation_Language TL ON TL.Client_ID = ML.Client_ID AND TL.Document_Type_ID = ML.Document_Type_ID AND TL.LOB_ID = ML.LOB_ID AND TL.[en-CA] = ML.[en-CA] WHERE MLG.Job_ID = @jobID AND TL.[en-CA] IS NULL ORDER BY ML.[en-CA]", new Dictionary<string, object>(1) { { "@jobID", JobID } }, dt))
                {
                    using (var writer = new StreamWriter(ms, Encoding.UTF8, 1000, true))
                    {
                        writer.Write(dt.ToCSV());
                        writer.Flush();
                    }
                    if (ms.CanSeek)
                        ms.Position = 0;
                }
                else
                    Logger.AddError(_log, $"Unable to generate missing French for Job ID {JobID}, select returned {dbConn.LastError}"); 
            }
            return ms;
        }

        /// <summary>
        /// If an error occurs set IsValid to false and set the passed error message
        /// </summary>
        /// <param name="errorMessage">The ErrorMessage to set</param>
        private void SetError(string errorMessage)
        {
            IsValid = false;

            if (_log is null && dbConn != null && FileID.HasValue && FileID > -1)
                _log = new Logger(dbConn, FileID, BatchID, FileRunID);

            if (_log != null && (!_log.FileID.HasValue || !_log.BatchID.HasValue || (_log.RunID is null || _log.RunID.Length < 0)))
                _log.UpdateParams(this);

            Logger.AddError(_log, errorMessage);
            ErrorMessage = errorMessage;
        }

        public string GetDefaultTemplateName()
        {
            return "TEMPLATE" + FILE_DELIMITER + DataType + FILE_DELIMITER + DocumentType + ".xlsx";
        }

        public string GetDefaultStaticTemplateName()
        {
            return "TEMPLATE" + FILE_DELIMITER + "STATIC" + FILE_DELIMITER + DocumentType + ".xlsx";
        }

        // for the current client and document type pull the data for a default static file
        //public DataSet GetStaticTemplateData()
        //{
        //    DataSet dtSet = new DataSet("Default Static");
        //    // we need to populate Field UPDATE with all the static fields
        //    DataTable dt = dtSet.Tables.Add("Field UPDATE");
        //    string sql = "SELECT Section, Sub-Section, Field Descripton, Category, Field Type, Client Code, Line of Business, Document Type, Field Name, 'N/A' As Load Field FROM 

        //}
    }
}
