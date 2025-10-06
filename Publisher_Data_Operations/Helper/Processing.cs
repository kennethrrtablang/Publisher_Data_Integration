using System;
using System.Collections.Generic;
using Publisher_Data_Operations.Extensions;

namespace Publisher_Data_Operations.Helper
{
    public class ProcessStatusObject
    {
        public string Job_Status { get; set; }
        public DateTime Job_Start { get; set; }
        //public DateTime Validation_Start { get; set; }
        public DateTime Validation_End { get; set; }
        //public DateTime Extract_Start { get; set; }
        public DateTime Extract_End { get; set; }
        //public DateTime Transform_Start { get; set; }
        public DateTime Transform_End { get; set; }
        //public DateTime Load_Start { get; set; }
        public DateTime Load_End { get; set; }
        //public DateTime Import_Start { get; set; }
        public DateTime Import_End { get; set; }
  


        public ProcessStatusObject()
        {

        }

        public ProcessStatusObject(ProcessingStage ps)
        {
            SetStatus(ps);
        }

        public bool SetStatus(ProcessingStage ps)
        {
            Job_Status = Processing.ProcessingText[ps];
            var curProp = this.GetType().GetProperty(Enum.GetName(typeof(ProcessingStage), ps));
            if (curProp != null)
                curProp.SetValue(this, DateTime.Now);
            else
                return false;

            return true;
        }
    }

    public enum ProcessingStage
    {
        Pending,
        Job_Start,
 //       Validation_Start,
        Validation_End,
 //       Extract_Start,
        Extract_End,
 //       Transform_Start,
        Transform_End,
 //       Load_Start,
        Load_End,
        Import_Ready,
 //       Import_Start,
        Import_End,
        Error,
        Validation,
        Duplicate,
        Complete
    }

    public class Processing
    {
        internal DBConnection dbCon = null;
        internal int jobID = -1;
        public string ErrorMessage { get; private set; } = null;

        public ProcessStatusObject ProcStatus = null;

        public static Dictionary<ProcessingStage, string> ProcessingText = new Dictionary<ProcessingStage, string>()
        {
            { ProcessingStage.Pending, "Pending" },
            { ProcessingStage.Job_Start, "Start ETL Process" },
//            { ProcessingStage.Validation_Start, "Validation in Progress" },
            { ProcessingStage.Validation_End, "Validation Complete" },
            { ProcessingStage.Validation, "Validation Failed" },
//            { ProcessingStage.Extract_Start, "Extract in Progress" },
            { ProcessingStage.Extract_End, "Extract Complete" },
//            { ProcessingStage.Transform_Start, "Transform in Progress" },
            { ProcessingStage.Transform_End, "Transform Complete" },
//            { ProcessingStage.Load_Start, "Import Prep in Progress" },
            { ProcessingStage.Load_End, "Import Prep Complete" },
            { ProcessingStage.Import_Ready, "Ready to Import" },
//            { ProcessingStage.Import_Start, "Load in Progress" },
            { ProcessingStage.Import_End, "Load Complete" },
            { ProcessingStage.Error, "Error Occurred" },
            { ProcessingStage.Duplicate, "Duplicate Record" },
            { ProcessingStage.Complete, "Complete" }
        };

        public Processing(int job_ID, object connectionObject)
        {
            if (connectionObject.GetType() == typeof(DBConnection))
                dbCon = (DBConnection)connectionObject;
            else
                dbCon = new DBConnection(connectionObject);

            jobID = job_ID;
            ProcStatus = new ProcessStatusObject(ProcessingStage.Pending);
            SetProcessingSource();
        }

