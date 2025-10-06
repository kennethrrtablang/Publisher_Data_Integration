using System;
using Azure.Storage.Blobs;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Publisher_Data_Operations;
using Publisher_Data_Operations.Helper;
using PDI_Azure_Function.Extensions;

namespace PDI_Azure_Function
{
    public static class QueueImport
    {
        [FunctionName("Queue_Import")]
        public static void Run([QueueTrigger("%PUB_QueueName%", Connection = "sapdi")] string myQueueItem, ILogger log) // 
        {
            string pdiCon = Environment.GetEnvironmentVariable("PDI_ConnectionString");
            string pdiMainContainer = Environment.GetEnvironmentVariable("MainContainer");
            DataTransferObject dto = new DataTransferObject(myQueueItem);
            log.LogInformation($"PUB Queue trigger function starting process : {dto.FileName}");
            PDIBatch pdiBatch = new PDIBatch(pdiCon, dto.Batch_ID);
            PDISendGrid pdiSendGrid = new PDISendGrid(Environment.GetEnvironmentVariable("SMTP_Password"), Environment.GetEnvironmentVariable("SMTP_FromEmail"), Environment.GetEnvironmentVariable("SMTP_FromName"));


            BlobServiceClient blobServiceClient = new BlobServiceClient(Environment.GetEnvironmentVariable("sapdi"));
            PDIStream curStream = new PDIStream(blobServiceClient.Open($"importing/{dto.FileName}", pdiMainContainer), dto.FileName, pdiCon);

            try
            {

                Orchestration orch = new Orchestration(pdiCon, Environment.GetEnvironmentVariable("PUB_ConnectionString"), curStream);

                // if running in the staging environment see if the stored procedure is out of date - STAGING should be a perfect copy of production so this isn't needed anymore 20221114
                //if (Environment.GetEnvironmentVariable("PUB_ConnectionString").IndexOf("staging", StringComparison.OrdinalIgnoreCase) >= 0)
                //    orch.ClearImportSP();

                //PDIFile pdiFile = new PDIFile(dto.FileName, orch.GetPDIConnection, true, int.Parse(dto.File_ID));
                
               

                if (int.TryParse(dto.Job_ID, out int jobID))
                {
                    if (orch.PublisherImport(jobID, log))
                    {
                        if (pdiBatch.BatchID > -1 && pdiBatch.Count() > 1 )
                        {
                            pdiBatch.SetComplete(dto.FileName);
                            if (pdiBatch.IsComplete())
                            {

                                PDIStream templateStream = null;
                                BlobClient templateBlob = blobServiceClient.GetBlobContainerClient(pdiMainContainer).GetBlobClient($"templates/{curStream.PdiFile.GetDefaultStaticTemplateName()}");

                                if (!templateBlob.Exists())
                                    log.LogCritical("Blob Trigger: Could not locate template file: {templateName}", templateBlob.Name);
                                else
                                    templateStream = new PDIStream(templateBlob.Open(), templateBlob.Name);

                                if (!pdiSendGrid.SendBatchMessage(pdiBatch))
                                    log.LogCritical("Unable to send Publisher batch import notification email for Batch ID {BatchID} error: {error}", dto.Batch_ID, pdiSendGrid.ErrorMessage);
                            }
                        }
                        else
                        {
                            if (!pdiSendGrid.SendTemplateMessage(curStream))
                                log.LogCritical("Unable to send Publisher import notification email for Job_ID {Job_ID} error: {error}", dto.Job_ID, pdiSendGrid.ErrorMessage);
                        }
                        blobServiceClient.MoveTo(pdiMainContainer, $"importing/{dto.FileName}", pdiMainContainer, $"completed/{dto.FileName}");
                    }
                    else
                    {
                        pdiBatch.SetComplete(dto.FileName);
                        pdiSendGrid.SendErrorMessage(curStream, orch.ErrorMessage);
                        log.LogCritical("Failed to import to Publisher Job_ID {Job_ID} error: {error}", dto.Job_ID, orch.ErrorMessage);
                        blobServiceClient.MoveTo(pdiMainContainer, $"importing/{dto.FileName}", pdiMainContainer, $"rejected/{dto.FileName}");
                    }

                }
                orch.Dispose();
                log.LogInformation($"PUB Queue trigger function finished process : {dto.FileName}");

            }
            catch (Exception e)
            {
                log.LogCritical("Error in PUB Queue processing for {FileName} - error: {Message} at: {Stack}", dto.FileName, e.Message, e.StackTrace);
                pdiSendGrid.SendErrorMessage(curStream, $"Error in PUB Queue processing for {dto.FileName} - error: {e.Message} at: {e.StackTrace}");
                pdiBatch.SetComplete(dto.FileName);
                if (pdiBatch.IsComplete())
                    if (!pdiSendGrid.SendBatchMessage(pdiBatch))
                        log.LogCritical(pdiSendGrid.ErrorMessage);
            }
            finally
            {
                curStream.Dispose();
                pdiBatch.Dispose();
            }
        }
    }
}
