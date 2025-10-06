using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Publisher_Data_Operations.Extensions;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Publisher_Data_Operations.Helper
{
    public class PDISendGrid
    {
        private static readonly HttpClient httpClient = new HttpClient();
        public EmailAddress FromEmail = null;
        public string HTMLTemplate = "<p><h2>Client Information</h2>Company Name: <b><Company_Name></b><br>Document Type Name: <Document_Type_Name></p><p><h2>File Received</h2>File Name: <File_Name><br>File Receipt time: <Batch_Created_Timestamp></p><TranslationMessage><p><h2>Validation Status:</h2>Validation Status: [ValidationString]<br>Record Count: <Number_of_Records></p><p><h2>Processing Status:</h2>Start time: <Job_Start><br>End time: <Import_End></p><p><h2>Publisher Data Load Status:</h2>Data load status: <b><Job_Status></b></p> ";
        public string HTMLErrorTemplate = "<p><h2>ERROR</h2><p>Message: <ErrorMessage></p><p><h2>Client Information</h2>Company Name: <b><Company_Name></b><br>Document Type Name: <Document_Type_Name></p><p><h2>File Received</h2>File Name: <File_Name><br>File Receipt time: <Batch_Created_Timestamp></p><TranslationMessage><p><h2>Validation Status:</h2>Validation Status: <b>Error</b> see attachment<br>Record Count: <Number_of_Records></p><p><h2>Processing Status:</h2>Start time: <Job_Start><br>End time: <Import_End></p><p><h2>Publisher Data Load Status:</h2>Data load status: <b><Job_Status></b></p> ";
        public string HTMLBatchTemplate = "<p><h2>Client Information</h2>Company Name: <b><Company_Name></b></p><p><h2>File Received</h2>File Name: <File_Name><br>File Receipt time: <Batch_Created_Timestamp><br>File Count: <Number_of_Files></p></p><TranslationMessage><p><h2>Processing Status:</h2>Start time: <Job_Start><br>End time: <Import_End></p><p><h2>Publisher Data Load Status:</h2><Table_of_Results></p>";

        public string HTMLtranslation = "<p><h2>Translation Status:</h2>The attached text file indicates wording for which a French translation has not been provided. Please provide a static file with the updated translations and resubmit this file for processing.</p>";

        public string ErrorMessage { get; set; }
        private SendGridClient sgClient = null;

        public PDISendGrid(string apiKey, string fromEmail = "pdi_support@investorcom.com", string fromName = "PDI Support")
        {
            sgClient = new SendGridClient(httpClient, new SendGridClientOptions { ApiKey = apiKey, HttpErrorAsException = true });
            FromEmail = new EmailAddress(fromEmail, fromName);
        }

        public void SendEmail(string toEmail, string toName, string subject = "Test") 
        {
            var msg = MailHelper.CreateSingleEmail(FromEmail, new EmailAddress(toEmail, toName), subject, "QA Test Message", HTMLTemplate);
            Task<Response> response = null;
            try
            {
                response = sgClient.SendEmailAsync(msg);
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message);
            }

            //return response;
            while (!response.IsCompleted)
            {
                response.Wait(-1);
            }
            //Console.WriteLine(msg.Serialize());
            //Console.WriteLine(response.IsCompleted);
            //Console.WriteLine(response.Result.StatusCode);
            //Console.WriteLine("\n\nPress <Enter> to continue.");
            //Console.ReadLine();

        }

        public bool SendErrorMessage(PDIStream attach, string errorMessage = "An Error Occurred")
        {
            SendGridMessage message = new SendGridMessage()
            {
                From = FromEmail,
                Subject = "PDI ERROR OCCURED",
                HtmlContent = HTMLErrorTemplate
            };
            message.AddTo(FromEmail);
            message.AddHeader("Priority", "Urgent"); // add high priority flag
            message.AddHeader("Importance", "high"); // add high importance flag for other email programs

            if (attach != null && attach.SourceStream != null && attach.PdiFile != null && attach.SourceStream.Length > 0)
            {
                bool isValid = attach.PdiFile.IsValidParameters();
                message.Subject = message.Subject + " - " + (isValid ? "" : "Failed - ") + attach.PdiFile.OnlyFileName;

                Dictionary<string, string> paramDict = attach.PdiFile.GetAllParameters();
                paramDict.Add("ErrorMessage", errorMessage);
                message.HtmlContent = ReplaceValidation(HTMLErrorTemplate.ReplaceByDictionary(paramDict), isValid);
                if (!isValid)
                {
                    //message.AddAttachment(attach.PdiFile.OnlyFileName, attach.SourceStream.ToBase64String()); //, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"  This Remove the original file sending via email, to resolve T20230717.0010
                    if (attach.PdiFile.CountValidationErrors() > 0)
                   message.AddAttachment(attach.PdiFile.FileNameWithoutExtension + "_Validation_Errors.csv", attach.PdiFile.GetValidationErrorsCSV().ToBase64String()); //, "text/csv"
                }  
            }
            else // send a simplified message 
                message.PlainTextContent = errorMessage;

            try
            {
                return SendEmail(message);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error sending email to '{FromEmail}' error was: {ex.Message}";
                return false;
            }
        }

        private bool SendEmail(SendGridMessage sg)
        {
            Task<Response> response = null;
            try
            {
                response = sgClient.SendEmailAsync(sg);
            }
            catch (Exception err)
            {
                ErrorMessage = err.Message;
                return false;
            }

            //return response;
            while (!response.IsCompleted)
                response.Wait(-1);

            if (response.Result.IsSuccessStatusCode)
                return true;

            return false;
        }

        public bool SendTemplateMessage(PDIStream attach, string toAddress = null, string subject = "PDI")
        {
            SendGridMessage message = new SendGridMessage()
            {
                From = FromEmail,
                Subject = subject,
                HtmlContent = HTMLTemplate,
            };
            //MailHelper.CreateSingleEmail(FromEmail, new EmailAddress(toEmail, toName), subject);
           
            if (attach != null && attach.SourceStream != null && attach.PdiFile != null && attach.SourceStream.Length > 0)
            {
                bool isValid = attach.PdiFile.IsValidParameters();
                message.Subject = message.Subject + " - " + (isValid ? "" : "Failed - ") + attach.PdiFile.OnlyFileName;
                
                Dictionary<string, string> paramDict = attach.PdiFile.GetAllParameters();
                
                if (!isValid)
                {
                    //message.AddAttachment(attach.PdiFile.OnlyFileName, attach.SourceStream.ToBase64String()); //, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    if (attach.PdiFile.CountValidationErrors() > 0)  
                        message.AddAttachment(attach.PdiFile.FileNameWithoutExtension + "_Validation_Errors.csv", attach.PdiFile.GetValidationErrorsCSV().ToBase64String()); //, "text/csv"
                }
                else if (attach.PdiFile.CountValidationErrors() > 0)
                   message.AddAttachment(System.IO.Path.GetFileNameWithoutExtension(paramDict["File_Name"]) + "_Validation_Messages.csv", attach.PdiFile.GetValidationErrorsCSV().ToBase64String()); //, "text/csv"

                if (attach.PdiFile.CountMissingFrench() > 0)
                {
                    paramDict.Add("TranslationMessage", HTMLtranslation);
                    message.AddAttachment(System.IO.Path.GetFileNameWithoutExtension(paramDict["File_Name"]) + "_MissingFrench.csv", attach.PdiFile.GetMissingFrenchCSV().ToBase64String()); //, "text/csv"
                }


                if (toAddress != null && toAddress.Length > 0)
                    message.AddTo(toAddress);
                else if (paramDict != null && paramDict.ContainsKey("Notification_Email_Address"))
                {
                    List<EmailAddress> toEmails = new List<EmailAddress>(paramDict["Notification_Email_Address"].CountOccurances(",") + 1);
                    foreach (string email in paramDict["Notification_Email_Address"].Split(','))
                        toEmails.Add(new EmailAddress(email)); //email.ExtractEmailAddress(), email.ExtractEmailName()

                    message.AddTos(toEmails);
                }
                else // we can't load the notification email addresses so send to the from TODO: Split the notification email into Global and Client and then send to global here
                {
                    message.Subject = "CLIENT NOTIFICATION FAILED - " + message.Subject;   // Modify the subject to indicate client is not notified
                    message.AddHeader("Priority", "Urgent"); // add high priority flag
                    message.AddHeader("Importance", "high"); // add high importance flag for other email programs
                    message.AddTo(FromEmail);
                }

                message.HtmlContent = ReplaceValidation(HTMLTemplate.ReplaceByDictionary(paramDict), isValid);

                try
                {
                    return SendEmail(message);
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error sending email to '{paramDict["Notification_Email_Address"]}' error was: {ex.Message}";
                    return false;
                }
            }
            return false;
        }

        public bool SendBatchMessage(PDIBatch batch, string toAddress = null, string subject = "PDI")
        {
            SendGridMessage message = new SendGridMessage()
            {
                From = FromEmail,
                Subject = subject,
                HtmlContent = HTMLBatchTemplate,
            };

            Dictionary<string, string> paramDict = batch.GetMessageParameters();
            paramDict.Add("Table_of_Results", batch.HTMLTableResults());
            message.Subject = message.Subject + " - " + paramDict["File_Name"];
            
            
            if (batch.ValidationMessageCount() > 0)
                message.AddAttachment(System.IO.Path.GetFileNameWithoutExtension(paramDict["File_Name"]) + "_Validation_Messages.csv", batch.GetValidationErrorsCSV().ToBase64String()); //, "text/csv"

            if (batch.CountMissingFrench() > 0)
            { 
                paramDict.Add("TranslationMessage", HTMLtranslation);
                message.AddAttachment(System.IO.Path.GetFileNameWithoutExtension(paramDict["File_Name"]) + "_MissingFrench.csv", batch.GetMissingFrenchCSV().ToBase64String()); //, "text/csv"
            }
            if (toAddress != null && toAddress.Length > 0)
                message.AddTo(toAddress);
            else if (paramDict != null && paramDict.ContainsKey("Notification_Email_Address"))
            {
                List<EmailAddress> toEmails = new List<EmailAddress>(paramDict["Notification_Email_Address"].CountOccurances(",") + 1);
                foreach (string email in paramDict["Notification_Email_Address"].Split(','))
                    toEmails.Add(new EmailAddress(email)); //email.ExtractEmailAddress(), email.ExtractEmailName()

                message.AddTos(toEmails);
            }
            else // we can't load the notification email addresses so send to the from TODO: Split the notification email into Global and Client and then send to global here
            {
                message.Subject = "CLIENT NOTIFICATION FAILED - " + message.Subject;   // Modify the subject to indicate client is not notified
                message.AddHeader("Priority", "Urgent"); // add high priority flag
                message.AddHeader("Importance", "high"); // add high importance flag for other email programs
                message.AddTo(FromEmail);
            }
            message.HtmlContent = HTMLBatchTemplate.ReplaceByDictionary(paramDict);
            try
            {
                return SendEmail(message);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error sending email to '{paramDict["Notification_Email_Address"]}' error was: {ex.Message}";
            }
            return false;
        }

        private string ReplaceValidation(string text, bool isValid)
        {
            string repText = string.Empty;
            if (isValid)
                repText = "The data file is has passed the validation checks.";
            else
                repText = "The data file has failed validation checks, please review the attached file for details.";

            return text.Replace("[ValidationString]", repText);

        }
    }
}
