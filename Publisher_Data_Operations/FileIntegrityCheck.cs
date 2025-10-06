using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Excel = Aspose.Cells;
using Publisher_Data_Operations.Helper;
using Publisher_Data_Operations.Extensions;

namespace Publisher_Data_Operations
{
    public class FileIntegrityCheck
    {
        //internal string Template = "";
        //internal PDIFileName FileValidation = null;
        private PDIStream ProcessStream = null;
        private PDIStream TemplateStream = null;
        internal ValidationList validationList = null;
        //private Excel.Workbook workbook = null;

        //public FileIntegrityCheck(PDIFile fileName, object conn, string template = null, Logger log = null) 
        //{
        //    ProcessStream = new PDIStream(fileName.FullPath, fileName);
        //    if (template.IsNaOrBlank())
        //        TemplateStream = new PDIStream(Path.Combine(Path.GetDirectoryName(fileName.FullPath), fileName.GetDefaultTemplateName()));
        //    else
        //    {
        //        FileAttributes attr = File.GetAttributes(template);
        //        if (attr.HasFlag(FileAttributes.Directory))
        //            TemplateStream = new PDIStream(Path.Combine(template, fileName.GetDefaultTemplateName()));
        //        else
        //            TemplateStream = new PDIStream(template);
        //    }

        //    Initialize(conn, log);
        //}

        public FileIntegrityCheck(PDIStream processStream, PDIStream templateStream, object conn, Logger log = null)
        {
            ProcessStream = processStream;
            TemplateStream = templateStream;

            //Set Aspose license
            Excel.License license = new Excel.License();
            license.SetLicense("Aspose.Total.lic");

            validationList = new ValidationList(new ExcelHelper(ProcessStream, conn, log));

            if (!ProcessStream.PdiFile.IsValid) //handle invalid file - these shouldn't be passed to the validator
                validationList.ExHelper.LogFileValidationIssue($"Unable to validate invalid file {ProcessStream.PdiFile.OnlyFileName} - {ProcessStream.PdiFile.ErrorMessage}");

            validationList.ExHelper.Log.WriteErrorsToDB();
        }
        /// <summary>
        /// Common initialization steps between the different class constructors
        /// </summary>
        /// <param name="conn">The database connection object</param>
        /// <param name="template">The path or path and filename of the template</param>
        //private void Initialize(object conn, Logger log = null)
        //{
        //    //Set Aspose license
        //    Excel.License license = new Excel.License();
        //    license.SetLicense("Aspose.Total.lic");

        //    validationList = new ValidationList(new ExcelHelper(ProcessStream, conn, log));

        //    if (ProcessStream.PdiFile.IsValid)
        //    {
                
        //    }
        //    else
        //    {
        //        //handle invalid file - these shouldn't be passed to the validator
        //        validationList.ExHelper.LogFileValidationIssue($"Unable to validate invalid file {ProcessStream.PdiFile.OnlyFileName} - {ProcessStream.PdiFile.ErrorMessage}");
        //    }
        //    validationList.ExHelper.Log.WriteErrorsToDB();
        //}


        //public PDIStream GetUpdatedStream()
        //{
        //    //MemoryStream toReturn = new MemoryStream();
        //    workbook.Save(ProcessStream.SourceStream, Excel.SaveFormat.Auto);
        //    return ProcessStream;
        //}

