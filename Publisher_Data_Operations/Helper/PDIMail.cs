using Publisher_Data_Operations.Extensions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace Publisher_Data_Operations.Helper
{
    //https://stackoverflow.com/questions/6244694/send-smtp-email-using-system-net-mail-via-exchange-online-office-365
    public class PDIMail : IDisposable
    {
        private SmtpClient _client = null;
        public string FromEmail = null;
        public string FromName = null;
        public string HTMLTemplate = "<p><h2>Client Information</h2>Company Name: <b><Company_Name></b><br>Document Type Name: <Document_Type_Name></p><p><h2>File Received</h2>File Name: <File_Name><br>File Receipt time: <File_Receipt_Timestamp></p><p><h2>Validation Status:</h2>Validation Status: [ValidationString]<br>Record Count: <Number_of_Records></p><p><h2>Processing Status:</h2>Start time: <Job_Start><br>End time: <Import_End></p><p><h2>Publisher Data Load Status:</h2>Data load status: <b><Job_Status></b></p> ";
        public string HTMLErrorTemplate = "<p><h2>Client Information</h2>Company Name: <b><Company_Name></b><br>Document Type Name: <Document_Type_Name></p><p><h2>File Received</h2>File Name: <File_Name><br>File Receipt time: <File_Receipt_Timestamp></p><p><h2>Validation Status:</h2>Validation Status: <b>Error</b> see attachment<br>Record Count: <Number_of_Records></p><p><h2>Processing Status:</h2>Start time: <Job_Start><br>End time: <Import_End></p><p><h2>Publisher Data Load Status:</h2>Data load status: <b><Job_Status></b></p> ";
        private bool disposedValue;
        public string ErrorMessage {get; set;}


        public PDIMail(string username, string password, string fromEmail = null, string fromName = "Publisher Import", string host = "smtp.sendgrid.net", int port = 587)
        {
            FromEmail = fromEmail ?? username;
            FromName = fromName;
            _client = new SmtpClient()
            {
                Host = host,
                Port = port,
                UseDefaultCredentials = false, // This require to be before setting Credentials property
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(username, password), // you must give a full email address for authentication with O365 
                TargetName = "STARTTLS/" + host, // Set to avoid MustIssueStartTlsFirst exception
                EnableSsl = true // Set to avoid secure connection exception
            };
        }


        public bool SendTemplateMessage(PDIFile pdiFile, string subject = "Publisher Notification", string bcc = "konkle@gmail.com")
        {
            if (pdiFile.JobID.HasValue)
            {
                MailMessage message = new MailMessage()
                {
                    From = new MailAddress(FromEmail, FromName), // sender must be a full email address - pdi_support@investorcom.com
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = HTMLTemplate,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8,
                };

                message.Subject = message.Subject + " - " + pdiFile.OnlyFileName;
                
                Dictionary<string, string> paramDict = pdiFile.GetAllParameters();
                message.Body = ReplaceValidation(HTMLTemplate.ReplaceByDictionary(paramDict), true);

                if (paramDict.ContainsKey("Notification_Email_Address"))
                    message.To.Add(paramDict["Notification_Email_Address"]);
                message.Bcc.Add(bcc);
                try
                {
                    _client.Send(message);
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error sending email to '{paramDict["Notification_Email_Address"]}' error was: {ex.Message}";
                    return false;
                }
            }
            return false;
        }

        public bool SendTemplateMessage(PDIStream attach, string toAddress = null, string subject = "Publisher Notification", string bcc = "konkle@gmail.com")
        {
            MailMessage message = new MailMessage()
            {
                From = new MailAddress(FromEmail, FromName), // sender must be a full email address - pdi_support@investorcom.com
                Subject = subject,
                IsBodyHtml = true,
                Body = HTMLTemplate,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8,

            };
            if (attach != null && attach.SourceStream != null && attach.PdiFile != null && attach.SourceStream.Length > 0)
            {
                message.Subject = message.Subject + " - " + attach.PdiFile.OnlyFileName;
                bool isValid = attach.PdiFile.IsValidParameters();
                Dictionary<string, string> paramDict = attach.PdiFile.GetAllParameters();
                message.Body = ReplaceValidation(HTMLTemplate.ReplaceByDictionary(paramDict), isValid);
                if (!isValid)
                {
                    if (attach.SourceStream.CanSeek && attach.SourceStream.Position != 0)
                        attach.SourceStream.Position = 0;

                    message.Attachments.Add(new Attachment(attach.SourceStream, attach.PdiFile.OnlyFileName));
                    message.Attachments.Add(new Attachment(attach.PdiFile.GetValidationErrorsCSV(), attach.PdiFile.FileNameWithoutExtension + "_Validation_Errors.csv", "text/csv"));
                }

                if (toAddress != null && toAddress.Length > 0)
                    message.To.Add(toAddress);
                else if (paramDict != null && paramDict.ContainsKey("Notification_Email_Address"))
                    message.To.Add(paramDict["Notification_Email_Address"]);
                //message.Bcc.Add(bcc);

                if (message.To.Count < 1)
                    message.To.Add(FromEmail);
                try
                {
                    _client.Send(message);
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error sending email to '{message.To[0].Address}' error was: {ex.Message}";
                    return false;
                }
            }
            return false; 
        }

        public bool SendErrorMessage(PDIFile pdiFile, string toAddress = null, string subject = "Publisher Error Notification", string bcc = "konkle@gmail.com")
        {
            if (pdiFile != null && pdiFile.JobID.HasValue)
            {
                MailMessage message = new MailMessage()
                {
                    From = new MailAddress(FromEmail, FromName), // sender must be a full email address - pdi_support@investorcom.com
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = HTMLTemplate,
                    BodyEncoding = System.Text.Encoding.UTF8,
                    SubjectEncoding = System.Text.Encoding.UTF8,

                };

                message.Subject = message.Subject + " - " + pdiFile.OnlyFileName;

                Dictionary<string, string> paramDict = pdiFile.GetAllParameters();
                message.Body = ReplaceValidation(HTMLErrorTemplate.ReplaceByDictionary(paramDict), false);

                if (toAddress != null && toAddress.Length > 0)
                    message.To.Add(toAddress);
                else if (paramDict.ContainsKey("Notification_Email_Address"))
                    message.To.Add(paramDict["Notification_Email_Address"]);
                //message.Bcc.Add(bcc);
                message.Attachments.Add(new Attachment(pdiFile.GetValidationErrorsCSV(), pdiFile.FileNameWithoutExtension + "_Validation_Errors.csv", "text/csv"));
                try
                {
                    _client.Send(message);
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorMessage = $"Error sending email to '{paramDict["Notification_Email_Address"]}' error was: {ex.Message}";
                    return false;
                }
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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _client.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                HTMLTemplate = null;
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PDIMail()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
