using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Compression;
using Publisher_Data_Operations.Extensions;
using System.IO;
using System.Data;

namespace Publisher_Data_Operations.Helper
{
    public class PDIBatch : IDisposable
    {
        DBConnection _dbCon = null;
        ZipArchive _zip = null;
        private bool disposedValue;
        public string LastError { get; set; }
        public int BatchID { get; private set; }

        public int RetryCount { get; private set; }

        public const string RetryName = "rerun.me";

        public PDIBatch(object con, int batchID = -1)
        {
            if (con.GetType() == typeof(DBConnection))
                _dbCon = (DBConnection)con;
            else
                _dbCon = new DBConnection(con);

            BatchID = batchID;
            RetryCount = 0;
        }

        public PDIBatch(object con, string batchID)
        {
            if (con.GetType() == typeof(DBConnection))
                _dbCon = (DBConnection)con;
            else
                _dbCon = new DBConnection(con);
            if (batchID != null && batchID.Length > 0 && int.TryParse(batchID, out int batchIntID))
                BatchID = batchIntID;
            else
                BatchID = -1;

            RetryCount = 0;
        }

        public List<Tuple<int, string>> LoadBatch(PDIStream pdiStream, DateTimeOffset? createdOn)
        {
            if (pdiStream is null || _dbCon is null)
                return null;

            BatchID = RecordBatch(pdiStream.PdiFile, createdOn);

            if (pdiStream.PdiFile.Extension == ".zip" && BatchID >= 0)
            {     
                _zip = new ZipArchive(pdiStream.SourceStream, ZipArchiveMode.Read);
                
                return RecordEntries();
            }
            return null;
        }

        public Stream ExtractEntry(string fileName)
        {
            if (_zip != null)
            {
                ZipArchiveEntry entry = _zip.Entries.Where(z => z.Name == fileName).FirstOrDefault();

                if (entry != null && entry.FullName.ToLower() == RetryName)
                    RetryCount = 1;
                else if (entry != null && entry.FullName.ToLower().EndsWith("xlsx"))
                {
                    return entry.Open();
                    //using (var stream = entry.Open())
                    //{
                    //    MemoryStream memStream = new MemoryStream();
                    //    stream.CopyTo(memStream);
                    //    if (memStream.CanSeek && memStream.Position != 0)
                    //        memStream.Position = 0;
                    //    return memStream;
                    //}
                }
                else
                    LastError = $"Unrecognized file {fileName} in archive.";
            }
            return null;
        }

        public bool MarkEntryExtracted( string fileName)
        {
            if (_dbCon != null && BatchID >= 0 && fileName != null && fileName.Length > 0)
            {
                bool ret = _dbCon.ExecuteNonQuery("UPDATE [pdi_Client_Batch_Files] SET [Extracted] = 1 WHERE Batch_ID = @batchID AND File_Name = @fileName;", out int rows, new Dictionary<string, object>(2) {
                    { "@batchID", BatchID },
                    { "@fileName", fileName}
                });
                if (ret && rows == 1)
                    return true;
                else
                    LastError = $"Unable to mark batch entry extracted for ID {BatchID} and file name {fileName} - error: {_dbCon.LastError}";
            }
            return false;      
        }