        /// <summary>
        /// Generates a default template file name based on the data and document types
        /// </summary>
        /// <returns>The generated file name</returns>
        //private string GetDefaultTemplateName()
        //{
        //    return "TEMPLATE" + FileName.FILE_DELIMITER + FileValidation.DataType + FileName.FILE_DELIMITER + FileValidation.DocType + ".xlsx";
        //}
        /// <summary>
        /// Perform structure and data check for worksheets and templates setup in validationList
        /// </summary>
        public bool FileCheck()
        {
            Excel.Workbook workbook = null;
            Excel.Workbook templateWorkbook= null;
            try
            {
                Excel.License license = new Excel.License();
                license.SetLicense("Aspose.Total.lic");
                Excel.LoadOptions opt = new Excel.LoadOptions();
                opt.MemorySetting = Excel.MemorySetting.MemoryPreference; // Load with memory optimizations on - about 1/4 memory use but slightly slower

                workbook = new Excel.Workbook(ProcessStream.SourceStream, opt);
                workbook.FileName = ProcessStream.PdiFile.OnlyFileName;
                templateWorkbook = new Excel.Workbook(TemplateStream.SourceStream, opt);
                templateWorkbook.FileName = TemplateStream.PdiFile.OnlyFileName;
            }
            catch (Exception e)
            {
                //TODO: this is a catastrophic error for the validator but for testing purposes we'll let it slide and return true
                //throw new FileNotFoundException("Unable to locate required template: " + Template + " - " + e.Message);
                
                validationList.ExHelper.UpdateFileStatusAndDcoumentCountToLogTable(ProcessStream.PdiFile.DataID.HasValue ? (int)ProcessStream.PdiFile.DataID : -1, validationList.TotalDocuments);
                if (workbook != null)
                    validationList.ExHelper.LogFileValidationIssue($"CRITICAL: Error loading Template file - {e.Message}");
                else
                    validationList.ExHelper.LogFileValidationIssue($"CRITICAL: Error loading Excel File(s) - {ProcessStream.PdiFile.OnlyFileName} - {e.Message}");
                validationList.ExHelper.Log.WriteErrorsToDB();
                return false;
            }

            if (TemplateStream.SourceStream.Length == 0)
            {
                validationList.ExHelper.UpdateFileStatusAndDcoumentCountToLogTable(ProcessStream.PdiFile.DataID.HasValue ? (int)ProcessStream.PdiFile.DataID : -1, validationList.TotalDocuments);
                validationList.ExHelper.LogFileValidationIssue("CRITICAL: Error loading Excel File(s) - template stream was empty");
                validationList.ExHelper.Log.WriteErrorsToDB();
                return true;
            }

            try
            {
                //workbook has macros
                if (workbook.HasMacro)
                    validationList.ExHelper.LogFileValidationIssue("2AI1: Macro exists in the workbook");
                
                validationList.ExHelper.CheckStaticUpdate(workbook);

                foreach (Excel.Worksheet templateSheet in templateWorkbook.Worksheets)
                {
                    Excel.Worksheet inputSheet = workbook.Worksheets[templateSheet.Name];

                    if (inputSheet != null)
                        validationList.AddWS(inputSheet, templateSheet);
                    else if (!templateSheet.IsOptionalWorksheet())
                        validationList.ExHelper.LogFileValidationIssue($"2AII1: \"{templateSheet.Name}\" sheet does not exist in {ProcessStream.PdiFile.OnlyFileName}");
                }

                // For additional sheets in Table UPDATE but not yet in the validationList add using the Static Text (or equivalent) sheet.
                // It is assumed that the LAST template worksheet in the above list is the one that should be duplicated for any sheet not in the template but in the submitted file
                if (validationList.ExHelper.WorksheetExist(workbook, "Table UPDATE"))
                {
                    Dictionary<string, bool> updateList = AsposeLoader.TableUpdate(workbook);
                    Excel.Worksheet templateSheet = validationList[validationList.Count-1].TemplateSheet; // Now uses the last template worksheet from the list of worksheets that exist -- updateList.ElementAt(1).Key Assumes the second item is the required one
                    foreach (KeyValuePair<string, bool> curKey in updateList)
                    {
                        if (curKey.Value && validationList.ExHelper.WorksheetExist(workbook, curKey.Key) && !validationList.ContainsSheetByName(curKey.Key))
                            validationList.AddWS(workbook.Worksheets[curKey.Key], templateSheet);      
                    }
                }


                validationList.Validate();
                validationList.ExHelper.UpdateFileStatusAndDcoumentCountToLogTable(ProcessStream.PdiFile.DataID.HasValue ? (int)ProcessStream.PdiFile.DataID : -1, validationList.TotalDocuments);
            }
            catch (Exception e)
            {
                validationList.ExHelper.LogFileValidationIssue("Execution Error During Validation: " + e.Message + Environment.NewLine + e.StackTrace);
                validationList.ExHelper.Log.WriteErrorsToDB();
                return false; // return false when the validation doesn't complete successfully
            }
            validationList.ExHelper.Log.WriteErrorsToDB();

            if (!validationList.ExHelper.IsValidData)
                workbook.Save(ProcessStream.SourceStream, Excel.SaveFormat.Auto); // save any errors back to the source stream

            workbook.Dispose();
            templateWorkbook.Dispose();

            return validationList.ExHelper.IsValidData;
            //MemoryStream memStream = new MemoryStream();
            //workbook.Save(memStream, Excel.SaveFormat.Auto); //@"C:\Users\Scott\source\Sample Files\Test_Comments.xlsx"
        }
    }

}