        public int SetProcessingQueue(ProcessingStage current)
        {
            if (ProcessingText.ContainsKey(current) && jobID >= 0)
            {
                string sql = $"UPDATE [pdi_Processing_Queue_Log] SET Job_Status = @jobStatus, {Enum.GetName(typeof(ProcessingStage), current)} = GETUTCDATE() WHERE Job_ID = @jobID";
                //if (current == ProcessingStage.Pending || current == ProcessingStage.Import_Ready || current == ProcessingStage.Error || current == ProcessingStage.Validation || current == ProcessingStage.Duplicate)
                if (!ProcStatus.SetStatus(current))
                    sql = $"UPDATE [pdi_Processing_Queue_Log] SET Job_Status = @jobStatus WHERE Job_ID = @jobID";

                if (dbCon.ExecuteNonQuery(sql, out int rows, new Dictionary<string, object>(2) {
                    { "@jobStatus", ProcStatus.Job_Status },
                    { "@jobID", jobID }
                }))
                {
                    if (rows != 1)
                        ErrorMessage = $"Unable to updateFile Receipt Log in database for Job_ID {jobID}, tried to set 1 record but set {rows}";
                    return rows;
                }
            }
            return -1;
        }

        public int SetProcessingSource()
        {

            if (jobID >= 0)
            {
                string sql = $"UPDATE [pdi_Processing_Queue_Log] SET Process_Source = @source WHERE Job_ID = @jobID";

                if (dbCon.ExecuteNonQuery(sql, out int rows, new Dictionary<string, object>(2) { { "@source", GetSourceString() }, { "@jobID", jobID } }))
                {
                    if (rows != 1)
                        ErrorMessage = $"Unable to updateFile Receipt Log in database for Job_ID {jobID}, tried to set 1 record but set {rows}";
                }
            }
            return -1;
        }
        public int InsertProcessing(PDIFile fileName)
        {
            jobID = InsertProcessing(fileName, dbCon, out string errorMsg);
            ErrorMessage = errorMsg;
            return jobID;
        }

        public static int InsertProcessing(PDIFile fileName, DBConnection localCon, out string ErrorMessage)
        {
            var cmdResult = localCon.ExecuteScalar("INSERT INTO [pdi_Processing_Queue_Log] ([Data_ID],[Client_ID],[LOB_ID],[Data_Type_ID],[Document_Type_ID],[Job_Status]) OUTPUT Inserted.Job_ID VALUES (@dataID, @clientID, @lobID, @dataTypeID, @docTypeID, @jobStatus)", new Dictionary<string, object>(6)
            {
                { "@dataID", fileName.DataID },
                { "@clientID", fileName.ClientID },
                { "@lobID", fileName.LOBID },
                { "@dataTypeID", fileName.DataTypeID },
                { "@docTypeID", fileName.DocumentTypeID },
                { "@jobStatus", ProcessingText[ProcessingStage.Pending] }
            });
            if (int.TryParse(cmdResult.ToString(), out int tempID))
                fileName.JobID = tempID;
            else
            {
                ErrorMessage = $"Unable to insert new Processing Queue Log record in database for {fileName.OnlyFileName}, insert returned {cmdResult}";
                fileName.JobID = -1;
            }
            ErrorMessage = null;
            return (int)fileName.JobID;   
        }

        public static string GetSourceString()
        {
            return $"Machine: {Environment.MachineName}\nUsername: {Environment.UserName}\nOS Version: {Environment.OSVersion}\nProcessor Count: {Environment.ProcessorCount}\nCLR Version: {Environment.Version}";
        }

        public static void UpdateFilingReferenceID(DBConnection dbCon, int jobID, string filingReferenceID, out string ErrorMessage)
        {
            ErrorMessage = string.Empty;
            if (dbCon is null)
                throw new ArgumentNullException("dbCon");

            if (!dbCon.ExecuteNonQuery("UPDATE [pdi_Processing_Queue_Log] SET [FilingReferenceID] = @filingReferenceID WHERE Job_ID = @jobID;", out int rows, new Dictionary<string, object>(2) { 
                { "@jobID", jobID }, 
                { "@filingReferenceID", filingReferenceID } 
            }) || rows != 1)
                ErrorMessage = $"Failed to update Filing Reference ID in  Processing Queue Log for Job_ID {jobID} - Error: {dbCon.LastError}";
        }
        
    }
}
