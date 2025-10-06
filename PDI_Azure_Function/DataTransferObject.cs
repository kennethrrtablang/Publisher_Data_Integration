using System.Collections.Specialized;
using Newtonsoft.Json;
using Publisher_Data_Operations;
using Publisher_Data_Operations.Helper;

namespace PDI_Azure_Function
{
   
 
    public class DataTransferObject
    { 
        public string File_ID { get; set; }
        public string FileName { get; set; }
        public string FileRunID { get; set; }
        public bool? FileStatus { get; set; }
        public string Job_ID { get; set; }
        public string Batch_ID { get; set; }
        public ProcessStatusObject ProcessStatus {get; set; }
        public FileDetailsObject FileDetails { get; set; }
        public string ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public string NotificationEmailAddress { get; set; }

        public DataTransferObject()
        {
            File_ID = null;
            FileName = null;
            FileRunID = null;
            FileStatus = null;
            Job_ID = null;
            Batch_ID = null;
            ProcessStatus = null;
            FileDetails = null;
            ErrorMessage = null;
            RetryCount = 0;
            NotificationEmailAddress = null;
        }

        public DataTransferObject(string jsonString)
        {
            dynamic data = JsonConvert.DeserializeObject<DataTransferObject>(jsonString);

            File_ID = data?.File_ID;
            FileName = data?.FileName;
            FileStatus = data?.FileStatus;
            FileRunID = data?.FileRunID;
            Job_ID = data?.Job_ID;
            Batch_ID = data?.Batch_ID;
            ProcessStatus = data?.ProcessStatus;
            FileDetails = data?.FileDetails;
            ErrorMessage = data?.ErrorMessage;
            RetryCount = data?.RetryCount ?? 0;
            NotificationEmailAddress = data?.NotificationEmailAddress;
        }

        public DataTransferObject(string jsonString, NameValueCollection values)
        {
            dynamic data = JsonConvert.DeserializeObject<DataTransferObject>(jsonString);

            File_ID = values["File_ID"] ?? data?.File_ID;
            FileName = values["FileName"] ?? data?.FileName;
            FileStatus = values["FileStatus"] ?? data?.FileStatus;
            FileRunID = values["FileRunID"] ?? data?.FileRunID;
            Job_ID = values["Job_ID"] ?? data?.Job_ID;
            Batch_ID = values["Batch_ID"] ?? data?.Batch_ID;
            ProcessStatus = values["ProcessStatus"] ?? data?.ProcessStatus;
            FileDetails = values["FileDetails"] ?? data?.FileDetails;
            ErrorMessage = values["ErrorMessage"] ?? data?.ErrorMessage;
            RetryCount = values["RetryCount"] ?? data?.RetryCount ?? 0;
            NotificationEmailAddress = values["NotificationEmailAddress"] ?? data?.NotificationEmailAddress;
        }
        public DataTransferObject(string fileName, int batchID, int retryCount)
        {
            FileName = fileName;
            Batch_ID = batchID.ToString();
            RetryCount = retryCount;
        }

        public DataTransferObject(Orchestration orch)
        {
            if (orch != null)
            {
                File_ID = orch.GetFile != null ? orch.GetFile.FileID.ToString() : null;
                Batch_ID = orch.GetFile != null ? orch.GetFile.BatchID.ToString() : null;
                FileName = orch.GetFile != null ? orch.GetFile.OnlyFileName : null;
                FileStatus = orch.FileStatus;
                FileRunID = orch.FileRunID;
                ProcessStatus = orch.GetProcessStatus;
                FileDetails = orch.GetFileDetails;
                //DocumentType = orch.GetFile.DocumentType;
                Job_ID = orch.GetFile != null ? orch.GetFile.JobID.ToString() : null;
                //Company_ID = orch.GetFile.CompanyID.ToString();
                //Document_Type_ID = orch.GetFile.DocumentTypeID.ToString();
                ErrorMessage = orch.ErrorMessage;
                RetryCount = orch.RetryCount;
                NotificationEmailAddress = orch.NotificationEmailAddress;
            }
        }

        public string Base64Encode()
        {
            return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this)));
        }
    }
}
