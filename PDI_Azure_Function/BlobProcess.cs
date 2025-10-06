using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Publisher_Data_Operations.Helper;
using Publisher_Data_Operations;
using PDI_Azure_Function.Extensions;
using Azure.Storage.Queues;

namespace PDI_Azure_Function
{
    public static class BlobProcess
    {
        //[FunctionName("Blob_Process")]
        //public static void Run([BlobTrigger("%IncomingContainer%/{name}", Connection = "sapdi")] Stream myBlob, string name, ILogger log, ExecutionContext exCtx) //, [Blob("pdi-result/processing/{name}", FileAccess.Write)] Stream outBlob
        //{
        //    log.LogInformation("C# Blob trigger function 'Blob_Process' started processing blob Name:{name} Size: {length} Bytes", name, myBlob.Length);


        //    // grab the environment variables we need
        //    string saPDI = Environment.GetEnvironmentVariable("sapdi");
        //    string pdiCon = Environment.GetEnvironmentVariable("PDI_ConnectionString");
        //    string pubCon = Environment.GetEnvironmentVariable("PUB_ConnectionString");
        //    string pdiIncomingContainer = Environment.GetEnvironmentVariable("IncomingContainer");
        //    string pdiMainContainer = Environment.GetEnvironmentVariable("MainContainer");
        //    string pubQueueName = Environment.GetEnvironmentVariable("PUB_QueueName");

        //    //string pdiQueueName = Environment.GetEnvironmentVariable("PublisherQueueName");
        //    //string pdiTemplateContainer = Environment.GetEnvironmentVariable("TemplateContainer");
        //    //string pdiRejectedContainer = Environment.GetEnvironmentVariable("RejectedContainer");
        //    //string pdiCompletedContainer = Environment.GetEnvironmentVariable("CompletedContainer");
        //    //string pdiProcessingContainer = Environment.GetEnvironmentVariable("ProcessingContainer");
        //    BlobServiceClient blobServiceClient = new BlobServiceClient(saPDI);

        //    // split the processing at this point - zip gets unzipped and dumped into the incoming directory while xlsx continues processing
        //    //if (Path.GetExtension(name).ToLower() == ".zip")
        //    //{
        //    //    using (var zip = new ZipArchive(myBlob, ZipArchiveMode.Read))
        //    //        foreach (ZipArchiveEntry entry in zip.Entries)
        //    //            if (!blobServiceClient.SaveTo(entry.Open(), entry.Name, pdiIncomingContainer)) // saving the files back to the incoming directory will trigger this blob processing again
        //    //                log.LogCritical("Failed to extract/write {name} from {zipName}", entry.Name, name);

        //    //    if (!blobServiceClient.MoveTo(pdiIncomingContainer, name, pdiResultContainer, $"archive/{name}", log))
        //    //        log.LogWarning("Failed to move archive {name} into archive directory", name);
        //    //}
        //    //else
        //    //{
        //    PDIStream curStream = new PDIStream(myBlob, name, pdiCon); //load the stream before moving it or it the stream contents won't be available
        //    curStream.PdiFile.FileRunID = exCtx.InvocationId.ToString();
            
        //    try
        //    {

            
        //    if (blobServiceClient.MoveTo(pdiIncomingContainer, name, pdiMainContainer, $"processing/{name}", log)) //pdiResultContainer, $"processing/{name}"
        //    {
        //        PDIStream templateStream = null;
        //        PDISendGrid pdiSendGrid = new PDISendGrid(Environment.GetEnvironmentVariable("SMTP_Password"), Environment.GetEnvironmentVariable("SMTP_FromEmail"), Environment.GetEnvironmentVariable("SMTP_FromName"));

        //        log.LogInformation("Blob Trigger: Started Processing {name}", curStream.PdiFile.OnlyFileName);
        //        if (templateStream is null || templateStream.PdiFile.OnlyFileName != curStream.PdiFile.GetDefaultTemplateName())
        //        {
        //            BlobClient templateBlob = blobServiceClient.GetBlobContainerClient(pdiMainContainer).GetBlobClient($"templates/{curStream.PdiFile.GetDefaultTemplateName()}");
        //            if (!templateBlob.Exists())
        //            {
        //                log.LogCritical("Blob Trigger: Could not locate template file: {templateName}", templateBlob.Name);
        //            }
        //            else
        //            {
        //                templateStream = new PDIStream(templateBlob.Open(), templateBlob.Name);
        //                log.LogInformation("Blob Trigger: Loaded template {templateName}", templateBlob.Name);
        //            }      
        //        }

        //        Orchestration orch = new Orchestration(pdiCon, null);
        //        bool result = orch.ProcessFile(curStream, templateStream, 0);
        //        log.LogInformation("Blob Trigger: Finished Processing {name} result: {result} Error?: {error}", curStream.PdiFile.OnlyFileName, result, orch.ErrorMessage);
        //        //string destDir = "rejected";
        //        if (result && orch.FileStatus)
        //        {
        //            //destDir = "completed";
        //            if (blobServiceClient.MoveTo(pdiMainContainer, $"processing/{name}", pdiMainContainer, $"importing/{name}", log))
        //            {
        //                if (pubQueueName != null && pubQueueName.Length > 0)
        //                {
        //                    QueueClient queueClient = new QueueClient(saPDI, pubQueueName);
        //                    queueClient.CreateIfNotExists();
        //                    if (queueClient.Exists())
        //                    {
        //                        queueClient.SendMessage(new DataTransferObject(orch).Base64Encode());
        //                        log.LogInformation("Blob Trigger: Created Queue message in {PublisherQueueName}", pubQueueName);
        //                    }
        //                    else
        //                    {
        //                        log.LogCritical("Unable to create or access {pdiQueueName} to add File_ID {File_ID} Job_ID {Job_ID} for Publisher import.", pubQueueName, orch.FileID, orch.GetFile.JobID);
        //                    }                              
        //                }
        //                else
        //                {
        //                    log.LogCritical("Publisher Queue in Environment Variable 'PublisherQueueName' not configured - Publisher import will not be performed on {name}", name);
        //                }    
        //            }
        //            else
        //            {
        //                log.LogCritical("Failed to move {name} into {destDir}", name, "{pdiMainContainer}/importing");
        //            }  
        //        }
        //        else
        //        {
        //            if (!blobServiceClient.MoveTo(pdiMainContainer, $"processing/{name}", pdiMainContainer, $"rejected/{name}", log))
        //                log.LogWarning("Failed to move {name} into {destDir}", name, $"{pdiMainContainer}/rejected");

        //            if (!pdiSendGrid.SendTemplateMessage(curStream))
        //                log.LogCritical(pdiSendGrid.ErrorMessage);
        //        }
        //            orch.Dispose();

        //            if (templateStream != null)
        //                templateStream.Dispose();
        //    }
        //    else
        //        log.LogCritical("Failed to move {name} into {container} - processing halted", name, $"{pdiMainContainer}/processing");

        //    }
        //    catch (Exception e)
        //    {
        //        log.LogCritical("Error in Blob processing for {FileName} - error: {Message} at: {Stack}", curStream.PdiFile.OnlyFileName, e.Message, e.StackTrace);
        //    }
        //    finally
        //    {
        //        curStream.Dispose();
        //    }
        //}
    }    
}
