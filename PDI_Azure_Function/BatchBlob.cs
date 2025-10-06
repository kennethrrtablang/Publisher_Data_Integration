using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Azure.Storage.Blobs;
using Publisher_Data_Operations.Helper;
using System.Collections.Generic;
using PDI_Azure_Function.Extensions;
using Azure.Storage.Queues;
using Azure.Storage.Blobs.Models;

namespace PDI_Azure_Function
{
    public static class BatchBlob
    {
        [FunctionName("Batch_Blob")]
        public static void Run([BlobTrigger("%IncomingBatchContainer%/{name}", Connection = "sapdi")] Stream myBlob, string name, Uri uri, BlobProperties properties, ILogger log) //, [Blob("pdi-result/processing/{name}", FileAccess.Write)] Stream outBlob
        {
            log.LogInformation("C# Blob trigger function 'Batch_Blob' started processing blob Name:{name} Size: {length} Bytes", name, myBlob.Length);


            // grab the environment variables we need
            string saPDI = Environment.GetEnvironmentVariable("sapdi");
            string pdiCon = Environment.GetEnvironmentVariable("PDI_ConnectionString");
            //string pdiIncomingContainer = Environment.GetEnvironmentVariable("IncomingContainer");
            string pdiIncomingBatchContainer = Environment.GetEnvironmentVariable("IncomingBatchContainer");
            string pdiMainContainer = Environment.GetEnvironmentVariable("MainContainer");
            string pdiQueueName = Environment.GetEnvironmentVariable("PDI_QueueName");
            //string pdiRejectedContainer = Environment.GetEnvironmentVariable("RejectedContainer");

            var splitName = name.Split('/');
            string onlyName;

            onlyName = splitName[splitName.Length - 1];
            try
            {
                BlobServiceClient blobServiceClient = new BlobServiceClient(saPDI);

                PDIBatch batch = new PDIBatch(pdiCon);

                if (Path.GetExtension(name).ToLower() == ".zip") // add register batch id for xlsx as well
                {
                    List<Tuple<int, string>> entries = batch.LoadBatch(new PDIStream(myBlob, onlyName, pdiCon), properties.CreatedOn);
                    foreach (Tuple<int, string> entry in entries)
                    {
                        if (blobServiceClient.SaveTo(batch.ExtractEntry(entry.Item2), $"processing/{entry.Item2}", pdiMainContainer))
                        {
                            if (!batch.MarkEntryExtracted(entry.Item2))
                                log.LogCritical("Failed to mark entry extracted on {name} in {zipName}", entry.Item2, name);

                            if (pdiQueueName != null && pdiQueueName.Length > 0)
                            {
                                QueueClient queueClient = new QueueClient(saPDI, pdiQueueName);
                                if (!queueClient.AddMessage(new DataTransferObject(entry.Item2, batch.BatchID, batch.RetryCount)))
                                    log.LogCritical("Unable to create or access {pdiQueueName} to add Batch_ID {Batch_ID} for Publisher import.", queueClient.Name, batch.BatchID);
                            }
                        }
                        else
                            log.LogCritical("Failed to extract/write {name} from {zipName} to {container}", entry.Item2, name, pdiMainContainer);
                    }
                    if (!blobServiceClient.MoveTo(pdiIncomingBatchContainer, name, pdiMainContainer, $"archive/{name}", log))
                        log.LogCritical("Failed to move {name} to {container}", name, $"{pdiMainContainer}/archive");
                }
                else if (Path.GetExtension(onlyName).ToLower() == ".xlsx")
                {
                    batch.RecordBatch(onlyName, properties.CreatedOn);
                    if (batch.BatchID >= 0)
                        if (batch.RecordSingle(onlyName))
                            if (blobServiceClient.MoveTo(pdiIncomingBatchContainer, name, pdiMainContainer, $"processing/{onlyName}", log))
                            {
                                if (!batch.MarkEntryExtracted(onlyName))
                                    log.LogCritical("Failed to mark entry extracted on {name} from non-batch file of the same name.", name);

                                if (pdiQueueName != null && pdiQueueName.Length > 0)
                                {
                                    QueueClient queueClient = new QueueClient(saPDI, pdiQueueName);
                                    queueClient.CreateIfNotExists();
                                    if (queueClient.Exists())
                                    {
                                        queueClient.SendMessage(new DataTransferObject(onlyName, batch.BatchID, batch.RetryCount).Base64Encode());
                                        log.LogInformation("Batch Blob Trigger: Created Queue message in {PDIQueueName}", pdiQueueName);
                                    }
                                    else
                                        log.LogCritical("Unable to create or access {pdiQueueName} to add Batch_ID {Batch_ID} for Publisher import.", pdiQueueName, batch.BatchID);
                                }
                            }
                            else
                                log.LogCritical("Failed to move {name} from {container} to {destination}.", name, pdiIncomingBatchContainer, pdiMainContainer);
                        else
                            log.LogCritical("Failed to create file for {name}. Error: {LastError}", onlyName, batch.LastError);
                    else
                        log.LogCritical("Failed to create batch for {name}. Error: {LastError}", onlyName, batch.LastError);
                }
                else if (onlyName == string.Empty)
                {
                    if (splitName.Length >= 2)
                        log.LogInformation("A new directory called {name} was created.", splitName[splitName.Length - 2]);
                    else
                        log.LogInformation("A new directory called {name} was created.", name);
                }
                else
                {
                    blobServiceClient.MoveTo(pdiIncomingBatchContainer, name, pdiMainContainer, $"rejected/{name}", log);
                    log.LogInformation("Unrecognized file {name} moved to rejected container.", name);
                }
            }
            catch (Exception err)
            {
                log.LogCritical(err, err.Message);
            }
        }
    }
}
