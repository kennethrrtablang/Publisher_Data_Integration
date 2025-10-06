using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Publisher_Data_Operations;
using Publisher_Data_Operations.Helper;
using PDI_Azure_Function.Extensions;

namespace PDI_Azure_Function
{
    public static class QueueProcess
    {
        [FunctionName("Queue_Process")]
        public static void Run([QueueTrigger("%PDI_QueueName%", Connection = "sapdi")] string myQueueItem, ILogger log, ExecutionContext exCtx)
        {
            DataTransferObject dto = new DataTransferObject(myQueueItem);
            log.LogInformation($"PDI Queue trigger function started processing: {dto.FileName}");

            string saPDI = Environment.GetEnvironmentVariable("sapdi");
            string pdiCon = Environment.GetEnvironmentVariable("PDI_ConnectionString");
            //string pdiIncomingContainer = Environment.GetEnvironmentVariable("IncomingContainer");
            string pdiMainContainer = Environment.GetEnvironmentVariable("MainContainer");
            string pubQueueName = Environment.GetEnvironmentVariable("PUB_QueueName");

            BlobServiceClient blobServiceClient = new BlobServiceClient(saPDI);

            PDIStream curStream = new PDIStream(blobServiceClient.Open($"processing/{dto.FileName}", pdiMainContainer), dto.FileName, pdiCon); //load the stream before moving it or it the stream contents won't be available
            curStream.PdiFile.FileRunID = exCtx.InvocationId.ToString();

            bool messageSent = false;
            string localError = string.Empty;
            Orchestration orch = new Orchestration(pdiCon, Environment.GetEnvironmentVariable("PUB_ConnectionString"));
            try
            {
                
                PDIStream templateStream = null;
                
                if (templateStream is null || templateStream.PdiFile.OnlyFileName != curStream.PdiFile.GetDefaultTemplateName())
                {
                    BlobClient templateBlob = blobServiceClient.GetBlobContainerClient(pdiMainContainer).GetBlobClient($"templates/{curStream.PdiFile.GetDefaultTemplateName()}");
                    if (!templateBlob.Exists())
                        log.LogCritical("Blob Trigger: Could not locate template file: {templateName}", templateBlob.Name);
                    else
                        templateStream = new PDIStream(templateBlob.Open(), templateBlob.Name);
                }

                bool result = orch.ProcessFile(curStream, templateStream, dto.RetryCount, log);
                log.LogInformation("PDI Queue Trigger: Finished Processing {name} result: {result} Error?: {error}", curStream.PdiFile.OnlyFileName, result, orch.ErrorMessage);
                //string destDir = "rejected";
                if (result && orch.FileStatus)
                {
                    //destDir = "completed";
                    if (blobServiceClient.MoveTo(pdiMainContainer, $"processing/{dto.FileName}", pdiMainContainer, $"importing/{dto.FileName}", log))
                    {
                        if (pubQueueName != null && pubQueueName.Length > 0)
                        {
                            QueueClient queueClient = new QueueClient(saPDI, pubQueueName);
                            if (!queueClient.AddMessage(new DataTransferObject(orch)))
                                log.LogCritical("Unable to create or access {pdiQueueName} to add File_ID {File_ID} Job_ID {Job_ID} for Publisher import.", pubQueueName, orch.FileID, orch.GetFile.JobID);
                            else
                                messageSent = true;
                        }
                        else
                            log.LogCritical("Publisher Queue in Environment Variable 'PUB_QueueName' not configured - Publisher import will not be performed on {name}", dto.FileName);
                    }
                    else
                        log.LogCritical("Failed to move {name} into {destDir}", dto.FileName, $"{pdiMainContainer}/importing");
                }
                else
                {
                    
                    if (!blobServiceClient.MoveTo(pdiMainContainer, $"processing/{dto.FileName}", pdiMainContainer, $"rejected/{dto.FileName}", log))
                        log.LogWarning("Failed to move {name} into {destDir}", dto.FileName, $"{pdiMainContainer}/rejected");
                }

                if (templateStream != null)
                    templateStream.Dispose();
            }
            catch (Exception e)
            {
                localError = $"Error in PDI Queue processing for {curStream.PdiFile.OnlyFileName} - error: {e.Message} at: {e.StackTrace}";
                log.LogCritical("Error in PDI Queue processing for {FileName} - error: {Message} at: {Stack}", curStream.PdiFile.OnlyFileName, e.Message, e.StackTrace);
            }
            finally
            {
                if (!messageSent)
                {
                    PDIBatch pdiBatch = new PDIBatch(pdiCon, dto.Batch_ID);
                    PDISendGrid pdiSendGrid = new PDISendGrid(Environment.GetEnvironmentVariable("SMTP_Password"), Environment.GetEnvironmentVariable("SMTP_FromEmail"), Environment.GetEnvironmentVariable("SMTP_FromName"));
                    // If there is an error message send the error email which only goes to the from email address
                    if (orch.ErrorMessage != null && orch.ErrorMessage.Trim().Length > 0)
                        pdiSendGrid.SendErrorMessage(curStream, orch.ErrorMessage);
                    if (localError != null && localError.Length > 0)
                        pdiSendGrid.SendErrorMessage(curStream, localError);

                    if (pdiBatch.Count() < 2)
                    {
                        if (!pdiSendGrid.SendTemplateMessage(curStream))
                            log.LogCritical(pdiSendGrid.ErrorMessage);
                    }
                    else
                    {
                        pdiBatch.SetComplete(dto.FileName);
                        if (pdiBatch.IsComplete())
                            if (!pdiSendGrid.SendBatchMessage(pdiBatch))
                                log.LogCritical(pdiSendGrid.ErrorMessage);
                    }
                    pdiBatch.Dispose();
                }
                orch.Dispose();
                curStream.Dispose();
                

            }
        }
    }
}
