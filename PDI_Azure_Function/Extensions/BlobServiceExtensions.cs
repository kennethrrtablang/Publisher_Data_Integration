using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace PDI_Azure_Function.Extensions
{
    static class BlobServiceExtensions
    {

        /// <summary>
        /// Uses the CopyTo function with delete on
        /// </summary>
        /// <param name="blobService"></param>
        /// <param name="sourceContainer"></param>
        /// <param name="sourcePath"></param>
        /// <param name="destContainer"></param>
        /// <param name="destPath"></param>
        /// <returns></returns>
        public static bool MoveTo(this BlobServiceClient blobService, string sourceContainer, string sourcePath, string destContainer, string destPath, ILogger log = null)
        {
            return CopyTo(blobService, sourceContainer, sourcePath, destContainer, destPath, true, log);
        }

        /// <summary>
        /// Copy a blob file from one container/directory into another with optional delete to become MoveTo
        /// </summary>
        /// <param name="blobService"></param>
        /// <param name="sourceContainer"></param>
        /// <param name="sourcePath"></param>
        /// <param name="destContainer"></param>
        /// <param name="destPath"></param>
        /// <param name="deleteSource"></param>
        /// <returns></returns>
        public static bool CopyTo(this BlobServiceClient blobService, string sourceContainer, string sourcePath, string destContainer, string destPath, bool deleteSource = false, ILogger log = null)
        {
            var blobSourceContainer = blobService.GetBlobContainerClient(sourceContainer);
            var blobDestContainer = blobService.GetBlobContainerClient(destContainer);

            if (!blobSourceContainer.Exists())
                return false;

            blobDestContainer.CreateIfNotExists(); // If the destination container doesn't exist create it
            BlobClient source = blobSourceContainer.GetBlobClient(sourcePath);
            BlobClient dest = blobDestContainer.GetBlobClient(destPath);
            if (source.Exists())
            {
                try
                {
                    CopyFromUriOperation res = dest.StartCopyFromUri(source.Uri);
                    var copyOp = res.WaitForCompletion(); // try to make sure we are waiting till the process finishes before continuing.
                    BlobProperties destProperties = dest.GetProperties(); // the copy returns immediately without finishing so get the properties in order to wait until the copy process is done

                    while (destProperties.CopyStatus == CopyStatus.Pending)
                    {
                        System.Threading.Thread.Sleep(1000);
                        destProperties = dest.GetProperties();
                    }

                   
                    if ( deleteSource && destProperties.CopyStatus == CopyStatus.Success)
                        return source.DeleteIfExists();
                    else if (destProperties.CopyStatus == CopyStatus.Success)
                        return true;
                    else
                        if (log != null)
                            log.LogCritical("Unable to copy blob from {sourceContainer} {sourcePath} to {destContainer} {destPath} - error: {status} ", sourceContainer, sourcePath, destContainer, destPath, destProperties.CopyStatusDescription);
                    return false;
                }
                catch (Exception err)
                {
                    if (log != null)
                        log.LogCritical("Unable to copy blob from {sourceContainer} {sourcePath} to {destContainer} {destPath} - error: {errorMessage} {trace}", sourceContainer, sourcePath, destContainer, destPath, err.Message, err.StackTrace);
                    return false;
                }

            }
            return false;
        }

        /// <summary>
        /// Saves a Stream to a specific container and path/filename
        /// </summary>
        /// <param name="blobService"></param>
        /// <param name="sourceStream"></param>
        /// <param name="filePath"></param>
        /// <param name="destContainer"></param>
        /// <returns></returns>
        public static bool SaveTo(this BlobServiceClient blobService, Stream sourceStream, string filePath, string destContainer)
        {
            var blobDestContainer = blobService.GetBlobContainerClient(destContainer);
            blobDestContainer.CreateIfNotExists(); // If the destination container doesn't exist create it
            BlobClient dest = blobDestContainer.GetBlobClient(filePath);

            if (sourceStream.CanSeek && sourceStream.Position != 0)
                sourceStream.Position = 0;

            var res = dest.Upload(sourceStream, true); // change to overwrite
            if (res.GetRawResponse().Status == 201 && dest.Exists())
                return true;

            return false;
        }

        /// <summary>
        /// Return a Stream object of the specified blob in the container
        /// </summary>
        /// <param name="blobService">The BlobServiceClient to use</param>
        /// <param name="filePath">The path/filename of the blob</param>
        /// <param name="sourceContainerName">The name of the container to use</param>
        /// <returns></returns>
        public static Stream Open(this BlobServiceClient blobService, string filePath, string sourceContainerName)
        {
            var blobSourceContainer = blobService.GetBlobContainerClient(sourceContainerName);
            if (blobSourceContainer.Exists())
            {
                BlobClient blobClient = blobSourceContainer.GetBlobClient(filePath);
                if (blobClient.Exists())
                    return blobClient.Open();
                else // where is the blob?
                {
                    //Console.WriteLine("Blob not Found");
                    return null;

                }    
            }
    
            return null;
        }

        public static Stream Open(this BlobClient source)
        {
            MemoryStream memStream = new MemoryStream();
            if (source.Exists())
            {
                var res = source.DownloadTo(memStream);
            }
            if (memStream.CanSeek && memStream.Position != 0)
                memStream.Position = 0;
            return memStream;
        }

        public static BlobClient Find(this BlobServiceClient blobService, string fileName, string sourceContainerName)
        {
            var blobSourceContainer = blobService.GetBlobContainerClient(sourceContainerName);
            if (blobSourceContainer.Exists())
            {
                BlobClient blob = blobSourceContainer.GetBlobClient(fileName);
                if (!blob.Exists())
                    blob = blobSourceContainer.GetBlobClient("completed/" + fileName);
                if (!blob.Exists())
                    blob = blobSourceContainer.GetBlobClient("rejected/" + fileName);
                if (!blob.Exists())
                    blob = blobSourceContainer.GetBlobClient("processing/" + fileName);

                return blob;
            }
            return null;
        }

        public static string GetListing(this BlobContainerClient blobContainerClient, int? segmentSize)
        {
            Task<string> task = Task.Run<string>(async () => await blobContainerClient.GetBlobsAsync(segmentSize));
            return task.Result;
        }

        private static async Task<string> GetBlobsAsync(this BlobContainerClient blobContainerClient,
                                               int? segmentSize)
        {
            DataTable dtBLob = new DataTable("Blob Contents");
            dtBLob.Columns.Add("File Name", typeof(string));
            dtBLob.Columns.Add("Created On", typeof(DateTimeOffset));
            dtBLob.Columns.Add("File Size (KB)", typeof(long));
            try
            {
                // Call the listing operation and return pages of the specified size.
                System.Collections.Generic.IAsyncEnumerable<Azure.Page<Azure.Storage.Blobs.Models.BlobItem>> resultSegment = blobContainerClient.GetBlobsAsync().AsPages(default, segmentSize);
                // Enumerate the blobs returned for each page.
                await foreach (Azure.Page<BlobItem> blobPage in resultSegment)
                {
                    foreach (BlobItem blobItem in blobPage.Values)
                        if (!blobItem.Name.EndsWith("/"))
                            dtBLob.Rows.Add(new object[] { blobItem.Name, blobItem.Properties.CreatedOn, blobItem.Properties.ContentLength / 1024 });
                }
                dtBLob.AcceptChanges();
            }
            catch (RequestFailedException e)
            {
                Console.WriteLine(e.Message);
                Console.ReadLine();
                throw;
            }
            return dtBLob.TableToHtml();
        }
    }
}
