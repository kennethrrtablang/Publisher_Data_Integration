using System;
using System.Data;
using Publisher_Data_Operations.Extensions;
using Microsoft.Extensions.Logging;

namespace Publisher_Data_Operations.Helper
{
    public class Logger :IDisposable
    {

        private DataTable _errLog = null;
        private DBConnection _dbCon = null;

        private bool disposedValue;

       
        public int? FileID { get; set; }
        public int? BatchID { get; set; }
        public string RunID { get; set; }


        public ILogger logger { get; set; }

        public Logger(object con, int? fileID = null, int? batchID = null, string runID = null)
        {
            if (con != null)
            {
                if (con.GetType() == typeof(DBConnection))
                    _dbCon = (DBConnection)con;
                else
                    _dbCon = new DBConnection(con);
            }
            else
                throw new ArgumentNullException("con");

            FileID = fileID;
            BatchID = batchID;
            RunID = runID;
            InitializeErrorLogTable();

        }

        public Logger(object con, PDIFile pdiFile)
        {
            if (con != null)
            {
                if (con.GetType() == typeof(DBConnection))
                    _dbCon = (DBConnection)con;
                else
                    _dbCon = new DBConnection(con);
            }
            //else
                //throw new ArgumentNullException("con");

            UpdateParams(pdiFile);

            InitializeErrorLogTable();
        }

        /// <summary>
        /// Initialize the columns in the ErrorLog DataTable
        /// </summary>
        private void InitializeErrorLogTable()
        {
            _errLog = new DataTable("ErrorLog");
            _errLog.Columns.Add("File_ID", typeof(int));
            _errLog.Columns.Add("Batch_ID", typeof(int));
            _errLog.Columns.Add("Run_ID", typeof(Guid));
            _errLog.Columns.Add("Validation_Message", typeof(string));
        }

        public void UpdateParams(PDIFile pdiFile)
        {
            if (pdiFile != null)
            {
                FileID = pdiFile.FileID;
                BatchID = pdiFile.BatchID;
                RunID = pdiFile.FileRunID;
            }
            else
                throw new ArgumentNullException("pdiFile");
        }

        public void AddError(string errorMessage)
        {
            if (errorMessage is null || errorMessage.Trim().Length == 0)
                throw new ArgumentNullException("errorMessage");

            if (FileID.HasValue || BatchID.HasValue || (RunID != null && RunID.Length > 0))
                _errLog.Rows.Add(FileID, BatchID, RunID, errorMessage);
            
            if (logger != null)
                logger.LogWarning(errorMessage);
            else
                try
                {
                    Console.WriteLine(((FileID ?? BatchID).ToString() ?? RunID) + " " + errorMessage);
                }
                catch { }
        }

        /// <summary>
        /// The static version is used to check if the log is null before adding the error - output to console if null
        /// </summary>
        /// <param name="log"></param>
        /// <param name="errorMessage"></param>
        public static void AddError(Logger log, string errorMessage)
        {
            if (log != null)
                log.AddError(errorMessage);
            else
            {
                try
                {
                    if (Environment.UserInteractive)
                        Console.WriteLine(errorMessage);
                    else
                        System.Diagnostics.Trace.WriteLine(errorMessage); 
                }
                catch { }  
            }
        }

        public void AzureError(string errorMessage)
        {
            if (logger != null)
                logger.LogError(errorMessage);
        }

        public static void AzureError(Logger log, string errorMessage)
        {
            if (log != null)
                log.AzureError(errorMessage);
            if (Environment.UserInteractive)
                Console.WriteLine(errorMessage);

        }

        public void AzureWarning(string warningMessage)
        {
            if (logger != null)
                logger.LogWarning(warningMessage);
        }

        public static void AzureWarning(Logger log, string warningMessage)
        {
            if (log != null)
                log.AzureWarning(warningMessage);
            if (Environment.UserInteractive)
                Console.WriteLine(warningMessage);
                
        }

        /// <summary>
        /// Bulk copy all accumulated error in ErrorLog DataTable to the database - for files with a large number of errors this reduced the error recording time by several orders of magnitude
        /// </summary>
        /// <returns>True if write successful (or nothing to write) false on error</returns>
        public bool WriteErrorsToDB()
        {
            if (_errLog.Rows.Count > 0 && _dbCon != null && _dbCon.GetServer != null && _dbCon.GetServer.Length > 0) // for unit tests make sure the bulkcopy doesn't try when the SQL server is ""
            {

                if (!_dbCon.BulkCopy("dbo.pdi_File_Validation_Log", _errLog))
                {
                    AzureError($"Error Log Write Failed for File_ID: {FileID} - Error: {_dbCon.LastError}");
                    return false;
                }
                _errLog.Clear();
            }
            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    WriteErrorsToDB(); // log any remaining errors
                    _errLog.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _dbCon = null;
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~Logger()
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