        private List<Tuple<int, string>> RecordEntries()
        {
            DataTable dt = new DataTable("ClientBatchFiles");
            if (_dbCon != null && _zip != null && BatchID > -1)
            {
                if (_dbCon.LoadDataTable("SELECT [Batch_ID], [File_Name], Extracted, File_ID, Finished FROM [pdi_Client_Batch_Files] WHERE Batch_ID = -1", null, dt))
                {
                    foreach (ZipArchiveEntry entry in _zip.Entries)
                    {

                        if (entry != null && entry.Name.ToLower() == RetryName)
                            RetryCount = 1;
                        else if (entry != null && entry.Name.ToLower().EndsWith("xlsx"))
                            dt.Rows.Add(BatchID, entry.Name, 0, null, 0);
                    } 
                    if (dt.Rows.Count > 0)
                        if (!_dbCon.BulkCopy("pdi_Client_Batch_Files", dt))
                            dt.Clear();
                }
                else
                    LastError = $"Unable to insert batch entries for ID {BatchID} - error: {_dbCon.LastError}";
            }
            List<Tuple<int, string>> entryList = new List<Tuple<int, string>>(dt.Rows.Count);
            foreach (DataRow dr in dt.Rows)
                entryList.Add(new Tuple<int, string>(dr.GetExactColumnIntValue("Batch_ID"), dr.GetExactColumnStringValue("File_Name")));

            return entryList;
        }

        public bool RecordSingle(string fileName)
        {
            if (_dbCon != null && BatchID > -1)
            {
                if (!_dbCon.ExecuteNonQuery("INSERT INTO [pdi_Client_Batch_Files] ([Batch_ID], [File_Name]) VALUES (@batchID, @fileName)", out int rows, new Dictionary<string, object>(2) {
                    { "@batchID", BatchID },
                    { "@fileName", fileName }
                }) || rows != 1)
                {
                    LastError = $"Unable to insert batch entry for {fileName} in Batch_ID {BatchID} - error: {_dbCon.LastError}";
                    return false;
                }
                else
                    return true;
            }
            return false;
        }

        public int RecordBatch(string fileName, DateTimeOffset? createdOn)
        {
            if (_dbCon != null)
            {
                BatchID = -1;
                var cmdResult = _dbCon.ExecuteScalar("INSERT INTO [pdi_Client_Batch_Receipt_Log] ([File_Name], [Batch_Created_Timestamp]) OUTPUT Inserted.Batch_ID VALUES (@fileName, @createdOn)", new Dictionary<string, object>(2) {
                    { "@fileName", fileName },
                    { "@createdOn", ( createdOn.HasValue ? ((DateTimeOffset)createdOn).UtcDateTime : createdOn) }
                });

                if (int.TryParse(cmdResult.ToString(), out int tempID))
                    BatchID = tempID;
                else
                    LastError = $"Unable to insert new File Receipt Log record in database for {fileName}, insert returned {_dbCon.LastError ?? cmdResult}";
            }
           
            return BatchID;
        }

        internal string GetFileName()
        {
            if (_dbCon != null && BatchID > -1)
            {
                var cmdResult = _dbCon.ExecuteScalar("SELECT File_Name FROM [pdi_Client_Batch_Receipt_Log] WHERE Batch_ID = @batchID", new Dictionary<string, object>(1) { { "@batchID", BatchID } });
                return cmdResult.ToString();
            }
            return string.Empty;
        }

        internal Dictionary<string, string> GetMessageParameters()
        {
            if (_dbCon != null && BatchID > -1)
            {
                DataTable dt = new DataTable("Parameters");
                if (_dbCon.LoadDataTable("SELECT Min(BRL.Batch_Created_Timestamp) AS Batch_Created_Timestamp,  Min(PQL.Job_Start) AS Job_Start, MAX(COALESCE(Import_End, Load_End, Transform_End, Extract_End, Job_Start)) As Import_End, BRL.File_Name, MAX(PC.Company_Name) AS Company_Name, MAX(PC.Notification_Email_Address) AS Notification_Email_Address, MAX(DC.Data_Custodian_Name) AS Custodian_Name, COUNT(BF.File_Name) As Number_of_Files FROM pdi_Client_Batch_Receipt_Log BRL INNER JOIN pdi_Client_Batch_Files BF on BF.Batch_ID = BRL.Batch_ID LEFT OUTER JOIN [pdi_File_Log] FL ON FL.File_ID = BF.File_ID LEFT OUTER JOIN [pdi_Processing_Queue_Log] PQL ON PQL.Data_ID = FL.Data_ID LEFT OUTER JOIN [pdi_Publisher_Client] PC ON PQL.Client_ID = PC.Client_ID LEFT OUTER JOIN [pdi_Data_Custodian] DC ON PC.Custodian_ID = DC.Custodian_ID WHERE BRL.Batch_ID = @batchID GROUP BY BRL.File_Name", new Dictionary<string, object>(1) { { "@batchID", BatchID } }, dt))
                {
                    if (dt.Rows.Count > 0)
                        return dt.Rows[0].GetDataRowDictionaryLocal(); //Table.Columns.Cast<DataColumn>().GroupBy(p => p.ColumnName).ToDictionary(col => col.Key, col => dt.Rows[0][col.Key].ToString());
                }
            }
            return null;
        }

