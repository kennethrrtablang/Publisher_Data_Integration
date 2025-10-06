using System;
using System.Collections.Generic;
using System.Text;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;

namespace PDI_Azure_Function.Extensions
{
    static class QueueServiceExtensions
    {
        public static int GetQueueLength(this QueueClient queueClient)
        {
            if (queueClient.Exists())
            {
                QueueProperties properties = queueClient.GetProperties();
                // Retrieve the cached approximate message count.
                return properties.ApproximateMessagesCount;
            }
            return -1;
        }

        public static bool AddMessage(this QueueClient queueClient, DataTransferObject dto, ILogger log = null)
        {
            queueClient.CreateIfNotExists();
            if (queueClient.Exists())
            {
                queueClient.SendMessage(dto.Base64Encode());
                if (log != null)
                    log.LogInformation("Batch Blob Trigger: Created Queue message in {PDIQueueName}", queueClient.Name);
                return true;
            }
            return false;
        } 
    }
}
