using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Publisher_Data_Operations;
using Publisher_Data_Operations.Helper;
using Publisher_Data_Operations.Extensions;
using System.Text;
using System.Data;
using System.Web;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using PDI_Azure_Function.Extensions;
using System.Net.Http;
using System.Net;
using System.Net.Http.Headers;
using System.IO;
using System.Security.Claims;
using System.Linq;
using Azure.Core;
using Microsoft.Azure.EventGrid;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Nancy.Json;
using Newtonsoft.Json.Linq;

namespace PDI_Azure_Function
{
    public static class StatusPage
    {
        const int PageSize = 10;

        [FunctionName("Status")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "Status/{offSet?}")] HttpRequest req, int? offSet, ILogger log, ClaimsPrincipal claimIdentity)
        {
            log.LogInformation("C# Status trigger function processed a request. Claim: {name}", claimIdentity.Identity.Name );

            //Extract User ID and Claims from the request headers
            var principal_name = req.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].FirstOrDefault();
            var principal_Id = req.Headers["X-MS-CLIENT-PRINCIPAL-ID"].FirstOrDefault();
            string easyAuthProvider = req.Headers["X-MS-CLIENT-PRINCIPAL-IDP"].FirstOrDefault();
            string clientPrincipalEncoded = req.Headers["X-MS-CLIENT-PRINCIPAL"].FirstOrDefault();

            log.LogInformation("Claims Info - Principal: {1} PrincipleID {2} EasyAuthProvider {3} ClientPrincipal {4}", principal_name, principal_Id, easyAuthProvider, clientPrincipalEncoded);
            var content = new ContentResult();

            content.Content = GetStatusHTMLPage(offSet, req);
            content.ContentType = "text/html";

            return content;
        }

        public static string GetHeader(string title)
        {
            return "<html><head><title>" + HttpUtility.HtmlEncode(title) + @"</title>
<link rel='stylesheet' href='https://cdn.jsdelivr.net/npm/bootstrap@5.1.3/dist/css/bootstrap.min.css' integrity='sha384-1BmE4kWBq78iYhFldvKuhfTAU6auU8tT94WrHftjDbrCEXSU1oBoqyl2QvZ6jIW3' crossorigin='anonymous'>
<link rel='shortcut icon' href='https://investorcom.com/wp-content/themes/sme-investorcom-child/favicon.ico' type='image/vnd.microsoft.icon'>
	<link rel='apple-touch-icon-precomposed' sizes='144x144' href='https://investorcom.com/wp-content/themes/sme-investorcom-child/favicon-144.png'>
</head>
<body>";
        }

        public static string GetJavaScript(string baseURL = "../")
        {
            return @"<script>
//Run this when the HTML gets loaded.
window.onload = () => {
  //Add event on the file input.
  document.getElementById('fileUpload').addEventListener('change', fileUploaded);
}
        function fileUploaded(event){
            //Get the upload input element.
            const fileInput = event.target;
  //Get the first file.
if (fileInput.files.length > 0) {
  const fileData = fileInput.files[0];
        //Create a form data object.
        let formData = new FormData();
        //Add file.
        formData.append('file', fileData);
  //Set request properties.
  const requestProperties = {
    method: 'POST',
    body: formData //Add form data to request.
  };
        //Url of the backend where you want to upload the file to.
        const fileUploadURL = '" + baseURL + @"FileUpload';
        //Make the request.
        makeRequest(fileUploadURL, requestProperties, event.target, document.getElementById('fileResponse'));
}

    }

    async function makeRequest(url, requestProperties, source, message)
    {
        let response = await fetch(url, requestProperties);
        console.log(response.status);
        if (response.status != 200) {
            message.innerHTML = 'Something went wrong';
        }
        else {
            source.value = null;
            message.innerHTML = 'File sent for processing';
            message.classList.remove('d-none')
        } 
    }
</script>";
        }
        public static string GetFooter()
        {
            return "</body></html>";
        }

        public static string GetStatusHTMLPage(int? offSet, HttpRequest req)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append(GetHeader("Status Page - " + Guid.NewGuid()));

            DBConnection dbCon = new DBConnection(Environment.GetEnvironmentVariable("PDI_ConnectionString"));
            DBConnection dbConPUB = new DBConnection(Environment.GetEnvironmentVariable("PUB_ConnectionString"));

            string baseURL = (req != null ? req.GetBaseURL() : "../");

            var intLog = new Logger(dbCon);
            var gen = new Generic(dbCon, intLog);
            builder.Append("<div class='container-fluid'>");
            builder.Append("<H2>" + Environment.GetEnvironmentVariable("SMTP_FromName") + "</H2><br />");
            builder.Append(dbCon.GetServer + " - " + dbCon.GetDatabase + "<br />");
            builder.Append(dbConPUB.GetServer + " - " + dbConPUB.GetDatabase + "<br />");

            string result = DBConnection.TestSQLConnectionString(Environment.GetEnvironmentVariable("PDI_ConnectionString"), "SELECT Count(*) FROM [pdi_Global_Text_Language];");
            if (result != null && result.Length > 0)
                builder.Append("</div><div class='container-fluid alert alert-danger'>Error with PDI Database: " + result);

            result = DBConnection.TestSQLConnectionString(Environment.GetEnvironmentVariable("PUB_ConnectionString"), "SELECT Count(*) FROM [Roles];");
            if (result != null && result.Length > 0)
                builder.Append("</div><div class='container-fluid alert alert-danger'>Error with Publisher Database: " + result);

            BlobServiceClient blobService = new BlobServiceClient(Environment.GetEnvironmentVariable("sapdi"));
            BlobContainerClient batchBlob = blobService.GetBlobContainerClient(Environment.GetEnvironmentVariable("IncomingBatchContainer"));

            builder.Append($"</div><br /><div class='container'><div class='row'><div class='col'><div><strong>{batchBlob.Name}</strong> Blob Container Contents<br />");
            builder.Append(batchBlob.GetListing(10));
            builder.Append("</div>");


            QueueClient queueClient = new QueueClient(Environment.GetEnvironmentVariable("sapdi"), Environment.GetEnvironmentVariable("PDI_QueueName"));

            builder.Append($"<br /><div><strong>{queueClient.Name}</strong> Queue Count<br />");
            builder.Append(queueClient.GetQueueLength());
            builder.Append("</div>");

            queueClient = new QueueClient(Environment.GetEnvironmentVariable("sapdi"), Environment.GetEnvironmentVariable("PUB_QueueName"));

            builder.Append($"<br /><div><strong>{queueClient.Name}</strong> Queue Count<br />");
            builder.Append(queueClient.GetQueueLength());
            builder.Append("</div></div>");

            builder.Append("<div class='col'><div><label class='form-label' for='file'>Choose a file to process:</label><input class='form-control' id='fileUpload' name='file' type='file' /></div><div class='alert alert-primary d-none' role='alert' id='fileResponse'></div></div></div></div>");

            //builder.Append($"<input id=\"fileUpload\" name=\"file\" type=\"file\" /><form enctype=\"multipart/form-data\" action=\"{baseURL + "FileUpload"}\" method =\"POST\"><input type=\"hidden\" name=\"MAX_FILE_SIZE\" value=\"100000\" /><label class=\"form-label\" for=\"uploadedfile\">Choose a file to process: </label><input class=\"form-control\" id=\"uploadedfile\" type=\"file\" /><input type=\"submit\" value=\"Process File\" /></form>");


            builder.Append("<div class='container-fluid'>");

            int localOffset = 0;
            if (offSet.HasValue)
                localOffset = (int)offSet;

            if (localOffset < PageSize)
                builder.Append($"<a class=\"btn btn-primary disabled\" href=\"#\" role=\"button\" aria-disabled=\"true\">Previous</a>");
            else
                builder.Append($"<a class=\"btn btn-primary\" href=\"{baseURL}Status/{localOffset - PageSize}\" role=\"button\">Previous</a>");
            DataTable dt = gen.GetStatusTable(localOffset, PageSize);
            if (dt.Rows.Count < PageSize)
                builder.Append($"<a class=\"btn btn-primary float-end disabled\" href=\"#\" role=\"button\">Next</a>");
            else
                builder.Append($"<a class=\"btn btn-primary float-end\" href=\"{baseURL}Status/{localOffset + PageSize}\" role=\"button\">Next</a>");
            builder.Append(dt.TableToHtml(baseURL));
            builder.Append("</div>");
            intLog.Dispose();

            builder.Append(GetJavaScript(baseURL));
            builder.Append(GetFooter());


            return builder.ToString();
        }

        //https://stackoverflow.com/questions/19682996/datatable-to-html-table
        public static string TableToHtml(this DataTable dt, string baseURL = "../")
        {
            if (dt is null || dt.Rows.Count == 0) return "Empty"; // enter code here
            StringBuilder builder = new StringBuilder();
            
            builder.Append("<table class='table'>");
            //builder.Append("style='border: solid 1px Silver; font-size: x-small;'>");
            builder.Append("<thead><tr>"); // align = 'left' valign = 'top'
            foreach (DataColumn c in dt.Columns)
            {
                if (!c.ColumnName.Contains("_ID"))
                {
                    builder.Append("<th>"); // align='left' valign='top' <b>
                    builder.Append(HttpUtility.HtmlEncode(c.ColumnName.Replace('_', ' ')));
                    builder.Append("</th>"); //</b>
                }
            }
            builder.Append("</tr></thead><tbody>");
            foreach (DataRow r in dt.Rows)
            {
                builder.Append("<tr>"); // align='left' valign='top'
                foreach (DataColumn c in dt.Columns)
                {
                    if (!c.ColumnName.Contains("_ID"))
                    {
                        builder.Append("<td style='word-wrap: break-word;max-width: 160px;'>"); // align='left' valign='top'
                        if (c.DataType.Name == "DateTime")
                        {
                            DateTime cur = (DateTime)r[c.ColumnName];
                            builder.Append(HttpUtility.HtmlEncode(cur.ToLocalTime().ToString("yyyy-MM-dd h:mm:ss tt")));
                        }
                        else if (c.DataType.Name == "DateTimeOffset")
                        {
                            DateTimeOffset cur = (DateTimeOffset)r[c.ColumnName];
                            builder.Append(HttpUtility.HtmlEncode(cur.ToLocalTime().ToString("yyyy-MM-dd h:mm:ss tt")));
                        }
                        else if (c.ColumnName.IndexOf("File_Name", StringComparison.OrdinalIgnoreCase) >= 0)
                            builder.Append($"<a title=\"Job ID: {r["Job_ID"]}\" href=\"{baseURL + "DownloadBlob/" + HttpUtility.HtmlEncode(r[c.ColumnName])}\">{HttpUtility.HtmlEncode(r[c.ColumnName])}</a>");
                        else if (c.ColumnName.IndexOf("Max_Message", StringComparison.OrdinalIgnoreCase) >= 0 && r[c.ColumnName].ToString().Length > 0)
                            builder.Append($"<a target=\"_blank\" href=\"{baseURL + "ValidationMessages/" + HttpUtility.HtmlEncode(r["File_ID"])}\">{HttpUtility.HtmlEncode(r[c.ColumnName])}</a>");
                        else
                            builder.Append(HttpUtility.HtmlEncode(r[c.ColumnName]));

                        builder.Append("</td>");
                    }
                }
                builder.Append("</tr>");
            }
            builder.Append("</tbody></table>");

            return builder.ToString();
        }

        [FunctionName("DownloadBlob")]
        public static async Task<HttpResponseMessage> DownloadBlob(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "DownloadBlob/{fileName}")] HttpRequest req, string fileName, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            BlobServiceClient blobService = new BlobServiceClient(Environment.GetEnvironmentVariable("sapdi"));
            BlobClient blobClient = blobService.Find(fileName, Environment.GetEnvironmentVariable("MainContainer"));


            HttpResponseMessage message = new HttpResponseMessage(HttpStatusCode.OK);
            if (blobClient.Exists())
            {
                var blobProperties = blobClient.GetProperties();
                message.Content = new StreamContent(blobClient.Open());
                message.Content.Headers.ContentLength = blobProperties.Value.ContentLength;
                message.StatusCode = HttpStatusCode.OK;
                message.Content.Headers.ContentType = new MediaTypeHeaderValue(blobProperties.Value.ContentType);
                message.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
                {
                    FileName = fileName,
                    Size = blobProperties.Value.ContentLength
                };
                return message;
            }

            message.StatusCode = HttpStatusCode.NotFound;
            return message;
        }


        [FunctionName("ValidationMessages")]
        public static async Task<IActionResult> ValidationMessages(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ValidationMessages/{fileName}")] HttpRequest req, string fileName, ILogger log)
        {
            log.LogInformation("ValidationMessages trigger function processed a request.");

            DBConnection dbCon = new DBConnection(Environment.GetEnvironmentVariable("PDI_ConnectionString"));
            var intLog = new Logger(dbCon);
            var gen = new Generic(dbCon, intLog);

            var content = new ContentResult();

            if (fileName.Length > 0)
            {
                content.Content = GetHeader("Validation - " + fileName) + TableToHtml(gen.GetValidationMessages(fileName)) + GetFooter();
                content.ContentType = "text/html";
            }
            else
                content.StatusCode = 404;


            return content;
        }


        [FunctionName("FileUpload")]
        public static async Task<IActionResult> FileUpload(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "FileUpload")] HttpRequest req, ILogger log)
        {
            log.LogInformation("FileUpload trigger function processed a request.");

            Stream myBlob = new MemoryStream();
            try
            {
                if (req.Form.Files.Count > 0)
                {
                    var file = req.Form.Files["File"];
                    myBlob = file.OpenReadStream();
                    var blobClient = new BlobContainerClient(Environment.GetEnvironmentVariable("sapdi"), Environment.GetEnvironmentVariable("IncomingBatchContainer"));
                    var blob = blobClient.GetBlobClient($"web/{file.FileName}");
                    await blob.UploadAsync(myBlob);
                    return new OkObjectResult("File uploaded successfully");
                }
                else
                    return new NotFoundObjectResult("No file found");
            }
            catch (Exception e)
            {
                return new BadRequestObjectResult("An error occurred: " + e.Message);
            }
        }

        //[FunctionName("CustomerEventHandlerEVG")]
        //public static void Run([EventGridTrigger] EventGridEvent eventGridEvent, ILogger log)
        //{
        //    Orchestration orch = new Orchestration(Environment.GetEnvironmentVariable("PDI_ConnectionString"), Environment.GetEnvironmentVariable("PUB_ConnectionString"));
        //    orch.HandleEventMessage(eventGridEvent);
        //}



        [FunctionName("EventGridHandlerHTTP")]
        public static async Task<IActionResult> EventGridHandlerHTTP(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "EventGridHandlerHTTP")] HttpRequest req, ILogger log)
        {
            string requestBody = await req.ReadAsStringAsync();
            string response = string.Empty;

            EventGridSubscriber eventGridSubscriber = new EventGridSubscriber();
            EventGridEvent[] eventGridEvents = eventGridSubscriber.DeserializeEventGridEvents(requestBody);
            Orchestration orch = new Orchestration(Environment.GetEnvironmentVariable("PDI_ConnectionString"), Environment.GetEnvironmentVariable("PUB_ConnectionString"));
            foreach (var eventGridEvent in eventGridEvents)
            {
                orch.HandleEventMessage(eventGridEvent);
            }
            return new OkObjectResult("File uploaded successfully");
        }
    }
}