        internal string HTMLTableResults()
        {
            if (_dbCon != null && BatchID > -1)
            {
                DataTable dt = new DataTable("Results");
                if (_dbCon.LoadDataTable("SELECT COALESCE(FL.Code, BF.File_Name) AS ID, FL.Document_Type As Type, PQL.Job_Status AS Status FROM [pdi_Client_Batch_Files] BF LEFT OUTER JOIN [pdi_File_Log] FL on BF.File_ID = FL.File_ID LEFT OUTER JOIN[pdi_Processing_Queue_Log] PQL on FL.Data_ID = PQL.Data_ID WHERE BF.Batch_ID = @batchID ORDER BY FL.Document_Type, COALESCE(FL.Code, BF.File_Name)", new Dictionary<string, object>(1) { { "@batchID", BatchID } }, dt))
                    return dt.DataTabletoHTML(true, true);
                else
                    LastError = $"Unable to load HTML Results Table for Batch ID {BatchID} with error: {_dbCon.LastError}";
            }
            return LastError;
        }

        internal int ValidationMessageCount()
        {
            if (_dbCon != null && BatchID > -1)
            {
                var cmdResult = _dbCon.ExecuteScalar("SELECT COUNT(Validation_Message) AS MessageCount FROM [pdi_File_Validation_Log] WHERE File_ID IN (SELECT File_ID FROM [pdi_Client_Batch_Files] WHERE Batch_ID = @batchID)", new Dictionary<string, object>(1) { { "@batchID", BatchID } });
                if (int.TryParse(cmdResult.ToString(), out int tempID))
                    return tempID;
                else
                    LastError = $"Unable to determine count for Batch ID {BatchID}, select returned {_dbCon.LastError ?? cmdResult}";
            }
            return -1;
        }

        internal Stream GetValidationErrorsCSV()
        {
            if (_dbCon != null && BatchID > -1)
            {
                DataTable dt = new DataTable("ValidationErrors");
                MemoryStream ms = new MemoryStream();
                if (_dbCon.LoadDataTable("SELECT COALESCE(FL.Code, BF.File_Name) As ID, FL.Document_Type As Type, Validation_Message FROM [pdi_Client_Batch_Files] BF LEFT OUTER JOIN [pdi_File_Log] FL ON BF.File_ID = FL.File_ID INNER JOIN [pdi_File_Validation_Log] VL ON BF.File_ID = VL.File_ID WHERE BF.Batch_ID = @batchID ORDER BY Document_Type, COALESCE(FL.Code, BF.File_Name)", new Dictionary<string, object>(1) { { "@batchID", BatchID } }, dt))
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
            return null;
            
        }

        public int CountMissingFrench()
        {
            if (_dbCon != null && BatchID > -1)
            {
                var cmdResult = _dbCon.ExecuteScalar("SELECT COUNT(ML.[en-CA]) AS MissingCount FROM pdi_Client_Translation_Language_Missing_Log_Details MLG INNER JOIN pdi_Client_Translation_Language_Missing_Log ML ON MLG.Missing_ID = ML.Missing_ID INNER JOIN pdi_Line_of_Business LB on LB.LOB_ID = ML.LOB_ID LEFT OUTER JOIN pdi_Client_Translation_Language TL ON TL.Client_ID = ML.Client_ID AND TL.Document_Type_ID = ML.Document_Type_ID AND TL.LOB_ID = ML.LOB_ID AND TL.[en-CA] = ML.[en-CA] WHERE MLG.Job_ID IN (SELECT Job_ID FROM pdi_Processing_Queue_Log PQL INNER JOIN pdi_File_Log FL on PQL.Data_ID = FL.Data_ID INNER JOIN pdi_Client_Batch_Files CBF ON FL.File_ID = CBF.File_ID WHERE Batch_ID = @batchID) AND TL.[en-CA] IS NULL", new Dictionary<string, object>(1) { { "@batchID", BatchID } });
                if (int.TryParse(cmdResult.ToString(), out int tempID))
                    return tempID;
                else
                    LastError = $"Unable to determine missing French count for Job ID {BatchID}, select returned {_dbCon.LastError ?? cmdResult}";
            }
            return -1;
        }

        public Stream GetMissingFrenchCSV()
        {
            MemoryStream ms = new MemoryStream();
            if (_dbCon != null && BatchID > -1)
            {
                DataTable dt = new DataTable("MissingFrench");
                if (_dbCon.LoadDataTable("SELECT DISTINCT LB.LOB_Code as [Line of Business], ML.[en-CA] as [English], 'N/A' as [French] FROM pdi_Client_Translation_Language_Missing_Log_Details MLG INNER JOIN pdi_Client_Translation_Language_Missing_Log ML ON MLG.Missing_ID = ML.Missing_ID INNER JOIN pdi_Line_of_Business LB on LB.LOB_ID = ML.LOB_ID LEFT OUTER JOIN pdi_Client_Translation_Language TL ON TL.Client_ID = ML.Client_ID AND TL.Document_Type_ID = ML.Document_Type_ID AND TL.LOB_ID = ML.LOB_ID AND TL.[en-CA] = ML.[en-CA] WHERE MLG.Job_ID IN (SELECT Job_ID FROM pdi_Processing_Queue_Log PQL INNER JOIN pdi_File_Log FL on PQL.Data_ID = FL.Data_ID INNER JOIN pdi_Client_Batch_Files CBF ON FL.File_ID = CBF.File_ID WHERE Batch_ID = @batchID) AND TL.[en-CA] IS NULL ORDER BY ML.[en-CA]", new Dictionary<string, object>(1) { { "@batchID", BatchID } }, dt))
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
                    LastError = $"Unable to generate missing French for Job ID {BatchID}, select returned {_dbCon.LastError}";
            }
            return ms;
        }



        private int RecordBatch(PDIFile pdiFile, DateTimeOffset? createdOn)
        {
            return RecordBatch(pdiFile.OnlyFileName, createdOn);
        }

        public int Count()
        {
            if (_dbCon != null && BatchID > -1)
            {
                var cmdResult = _dbCon.ExecuteScalar("SELECT COUNT(File_Name) AS FileCount FROM [pdi_Client_Batch_Files] WHERE Batch_ID = @batchID", new Dictionary<string, object>(1) { { "@batchID", BatchID } });
                if (int.TryParse(cmdResult.ToString(), out int tempID))
                    return tempID;
                else
                    LastError = $"Unable to determine count for Batch ID {BatchID}, select returned {_dbCon.LastError ?? cmdResult}";
            }
            return -1;
        }

        public bool SetComplete(string fileName)
        {
            if (_dbCon != null && BatchID > -1)
            {
                if (!_dbCon.ExecuteNonQuery("UPDATE [pdi_Client_Batch_Files] SET Finished = 1 WHERE Batch_ID = @batchID AND File_Name = @fileName", out int rows, new Dictionary<string, object>(2) {
                    { "@batchID", BatchID },
                    { "@fileName", fileName }
                }) || rows != 1)
                {
                    LastError = $"Unable to insert batch entry for {fileName} in Batch_ID {BatchID} - error: {_dbCon.LastError}";
                    return false;
                }
                else
                    return true;
            }
            return false;
        }

        public bool IsComplete()
        {
            if (_dbCon != null && BatchID > -1)
            {
                var cmdResult = _dbCon.ExecuteScalar("SELECT CASE WHEN EXISTS (SELECT 0 FROM [pdi_Client_Batch_Files] WHERE Finished = 0 AND Batch_ID = @batchID) THEN 0 ELSE 1 END AS 'BatchFinished'", new Dictionary<string, object>(1) { { "@batchID", BatchID } });
                if (int.TryParse(cmdResult.ToString(), out int tempID))
                    return tempID == 1;
                else
                    LastError = $"Unable to determine complete for Batch ID {BatchID}, select returned {_dbCon.LastError ?? cmdResult}";
            }
            return false;
            //DataTable dt = new DataTable("IncompleteRecords");
            //if (_dbCon.LoadDataTable("SELECT DISTINCT Job_Status FROM [pdi_Client_Batch_Receipt_Log]  BRL INNER JOIN [pdi_Client_Batch_Files] BF on BF.Batch_ID = BRL.Batch_ID LEFT OUTER JOIN [pdi_File_Receipt_Log] FRL ON BF.File_ID = FRL.File_ID LEFT OUTER JOIN [pdi_File_Log] FL on FL.File_ID = FRL.File_ID LEFT OUTER JOIN [pdi_Processing_Queue_Log] PQL ON FL.Data_ID = PQL.Data_ID WHERE BRL.Batch_ID = @batchID AND BF.Extracted=1 AND IsValidFileName = 1 AND IsValidDataFile = 1 AND Job_Status NOT IN (@complete1, @complete2, @complete3, @complete4) UNION SELECT 'Incomplete' AS Job_Status FROM [pdi_Client_Batch_Files] WHERE Batch_ID = @batchID AND File_ID IS NULL", new Dictionary<string, object>(5) {
            //    { "@batchID", BatchID },
            //    { "@complete1", Processing.ProcessingText[ProcessingStage.Complete] },
            //    { "@complete2", Processing.ProcessingText[ProcessingStage.Duplicate] },
            //    { "@complete3", Processing.ProcessingText[ProcessingStage.Error] },
            //    { "@complete4", Processing.ProcessingText[ProcessingStage.Validation] },
            //}, dt))
            //{
            //        if (dt.Rows.Count == 0)
            //            return true;
            //    }
            //    else
            //        LastError = $"Unable to check is complete for batch id {BatchID} error was : {_dbCon.LastError}";
            //}
            //return false;
        }

        public DataTable StatusTable()
        {
            DataTable dtStatus = new DataTable("Status");
            if (_dbCon != null && BatchID > -1)
            {
                if (!_dbCon.LoadDataTable("SELECT BF.File_Name as ID, COALESCE(PQL.Job_Status, CASE WHEN IsValidDataFile = 0 THEN 'Validation Failed' WHEN IsValidFileName = 0 THEN 'Invalid FileName' ELSE 'Unknown' END) As Status FROM [pdi_Client_Batch_Files] BF LEFT OUTER JOIN [pdi_File_Receipt_Log] FRL ON FRL.File_ID = BF.File_ID LEFT OUTER JOIN [pdi_File_Log] FL ON FL.File_ID = BF.File_ID LEFT OUTER JOIN [pdi_Processing_Queue_Log] PQL ON FL.Data_ID = PQL.Data_ID WHERE BF.Batch_ID = @batchID", new Dictionary<string, object>(1) { { "@batchID", BatchID } }, dtStatus))
                    LastError = $"Unable to load batch status for Batch ID {BatchID} error was : {_dbCon.LastError}";
            }
            return dtStatus;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    if (_zip != null)
                        _zip.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Batch()
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
}
