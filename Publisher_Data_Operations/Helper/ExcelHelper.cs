using System;
using System.Collections.Generic;
using System.Linq;
using Excel = Aspose.Cells;
using Publisher_Data_Operations.Extensions;
using System.Data;

namespace Publisher_Data_Operations.Helper
{
    /// <summary>
    /// Validation rule numbers correspond to:
    /// 
    /// https://dev.azure.com/investorpos/ICOM%20DevOps/_workitems/edit/8590
    /// </summary>
    public class ExcelHelper
    {
        public bool IsValidData { get; private set; }
        private DBConnection dBConnection;  // use dbConnection.GetSQLConnection instead of maintaining both dbConnection and SQLconnection

        private int FileID = -1;    // keep track of FileID globally instead of passing it 

        public PDIStream ProcessStream { get; private set; }
        private ParameterValidator paramValidator;

        public Logger Log = null;

        public ExcelHelper(PDIStream fileStream, object conn, Logger log = null)
        {
            if (conn.GetType() == typeof(DBConnection))
                dBConnection = (DBConnection)conn;
            else
                dBConnection = new DBConnection(conn);

            ProcessStream = fileStream;
            FileID = (ProcessStream.PdiFile.FileID.HasValue) ? (int)ProcessStream.PdiFile.FileID : -1;
            IsValidData = true;

            Log = log;
            if (Log is null)
                Log = new Logger(conn, ProcessStream.PdiFile);

            if (ProcessStream.PdiFile.DocumentTypeID.HasValue)
                paramValidator = new ParameterValidator(loadValidParameters());
        }

        /// <summary>
        /// Check if worksheet exists in Workbook
        /// </summary>
        /// <param name="workbook">Workbook</param>
        /// <param name="WSName">worksheet name</param>
        /// <returns>boolean</returns>
        public bool WorksheetExist(Excel.Workbook workbook, String WSName)
        {
            Excel.Worksheet worksheet = workbook.Worksheets[WSName];

            if (worksheet == null)
                return false;

            return true;
        }

        /// <summary>
        /// Search for hidden row in given sheet
        /// </summary>
        /// <param name="worksheet">worksheet</param>
        /// <returns>boolean</returns>
        public void FindHiddenRowInWS(Excel.Worksheet worksheet)
        {
            Excel.Cells cells = worksheet.Cells;
            int maxRow = cells.MaxDataRow;
            for (int i = 0; i <= maxRow; i++)
            {
                if (cells.Rows[i].IsHidden)
                    LogFileValidationIssue("Row " + (i + 1) + " found hidden in " + worksheet.Name + " worksheet");
            }

        }

        /// <summary>
        /// Search for hidden column in given sheet
        /// </summary>
        /// <param name="worksheet">worksheet</param>
        /// <returns>boolean</returns>
        public void FindHiddenColumnInWS(Excel.Worksheet worksheet)
        {
            Excel.Cells cells = worksheet.Cells;
            int maxColumn = cells.MaxDataColumn;

            for (int i = 0; i <= maxColumn; i++)
            {
                if (cells.Columns[i].IsHidden)
                    LogFileValidationIssue("Column " + (i + 1) + " found hidden in " + worksheet.Name + " worksheet");
            }
        }

        /// <summary>
        /// Log hidden sheet, rows and columns
        /// </summary>
        /// <param name="worksheet">sheet</param>
        public void LogHiddenSheetRowsColumns(Excel.Worksheet worksheet)
        {
            if (!worksheet.IsVisible)
                LogFileValidationIssue("2AII4: " + worksheet.Name + "isn't visible.");

            Excel.Cells cells = worksheet.Cells;
            int maxRow = cells.MaxDataRow;
            int maxColumn = cells.MaxDataColumn;
            for (int i = 0; i <= maxRow; i++)
            {
                if (cells.Rows[i].IsHidden)
                    LogFileValidationIssue("2AII6: Row " + (i + 1) + " found hidden in " + worksheet.Name + " worksheet");
            }
            for (int i = 0; i <= maxColumn; i++)
            {
                if (cells.Columns[i].IsHidden)
                    LogFileValidationIssue("2AII6: Column " + (i + 1) + " found hidden in " + worksheet.Name + " worksheet");
            }
        }

        /// <summary>
        /// Separate check for existing auto filters - the error says active but it's really only checking that the filter exists
        /// Hidden row check will trip if the filter is actually on
        /// Configured as a template validation on sheets with the note: "ActiveFilter"
        /// </summary>
        /// <param name="worksheet">The Aspose Cells worksheet to check for filters</param>
        public void LogFilters(Excel.Worksheet worksheet)
        {
            if (worksheet.HasAutofilter)           
                LogFileValidationIssue("2AII5: Active filter found in " + worksheet.Name + " worksheet"); //var filters = worksheet.AutoFilter;

        }

        /// <summary>
        /// Log formulas to the table
        /// </summary>
        /// <param name="FileID">FileID</param>
        /// <param name="worksheet">sheet</param>
        public void LogFormulas(Excel.Worksheet worksheet)
        {
 
            Excel.Cells cells = worksheet.Cells;
            Excel.Cell cell = null;
            Excel.FindOptions opts = new Excel.FindOptions
            {
                LookInType = Excel.LookInType.OnlyFormulas,
                LookAtType = Excel.LookAtType.StartWith
            };

            do
            {
                cell = cells.Find("=", cell, opts);
                if (cell == null)
                    break;
                else
                    LogFileValidationIssue("2AI2: Formula found in " + worksheet.Name + " worksheet at " + cell.Name + " cell", cell);

            } while (true);
        }

        /// <summary>
        /// Identify and log non text type cells
        /// </summary>
        /// <param name="cellFormat">cell type</param>
        /// <param name="worksheet">sheet</param>
        public void CheckCellDataType(string cellFormat, Excel.Worksheet worksheet)
        {


            Excel.FindOptions options = new Excel.FindOptions();
            HashSet<string> uniqueCells = new HashSet<string>(); // since we'll be scanning the sheet multiple times keep track of failed cells
            HashSet<string> uniqueCulture = new HashSet<string>(); // only check each CustomCulture once

            int maxRow = worksheet.Cells.MaxDataRow;
            int maxColumn = worksheet.Cells.MaxColumn;

            for (int i = 0; i < worksheet.Workbook.CountOfStylesInPool; i++) // check all styles in the workbook
            {
                Excel.Style style = worksheet.Workbook.GetStyleInPool(i);
                if (style.CultureCustom != cellFormat && !uniqueCulture.Contains(style.CultureCustom)) // don't check the CultureCustom we are looking for or any we have already checked
                {
                    options.Style = style;  // set the seach options style
                    Excel.Cell nextCell = null; // nextCell for search results
                    uniqueCulture.Add(style.CultureCustom); // add the current style CultureCustom so we don't check it again (multiple null cultures) - 26 loops reduced to 8 for example

                    do
                    {
                        nextCell = worksheet.Cells.Find(null, nextCell, options);
                        if (nextCell == null)
                            break; // break out of the loop when we don't have any search results

                        if (nextCell.Row <= maxRow && nextCell.Column <= maxColumn && nextCell.GetStyle().CultureCustom != cellFormat) // double check that we have a valid fail as null culture has odd results - Added a check to not bother with cells outside of the max data range as we don't care if they are text or not
                            uniqueCells.Add(nextCell.Name); // record the fail just once

                    } while (true);

                }
            }

            foreach (string name in uniqueCells) // report all failed cells
                LogFileValidationIssue("2AII3: " + name + " cell format is not set to Text in " + worksheet.Name + " worksheet");


            //// For some reason the Find method above is 97% faster than the iteration method below - FP test file with 13000+ 10K rows goes from 138 seconds to validate to 3.5 !! what the heck?

            // Excel.Cell cell;
            // string cultureCustom;
            //Iterate through all the cells in the sheet.
            //for (int row = 0; row <= worksheet.Cells.MaxDataRow; row++)
            //{
            //    for (int col = 0; col <= worksheet.Cells.MaxDataColumn; col++)
            //    {
            //        cell = worksheet.Cells[row, col]; //Get the cell object
            //        if (cell.Type != Excel.CellValueType.IsNull)
            //        {
            //            cultureCustom = cell.GetStyle().CultureCustom; 
            //            if (cultureCustom is null || (!cultureCustom.Equals(cellFormat)))
            //                LogFileValidationIssue("2AII3: " + cell.Name + " cell format is not set to Text in " + worksheet.Name + " worksheet");
            //        }
            //    }
            //}


        }

        internal void CheckStaticUpdate(Excel.Workbook workbook)
        {
            if (WorksheetExist(workbook, "Table UPDATE"))
            {
                Dictionary<string, bool> updateList = AsposeLoader.TableUpdate(workbook);
                // similar to the extract

                foreach (KeyValuePair<string, bool> curKey in updateList)
                {
                    if (curKey.Value)
                    {
                        if (!WorksheetExist(workbook, curKey.Key))
                            LogFileValidationIssue($"STATIC: 'Table UPDATE' sheet indicates that '{curKey.Key}' should be updated but a corresponding sheet could not be found.");

                    }
                }
            }
        }

        /// <summary>
        /// Validate header using given template
        /// </summary>
        /// <param name="worksheet">worksheet</param>
        /// <param name="templateWS">template worksheet</param>
        public void ValidateWorkSheetHeader(WorksheetValidation wv)
        {
            for (int r = 0; r < wv.FirstDataRow; r++) 
            {
                for (int c = 0; c <= wv.TemplateSheet.Cells.MaxDataColumn; c++) 
                {
                    if (wv.InputSheet.Cells[r, c].StringValue != wv.TemplateSheet.Cells[r, c].StringValue)
                        LogFileValidationIssue($"2AII2A/B: {wv.InputSheet.Name} Worksheet does not match header cell {wv.InputSheet.Cells[r, c].Name} - Expected '{wv.TemplateSheet.Cells[r, c].StringValue}' but found '{wv.InputSheet.Cells[r, c].StringValue}'", wv.InputSheet.Cells[r, c]);
                }
            }
        }

        /// <summary>
        /// Check that the 16 17 and optionally 40 sheets contain data for fundcodes that are not IsProforma and don't for data that IsProforma
        /// </summary>
        /// <param name="validList">The ValidationList to check</param>
        internal void ValidateIsProforma(ValidationList validList)
        {
            WorksheetValidation wsDD = validList.GetSheetByType(SheetType.wsData);

            int documentCodeCol = FindColumnByName(wsDD, "DocumentCode");
            int fundCodeCol = FindColumnByName(wsDD, "FundCode");
            int isProformaCol = FindColumnByName(wsDD, "IsProforma");
            int filingIDCol = FindColumnByName(wsDD, "FilingReferenceID");

            Excel.FindOptions findOptions = new Excel.FindOptions
            {
                SeachOrderByRows = true,
                LookInType = Excel.LookInType.Values,
                LookAtType = Excel.LookAtType.EntireContent
            };

            for (int ddRow = wsDD.FirstDataRow; ddRow <= wsDD.InputSheet.Cells.MaxDataRow; ddRow++)
            {
                string docCode = wsDD.InputSheet.Cells[ddRow, documentCodeCol].StringValue.Trim();
                string fundCode = wsDD.InputSheet.Cells[ddRow, fundCodeCol].StringValue.Trim();
                bool isProforma = wsDD.InputSheet.Cells[ddRow, isProformaCol].StringValue.ToBool();
                string filingID = wsDD.InputSheet.Cells[ddRow, filingIDCol].StringValue.Trim();

                foreach (WorksheetValidation curWV in validList)
                {
                    if (curWV.TypeOfSheet != SheetType.wsData && curWV.TypeOfSheet != SheetType.wsUnknown)
                    {
                        int curWVFundCodeCol = FindColumnByName(curWV, "FundCode");

                        Excel.CellArea ca = new Excel.CellArea
                        {
                            StartRow = curWV.FirstDataRow,
                            StartColumn = curWVFundCodeCol,
                            EndRow = curWV.InputSheet.Cells.MaxDataRow,
                            EndColumn = curWVFundCodeCol
                        };

                        findOptions.SetRange(ca);
                        Excel.Cell cell = null;
                       
                        cell = curWV.InputSheet.Cells.Find(fundCode, cell, findOptions);
                        // As Per John Gilhuly 20220419 - client will be allowed to enter 16/17/40 data even for IsProforma and it will be ignored
                        //if (isProforma && cell != null)
                            //LogFileValidationIssue("2AVI3: FundCode " + fundCode + " on worksheet " + wsDD.InputSheet.Name + " at cell " + wsDD.InputSheet.Cells[ddRow, fundCodeCol].Name + " is Proforma and should not have data in " + curWV.InputSheet.Name + " worksheet", wsDD.InputSheet.Cells[ddRow, fundCodeCol]);

                        
                        if (!isProforma && cell is null && curWV.TypeOfSheet != SheetType.ws40)
                        {
                            // if the document is a brand new document then it's allowed to not have data on ws16 and/or ws17 regardless of isProforma setting
                            if (!IsBrandNewFund(docCode, fundCode, ProcessStream.PdiFile.DocumentTypeID, filingID, ProcessStream.PdiFile.ClientID))
                                LogFileValidationIssue("2AVI4: FundCode " + fundCode + " on worksheet " + wsDD.InputSheet.Name + " at cell " + wsDD.InputSheet.Cells[ddRow, fundCodeCol].Name + " should have data in " + curWV.InputSheet.Name + " worksheet", wsDD.InputSheet.Cells[ddRow, fundCodeCol]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Identify empty cell(s), NA and log the error message
        /// </summary>
        /// <param name="FileID">FileID</param>
        /// <param name="worksheet">worksheet</param>
        public void CheckBlanksAndNAInWorksheet(WorksheetValidation worksheet)
        {
            int lastColumn = worksheet.InputSheet.Cells.MaxDataColumn;
            int lastRow = worksheet.InputSheet.Cells.MaxDataRow;
            Excel.Cell cell;
            bool checkRow = false;
            for (int r = worksheet.FirstDataRow; r <= lastRow; r++)
            {
                checkRow = false;
                //if the entire row is blank - ignore - This avoids excessive error reporting
                for (int c = 0; c <= lastColumn; c++)
                {
                    if (worksheet.InputSheet.Cells[r, c].Type != Excel.CellValueType.IsNull)
                    {
                        checkRow = true;
                        break;
                    }
                }

                if (checkRow)
                {
                    for (int c = 0; c <= lastColumn; c++)
                    {
                        cell = worksheet.InputSheet.Cells[r, c];

                        if (cell.Type == Excel.CellValueType.IsNull)
                            LogFileValidationIssue("2AII7: Empty cell found at " + cell.Name + " in " + worksheet.InputSheet.Name + " worksheet", cell);
                        else if (cell.StringValue.Trim().ToUpper().Equals("NA")) // This seems like a really specific check?
                            LogFileValidationIssue("\"" + cell.StringValue + "\" value is not accepted at " + cell.Name + " in " + worksheet.InputSheet.Name + " worksheet", cell);
                    }
                }

            }
        }

        /// <summary>
        /// Identify & log unknown fund codes to main data sheet
        /// </summary>
        /// <param name="FileID">FileID</param>
        /// <param name="mainWorksheet">main data sheet</param>
        /// <param name="worksheet">16/17/40 sheets</param>
        public void MatchFundCodeToDocumentDataTab(WorksheetValidation mainWorksheet, WorksheetValidation worksheet)
        {
            Excel.Cell cell, cellLocated;
            List<string> fundCodeList = new List<string>();
            Excel.Cells cells = mainWorksheet.InputSheet.Cells;

            // Instantiate Excel.FindOptions Object
            Excel.FindOptions findOptions = new Excel.FindOptions()
            {
                SeachOrderByRows = true,
                LookInType = Excel.LookInType.Values, //Set the Excel.LookInType, you may specify, values, formulas, comments etc.
                LookAtType = Excel.LookAtType.EntireContent // Set the Excel.LookAtType, you may specify Match entire content, endswith, starwith etc.
            };
            int fundCol = FindColumnByName(mainWorksheet, "FundCode");
            // Create a Cells Area
            Excel.CellArea ca = new Excel.CellArea()
            {
                StartRow = mainWorksheet.FirstDataRow,
                StartColumn = fundCol,
                EndRow = cells.MaxDataRow,
                EndColumn = fundCol
            };
            findOptions.SetRange(ca);

            fundCol = FindColumnByName(worksheet, "FundCode");
            for (int x = worksheet.FirstDataRow; x <= worksheet.InputSheet.Cells.MaxDataRow; x++)
            {
                cell = worksheet.InputSheet.Cells[x, fundCol];
                if (cell.Type != Excel.CellValueType.IsNull && !fundCodeList.Contains(cell.StringValue))
                {
                    fundCodeList.Add(cell.StringValue);
                    cellLocated = cells.Find(cell.StringValue, null, findOptions); // Find the cell with value
                    if (cellLocated is null)
                        LogFileValidationIssue($"2AII8: \"{cell.StringValue}\" fund code listed in {worksheet.InputSheet.Name} not found in {mainWorksheet.InputSheet.Name} worksheet");
                }
            }
        }

        /// <summary>
        /// Validate the sequence of row numbers
        /// </summary>
        /// <param name="worksheet">sheet</param>
        public void ValidateRowNumbers(WorksheetValidation worksheet, int[] columns)
        {
            int counter = 1;
            Excel.Cell fundCodeCell, rowNumberCell;
            List<string> fundCodeList = new List<string>();

            string additionalCellValue = string.Empty;

            if (columns.Length == 2 || columns.Length == 3)
            {
                for (int r = worksheet.FirstDataRow; r <= worksheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    fundCodeCell = worksheet.InputSheet.Cells[r, columns[0]];
                    rowNumberCell = worksheet.InputSheet.Cells[r, columns[1]];

                    if (columns.Length == 3 && worksheet.InputSheet.Cells[r, columns[2]].Type != Excel.CellValueType.IsNull) // a third column in row sequence validation is only used in fund profile allocation tables
                        additionalCellValue = worksheet.InputSheet.Cells[r, columns[2]].StringValue.Trim();
                    else
                        additionalCellValue = string.Empty;

                    if (fundCodeCell.Type != Excel.CellValueType.IsNull && !fundCodeList.Contains(fundCodeCell.StringValue.Trim() + additionalCellValue))
                    {
                        fundCodeList.Add(fundCodeCell.StringValue + additionalCellValue);
                        counter = 1;
                    }

                    if (rowNumberCell.Type != Excel.CellValueType.IsNull && int.TryParse(rowNumberCell.StringValue, out int number))
                    {
                        if ((number != counter))
                            LogFileValidationIssue("2AVI1: Unexpected row number \"" + number + "\" found at " + rowNumberCell.Name + " cell in " + worksheet.InputSheet.Name + " worksheet", rowNumberCell);
                    }
                    else
                        LogFileValidationIssue("2AVI1: Parsing error on row number \"" + rowNumberCell.StringValue + "\" at row " + r + " in " + worksheet.InputSheet.Name + " worksheet", rowNumberCell);

                    counter++;
                }
            }
        }

        public void ValidateRowWithDateNumbers(WorksheetValidation worksheet, int[] columns)
        {
            int counter = 1;
            Excel.Cell fundCodeCell, rowNumberCell, dateValueCell;
            List<string> fundCodeList = new List<string>();

            string additionalCellValue = string.Empty;
            DateTime prevDate = DateTime.MinValue;
            if (columns.Length == 3)
            {
                for (int r = worksheet.FirstDataRow; r <= worksheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    fundCodeCell = worksheet.InputSheet.Cells[r, columns[0]];
                    rowNumberCell = worksheet.InputSheet.Cells[r, columns[1]];
                    dateValueCell = worksheet.InputSheet.Cells[r, columns[2]];

                    if (fundCodeCell.Type != Excel.CellValueType.IsNull && !fundCodeList.Contains(fundCodeCell.StringValue.Trim()))
                    {
                        fundCodeList.Add(fundCodeCell.StringValue);
                        counter = 1;
                        prevDate = DateTime.MinValue;
                    }

                    
                    if (rowNumberCell.Type != Excel.CellValueType.IsNull && int.TryParse(rowNumberCell.StringValue, out int number))
                    {
                        if ((number != counter))
                            LogFileValidationIssue("2AVI1: Unexpected row number \"" + number + "\" found at " + rowNumberCell.Name + " cell in " + worksheet.InputSheet.Name + " worksheet", rowNumberCell);
                    }
                    else
                        LogFileValidationIssue("2AVI1: Parsing error on row number \"" + rowNumberCell.StringValue + "\" at row " + r + " in " + worksheet.InputSheet.Name + " worksheet", rowNumberCell);

                    if (dateValueCell.Type != Excel.CellValueType.IsNull && DateTime.TryParseExact(dateValueCell.StringValue, "MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime resDate))
                    {
                        if (prevDate >= resDate)
                            LogFileValidationIssue("2AVI1C: Unexpected date value \"" + dateValueCell.StringValue + "\" found at " + dateValueCell.Name + " cell in " + worksheet.InputSheet.Name + " worksheet - Less than or equal to previous date value of \"" + prevDate.ToString("MM/yyyy") + "\"", dateValueCell);
                        prevDate = resDate;
                    }
                    else
                        LogFileValidationIssue("2AVI1B: Parsing error on date value \"" + dateValueCell.StringValue + "\" at row " + r + " in " + worksheet.InputSheet.Name + " worksheet", dateValueCell);

                    counter++;
                }
            }
        }

        /// <summary>
        /// Check indicated column values in the source sheet against matching column names in the check sheet
        /// all values present in the source must also exist in the check
        /// </summary>
        /// <param name="sourceWorksheet">the WorksheetValidation source</param>
        /// <param name="checkWorksheet">the WorksheetValidation to check against</param>
        /// <param name="columns">List of integer columns</param>
        public void MatchColumnValueToDocumentDataTab(WorksheetValidation sourceWorksheet, WorksheetValidation checkWorksheet, List<int> columns)
        {
            Excel.Cell cell;
            List<string> foundList = new List<string>();
            List<string> searchList = new List<string>();

            foreach (int col in columns) 
            {
                string findColName = FindColumnNameByInt(sourceWorksheet, col).Trim('<').Trim('>');
                int findCol = FindColumnByName(checkWorksheet, findColName);
                if (findCol >= 0)
                {
                    foundList.Clear();
                    searchList.Clear();

                    for (int x = sourceWorksheet.FirstDataRow; x <= sourceWorksheet.InputSheet.Cells.MaxDataRow; x++)
                    { 
                        cell = sourceWorksheet.InputSheet.Cells[x, col];
                        if (cell.Type != Excel.CellValueType.IsNull && !foundList.Contains(cell.StringValue.Trim()))
                            foundList.Add(cell.StringValue.Trim());
                    }

                    for (int x = checkWorksheet.FirstDataRow; x <= checkWorksheet.InputSheet.Cells.MaxDataRow; x++)
                    {
                        cell = checkWorksheet.InputSheet.Cells[x, findCol];
                        if (cell.Type != Excel.CellValueType.IsNull && !searchList.Contains(cell.StringValue.Trim()))
                            searchList.Add(cell.StringValue.Trim());
                    }

                    foreach (string foundVal in foundList)
                    {
                        if (!searchList.Contains(foundVal))
                            LogFileValidationIssue($"3C: Listed code {foundVal} in {sourceWorksheet.InputSheet.Name} sheet is not available in {checkWorksheet.InputSheet.Name} sheet");
                    }
                }
                else
                    LogFileValidationIssue($"3C: Could not find matching column for {findColName} from {sourceWorksheet.InputSheet.Name} sheet - column is not available in {checkWorksheet.InputSheet.Name} sheet");
            }
        }

        /// <summary>
        /// compare account name, document type and line of business to file name parameters at record level
        /// </summary>
        /// <param name="worksheet">main data sheet</param>
        public void ValidateAccountDocumentTypeAndLOB(WorksheetValidation worksheet)
        {
            PDIFile fileParameters = new PDIFile(worksheet.InputSheet.Workbook.FileName, null, true);
            Excel.Cell cell;
            int clientCol, docCol, lobCol = -1;
            Generic gen = new Generic(dBConnection, Log);

            clientCol = FindColumnByName(worksheet, "ClientCode");
            docCol = FindColumnByName(worksheet, "DocumentType");
            lobCol = FindColumnByName(worksheet, "LineOfBusiness");

            //check that the file parameters are available in the template
            if (!gen.CheckDocumentTemplates(fileParameters.ClientName, fileParameters.DocumentType, out bool active) || !active)
                LogFileValidationIssue($"2AIII1: Publisher Template {(!active ? "is not active" : "does not exist")} for '{fileParameters.ClientName}' with document type '{fileParameters.DocumentType}'");
                
               

            for (int z = worksheet.FirstDataRow; z <= worksheet.InputSheet.Cells.MaxDataRow; z++)
            {
                cell = worksheet.InputSheet.Cells.CheckCell(z, clientCol);
                if (cell != null && !cell.StringValue.ToUpper().Trim().Equals(fileParameters.ClientName))
                    LogFileValidationIssue("2AIII1: Unrecognized client account \"" + cell.StringValue + "\" found at " + cell.Name + " in " + worksheet.InputSheet.Name + " worksheet", cell);
                
                cell = worksheet.InputSheet.Cells.CheckCell(z, docCol);
                if (cell != null && !cell.StringValue.ToUpper().Trim().Equals(fileParameters.DocumentType))
                        LogFileValidationIssue("2AIII1: Unrecognized document type \"" + cell.StringValue + "\" found at " + cell.Name + " in " + worksheet.InputSheet.Name + " worksheet", cell);
              
                cell = worksheet.InputSheet.Cells.CheckCell(z, lobCol);
                //lob = fileParameters.LOB == "NLOB" ? "N/A" : fileParameters.LOB;
                if (cell != null && !cell.StringValue.ToUpper().Trim().Equals(fileParameters.LOB))
                        LogFileValidationIssue("2AIII1: Unrecognized line of business \"" + cell.StringValue + "\" found at " + cell.Name + " in " + worksheet.InputSheet.Name + " worksheet", cell);
            }
        }

        /// <summary>
        /// Identify if any unacceptable character found in given columns' cells
        /// </summary>
        /// <param name="FileID">FileID</param>
        /// <param name="worksheet">sheet</param>
        /// <param name="columnList">index of columns</param>
        /// <param name="elements">list of elements</param>
        public void CheckElementsInColumn(WorksheetValidation worksheet, int[] columnList, string[] elementList, ValidationType vt)
        {
            Excel.Cell cell;

            for (int x = worksheet.FirstDataRow; x <= worksheet.InputSheet.Cells.MaxDataRow; x++)
            {
                foreach (int col in columnList)
                {
                    cell = worksheet.InputSheet.Cells[x, col];

                    if (cell.Type != Excel.CellValueType.IsNull)
                    {
                        bool exists = cell.StringValue.ContainsWords(elementList);
                        if (vt == ValidationType.AcceptableElements && !exists)
                            LogFileValidationIssue("2AVI2: [" + string.Join(", ", elementList) + "] are required at " + cell.Name + " in " + worksheet.InputSheet.Name + " sheet", cell);
                       
                        else if (vt != ValidationType.AcceptableElements && exists)
                            LogFileValidationIssue("2AIII6: [" + string.Join(", ", elementList) + "] are not accepted at " + cell.Name + " in " + worksheet.InputSheet.Name + " sheet", cell);
                 
                    }
                }
            }
        }

        public void MerValidation(WorksheetValidation workSheet, int row, (Excel.Cell merPercentageCell, Excel.Cell terPercentageCell, Excel.Cell totalFundsPercentCell, Excel.Cell totalFundsAmountCell) cellMerTer, bool isSwitch = false)
        {

            List<DateTime> dates = new List<DateTime>();

            Excel.Cell filingDateCell = workSheet.InputSheet.Cells[row, FindColumnByName(workSheet, "FilingDate")];
            Excel.Cell merDateCell = workSheet.InputSheet.Cells[row, FindColumnByName(workSheet, "MerDate")];

            dates.Clear();
            dates.Add(filingDateCell.StringValue.ToDate(DateTime.MinValue));
            dates.Add(merDateCell.StringValue.ToDate(DateTime.MinValue));

            if ((DaysDiff(dates) >= 60 && !isSwitch) || isSwitch) // 3.g.1.i.merTimespan >= 60 && merIncTimespan >= 60
            {
                if (RequiredColumns(workSheet, row, new List<int> { cellMerTer.merPercentageCell.Column, cellMerTer.terPercentageCell.Column, cellMerTer.totalFundsPercentCell.Column, cellMerTer.totalFundsAmountCell.Column, filingDateCell.Column, merDateCell.Column }, "3G1I1A"))
                {
                    // don't perform additional checks if the required columns fail
                    if (double.TryParse(cellMerTer.merPercentageCell.StringValue, out double merPercentage) && double.TryParse(cellMerTer.terPercentageCell.StringValue, out double terPercentage) && double.TryParse(cellMerTer.totalFundsPercentCell.StringValue, out double totalFundExpensesPercentage))
                    {
                        if (Math.Round((merPercentage + terPercentage), 4) != totalFundExpensesPercentage) // Round to deal with floating point arithmetic issues
                            LogFileValidationIssue($"3G1I1B: Sum of {(isSwitch ? "Switch ":string.Empty)}Mer ({cellMerTer.merPercentageCell.Name}) and {(isSwitch ? "Switch " : string.Empty)}Ter ({cellMerTer.terPercentageCell.Name}) percentage does not match to total {(isSwitch ? "switch " : string.Empty)}fund expenses percentage ({cellMerTer.totalFundsPercentCell.Name}) in {workSheet.InputSheet.Name} sheet - found {totalFundExpensesPercentage} but expected {Math.Round((merPercentage + terPercentage), 4)}", cellMerTer.totalFundsPercentCell);

                        if (double.TryParse(cellMerTer.totalFundsAmountCell.StringValue, out double totalFundExpensesDollarAmount))
                        {

                            if (Math.Round((totalFundExpensesPercentage * 10), 4) != totalFundExpensesDollarAmount) // the percentage values are shown as percents (2.43%) and need to be divided by 100 to be decimal - adjusted calculation to be (percent / 100) * 1000 (1000 is the example amount) - then switched to equivalent  percent * 10 - added Round to 4 decimals to deal with floating point math
                                LogFileValidationIssue($"3G1I1C: Sum of {(isSwitch ? "Switch " : string.Empty)}Mer ({cellMerTer.merPercentageCell.Name}) and {(isSwitch ? "Switch " : string.Empty)}Ter ({cellMerTer.terPercentageCell.Name}) percentage does not match to total {(isSwitch ? "switch " : string.Empty)}fund expenses dollar amount ({cellMerTer.totalFundsAmountCell.Name}) in {workSheet.InputSheet.Name} sheet - found {totalFundExpensesDollarAmount} but expected {Math.Round((totalFundExpensesPercentage * 10), 4)}", cellMerTer.totalFundsAmountCell);
                        }
                        else
                            LogFileValidationIssue($"3G1I1C: Unable to parse {cellMerTer.totalFundsAmountCell.Name} in {workSheet.InputSheet.Name} sheet", cellMerTer.totalFundsAmountCell);
                    }
                    else
                        LogFileValidationIssue($"3G1I: Unable to parse one or more cells ({cellMerTer.merPercentageCell.Name}, {cellMerTer.terPercentageCell.Name}, {cellMerTer.totalFundsPercentCell.Name}) in {workSheet.InputSheet.Name} sheet", cellMerTer.terPercentageCell);
                }
            }
            else
                RequiredNAColumns(workSheet, row, new List<int> { cellMerTer.merPercentageCell.Column, cellMerTer.terPercentageCell.Column, cellMerTer.totalFundsPercentCell.Column, cellMerTer.totalFundsAmountCell.Column }, "3G1I2A");
        }

        /// <summary>
        /// Very specific validation for MER and associated values
        /// </summary>
        /// <param name="workSheet">WorksheetValidation object</param>
        public void MerValidation(WorksheetValidation workSheet)
        {
            // this validator needs to be modified to not check if the IsProforma flag is set - this means if the IsProforma flag is part of the columnlist the column should be removed from the checkColumns and the 
            int proformaColumn = FindColumnByName(workSheet, "IsProforma");
            
            for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
            {
                if (proformaColumn > 0 && workSheet.InputSheet.Cells[r, proformaColumn].StringValue.ToBool())
                    continue; // when the proformaColumn is found and IsProforma is true skip this check

                int curCol = FindColumnByName(workSheet, "MerDate");
                if (curCol >= 0)
                {
                    Excel.Cell curCell = workSheet.InputSheet.Cells[r, curCol];
                    if (curCell.Type != Excel.CellValueType.IsNull && curCell.StringValue.IsNaOrBlank())
                        RequiredColumns(workSheet, r, FindColumnsByName(workSheet, "FeePercent"), "3G2I");
                    else
                    {
                        //TODO: this will cause an exception if there is a missing header
                        MerValidation(workSheet, r, (workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "MerPercent")], workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "TerPercent")], workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "TotalFundExpensePercent")], workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "TotalFundExpenseAmount")]));

                        if (workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "<Switching>")].StringValue.ToBool()) // validate the switch mer ter values when switch flag is true - need to use <switching> to get the right flag column
                            MerValidation(workSheet, r, (workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "SwitchMerPercent")], workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "SwitchTerPercent")], workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "TotalSwitchFundExpensePercent")], workSheet.InputSheet.Cells[r, FindColumnByName(workSheet, "TotalSwitchFundExpenseAmount")]), true);
                    }
                }
            }
        }

        /// <summary>
        /// Prepare a list of unique fund codes from reference sheet and validate total fund and investments' cells in main data sheet
        /// </summary>
        /// <param name="FileID">FileID</param>
        /// <param name="mainWorksheet">main data sheet</param>
        /// <param name="referenceWorksheet">reference sheet</param>
        /// <param name="startingRow">starting row index</param>
        /// <param name="columnList">column list</param>
        public void RequiredFundValidation(WorksheetValidation mainWorkSheet, WorksheetValidation referenceWorksheet, ValidationType vt)
        {
            List<string> fundCodes = new List<string>();
            Excel.Cell cell = null, curCell;
            Excel.Cells cells;
            int counter = 0;
            int[] columnList = LoadValidationFields(mainWorkSheet.TemplateSheet, Enum.GetName(typeof(ValidationType), vt)).ToArray();
            if (columnList.Length < 1)
                return;

            // this validator needs to be modified to not check if the IsProforma flag is set - this means if the IsProforma flag is part of the columnlist the column should be removed from the checkColumns and the 
            int proformaColumn = FindColumnByName(mainWorkSheet, "IsProforma");
            
            int fundCol = FindColumnByName(referenceWorksheet, "FundCode");
            //capture unique fundcodes 
            for (int r = referenceWorksheet.FirstDataRow; r <= referenceWorksheet.InputSheet.Cells.MaxDataRow; r++)
            {
                cell = referenceWorksheet.InputSheet.Cells[r, fundCol];
                if (cell.Type != Excel.CellValueType.IsNull && !fundCodes.Contains(cell.StringValue.Trim()))
                    fundCodes.Add(cell.StringValue.Trim());
            }

            cells = mainWorkSheet.InputSheet.Cells;

            // Instantiate Excel.FindOptions Object
            Excel.FindOptions findOptions = new Excel.FindOptions() 
            {
                SeachOrderByRows = true,
                LookInType = Excel.LookInType.Values, // Set the Excel.LookInType, you may specify, values, formulas, comments etc.
                LookAtType = Excel.LookAtType.EntireContent // Set the Excel.LookAtType, you may specify Match entire content, endswith, starwith etc.
            };

            fundCol = FindColumnByName(mainWorkSheet, "FundCode");
            // Create a Cells Area
            Excel.CellArea ca = new Excel.CellArea()
            {
                StartRow = mainWorkSheet.FirstDataRow,
                StartColumn = fundCol,
                EndRow = cells.MaxDataRow,
                EndColumn = fundCol
            };
            findOptions.SetRange(ca); // Set cells area for find options
           
            cell = null;
            if (fundCodes.Count > 0) 
            { 
                do
                {
                    cell = cells.Find(fundCodes[counter], cell, findOptions);

                    if (cell != null)
                    {
                        if (proformaColumn > 0 && mainWorkSheet.InputSheet.Cells[cell.Row, proformaColumn].StringValue.ToBool())
                            continue; // when the proformaColumn is found and IsProforma is true skip this check

                        for (int i = 0; i < columnList.Count(); i++)
                        {
                            curCell = mainWorkSheet.InputSheet.Cells[cell.Row, columnList[i]];
                            if (curCell.Type != Excel.CellValueType.IsNull && curCell.StringValue.IsNaOrBlank())
                                LogFileValidationIssue("3A1: " + curCell.StringValue + " is not allowed at " + curCell.Name + " in " + mainWorkSheet.InputSheet.Name + " sheet when FundCode used on " + referenceWorksheet.InputSheet.Name + " sheet", curCell);
                        }
                    }
                    else if (cell == null && counter == (fundCodes.Count - 1))
                        break;
                    else
                    {
                        counter++;
                        cell = null;
                    }

                } while (true);
            }
        }

        /// <summary>
        /// will match unique fund codes of first sheet to the second and report missing ones
        /// </summary>
        /// <param name="FileID">FileID</param>
        /// <param name="startingRow">starting row index</param>
        /// <param name="firstWorksheet">first worksheet object</param>
        /// <param name="secondWorksheet">second worksheet object</param>
        public void MatchFundCodesOfTwoSheets(WorksheetValidation firstWorksheet, WorksheetValidation secondWorksheet)
        {
            Excel.Cell cell;
            List<string> fundCodeList1 = new List<string>();
            List<string> fundCodeList2 = new List<string>();

            int fundCol = FindColumnByName(firstWorksheet, "FundCode");

            for (int x = firstWorksheet.FirstDataRow; x <= firstWorksheet.InputSheet.Cells.MaxDataRow; x++)
            {
                cell = firstWorksheet.InputSheet.Cells[x, fundCol];
                if (cell.Type != Excel.CellValueType.IsNull && !fundCodeList1.Contains(cell.StringValue.Trim()))
                    fundCodeList1.Add(cell.StringValue.Trim());
            }

            fundCol = FindColumnByName(secondWorksheet, "FundCode");
            for (int x = secondWorksheet.FirstDataRow; x <= secondWorksheet.InputSheet.Cells.MaxDataRow; x++)
            {
                cell = secondWorksheet.InputSheet.Cells[x, fundCol];
                if (cell.Type != Excel.CellValueType.IsNull && !fundCodeList2.Contains(cell.StringValue.Trim()))
                    fundCodeList2.Add(cell.StringValue.Trim());
            }

            foreach (string fundCode in fundCodeList1)
            {
                if (!fundCodeList2.Contains(fundCode))
                    LogFileValidationIssue("3C: Listed fund code " + fundCode + " in " + firstWorksheet.InputSheet.Name + " sheet is not available in " + secondWorksheet.InputSheet.Name + " sheet");
            }

        }

        /// <summary>
        /// Given a column number lookup the column name using the row before the FirstDataRow
        /// </summary>
        /// <param name="workSheet">The WorkseheetValidation sheet to check</param>
        /// <param name="column">the integer column to find the column name for</param>
        /// <returns></returns>
        internal string FindColumnNameByInt(WorksheetValidation workSheet, int column)
        {
            return workSheet.InputSheet.Cells[workSheet.FirstDataRow - 1, column].Value.ToString();
        }

        /// <summary>
        /// Helper to search a single string for the first result
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnName">The search text</param>
        /// <param name="lookAt">Optional LookAtType</param>
        /// <returns>First found column index or null</returns>
        internal int FindColumnByName(WorksheetValidation workSheet, string columnName, Excel.LookAtType lookAt = Excel.LookAtType.Contains)
        {
            return FindColumnsByName(workSheet, new string[] { columnName }, lookAt).FirstOrDefault();
        }

        /// <summary>
        /// Helper to search a single string and return all matching columns
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnNames">The array of search text</param>
        /// <param name="lookAt">Optional LookAtType</param>
        /// <returns>List of matching column indexes</returns>
        internal List<int> FindColumnsByName(WorksheetValidation workSheet, string columnName, Excel.LookAtType lookAt = Excel.LookAtType.Contains)
        {
            return FindColumnsByName(workSheet, new string[] { columnName }, lookAt);
        }

        /// <summary>
        /// Finds the all instances of a column that has text matching the columnName array using the default or supplied lookAt - searches template
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnNames">The array of search text</param>
        /// <param name="lookAt">Optional LookAtType</param>
        /// <returns>List of matching column indexes</returns>
        internal List<int> FindColumnsByName(WorksheetValidation workSheet, string[] columnNames, Excel.LookAtType lookAt = Excel.LookAtType.Contains)
        {
            Excel.Cell cell = null;
            List<int> foundColumns = new List<int>();

            Excel.FindOptions findOptions = new Excel.FindOptions
            {
                SeachOrderByRows = true,
                LookInType = Excel.LookInType.Values,
                LookAtType = lookAt
            };

            Excel.CellArea ca = new Excel.CellArea
            {
                StartRow = 0,
                StartColumn = 0,
                EndRow = workSheet.FirstDataRow - 1,
                EndColumn = workSheet.TemplateSheet.Cells.MaxDataColumn
            };

            findOptions.SetRange(ca);

            for (int i = 0; i < columnNames.Length; i++)
            {
                do
                {
                    cell = workSheet.TemplateSheet.Cells.Find(columnNames[i], cell, findOptions);
                    if (cell is null)
                        break;

                    foundColumns.Add(cell.Column);
                } while (cell != null);
            }
            return foundColumns;

        }

        /// <summary>
        /// Helper function to Find the first occurrence of a value in a list of column indexes
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnList">List of column indexes</param>
        /// <param name="findValue">The string value to search for</param>
        /// <param name="lookAt">Optional LookAtType</param>
        /// <returns>The found column index or null</returns>
        internal int FindColumnInList(WorksheetValidation workSheet, List<int> columnList, string findValue, Excel.LookAtType lookAt = Excel.LookAtType.Contains)
        {
            return FindColumnsInList(workSheet, columnList, findValue, lookAt).FirstOrDefault();
        }

        /// <summary>
        /// Helper function to Find all occurrence of a value in a list of column indexes
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnList">List of column indexes</param>
        /// <param name="findValue">The string value to search for</param>
        /// <param name="lookAt">Optional LookAtType</param>
        /// <returns>The found column index or null</returns>
        internal List<int> FindColumnsInList(WorksheetValidation workSheet, List<int> columnList, string findValue, Excel.LookAtType lookAt = Excel.LookAtType.Contains)
        {
            return FindColumnsInList(workSheet, columnList, new string[] { findValue }, lookAt);
        }

        /// <summary>
        /// Search a list of column indexes for a list of values and return all of the found column indexes
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnList">List of column indexes</param>
        /// <param name="findValues">The array of strings to search for</param>
        /// <param name="lookAt">Optional LookAtType</param>
        /// <returns>The found column index or null</returns>
        internal List<int> FindColumnsInList(WorksheetValidation workSheet, List<int> columnList, string[] findValues, Excel.LookAtType lookAt = Excel.LookAtType.Contains)
        {

            Excel.Cell cell = null;
            List<int> foundList = new List<int>();

            // Instantiate Excel.FindOptions Object
            Excel.FindOptions findOptions = new Excel.FindOptions
            {
                SeachOrderByRows = true,
                LookInType = Excel.LookInType.Values,
                LookAtType = lookAt
            };
            // Create a Cells Area
            Excel.CellArea ca = new Excel.CellArea();

            foreach (int i in columnList)
            {
                ca.StartRow = 0;
                ca.StartColumn = i;
                ca.EndRow = workSheet.FirstDataRow - 1;
                ca.EndColumn = i;

                findOptions.SetRange(ca);

                for (int x = 0; x < findValues.Length; x++)
                {
                    cell = workSheet.TemplateSheet.Cells.Find(findValues[x], cell, findOptions);
                    if (cell != null)
                        foundList.Add(cell.Column);
                }
            }
            return foundList;
        }

        /// <summary>
        /// This version of FindcolumnsInList uses a dictionary and returns the column numbers as part of the passed dictionary so that the order of the columns is maintained
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnList">List of column indexes</param>
        /// <param name="findValues">The dictionary of columns to search for and return</param>
        /// <param name="lookAt"></param>
        internal void FindColumnsInList(WorksheetValidation workSheet, List<int> columnList, Dictionary<string, int> findValues, Excel.LookAtType lookAt = Excel.LookAtType.Contains)
        {

            Excel.Cell cell = null;

            // Instantiate Excel.FindOptions Object
            Excel.FindOptions findOptions = new Excel.FindOptions
            {
                SeachOrderByRows = true,
                LookInType = Excel.LookInType.Values,
                LookAtType = lookAt
            };
            // Create a Cells Area
            Excel.CellArea ca = new Excel.CellArea();

            foreach (int i in columnList) // would be nice to setup consecutive ranges as one search area but this is still fast enough
            {
                ca.StartRow = 0;
                ca.StartColumn = i;
                ca.EndRow = workSheet.FirstDataRow - 1;
                ca.EndColumn = i;

                findOptions.SetRange(ca);

                foreach (KeyValuePair<string, int> kvp in findValues)
                {
                    cell = workSheet.TemplateSheet.Cells.Find(kvp.Key, cell, findOptions);
                    if (cell != null)
                    {
                        findValues[kvp.Key] = cell.Column;
                        break;
                    }   
                }
            }
        }

        /// <summary>
        /// Handles validation type "FlagValueValidation" - If the column matching the flagValue calumn is true then the other columns in the list must not be N/A otherwise the must be N/A
        /// either N/A or not
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnList">List of column indexes to use for validation</param>
        /// <param name="flagValue">The optional string indicator of a "Flag" column</param>
        internal void ValidateFlagRow(WorksheetValidation workSheet, List<int> columnList, string flagValue = "Flag")
        {
            string cellValue;
            bool flag = false;
            int flagColumn = FindColumnInList(workSheet, columnList, flagValue);
            if (flagColumn >= 0)
            {
                columnList.Remove(flagColumn);
                for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    flag = workSheet.InputSheet.Cells[r, flagColumn].StringValue.ToBool();
                    foreach (int c in columnList)
                    {
                        cellValue = workSheet.InputSheet.Cells[r, c].StringValue;
                        if (flag && cellValue.IsNaOrBlank())
                            LogFileValidationIssue("3H1I: " + cellValue + " is not allowed at " + workSheet.InputSheet.Cells[r, c].Name + " in " + workSheet.InputSheet.Name + " sheet when " + workSheet.InputSheet.Cells[r, flagColumn].Name + " is " + flag.ToString(), workSheet.InputSheet.Cells[r, c]);
                        else if (!flag && !cellValue.IsNaOrBlank())
                            LogFileValidationIssue("3H2I: " + cellValue + " is not allowed at " + workSheet.InputSheet.Cells[r, c].Name + " in " + workSheet.InputSheet.Name + " sheet when " + workSheet.InputSheet.Cells[r, flagColumn].Name + " is " + flag.ToString(), workSheet.InputSheet.Cells[r, c]);
                    }
                }
            }
        }

        /// <summary>
        /// Handles validation type "AtLeastOneRequired" - at least one value in the provided columnList must not be N/A
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnList">List of column indexes to use for validation</param>
        internal void ValidateOneRequired(WorksheetValidation workSheet, List<int> columnList)
        {
            bool foundVal = false;
            List<Excel.Cell> missingList = new List<Excel.Cell>();
            for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
            {
                foundVal = false;
                missingList.Clear();
                foreach (int c in columnList)
                {
                    if (!workSheet.InputSheet.Cells[r, c].StringValue.IsNaOrBlank())
                    {
                        foundVal = true;
                        break;
                    }
                    else
                        missingList.Add(workSheet.InputSheet.Cells[r, c]);
                }
                if (!foundVal)
                    LogFileValidationIssue("3D: At least one of [" + String.Join(", ", missingList.Select(x => x.Name)) + "] is required to have a value in " + workSheet.InputSheet.Name + " sheet ", missingList);
            }
        }

        /// <summary>
        /// Handles validation type "AllOrNone" - at least one value in the provided columnList must not be N/A
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnList">List of column indexes to use for validation</param>
        internal void ValidateAllOrNone(WorksheetValidation workSheet, List<int> columnList)
        {
            bool foundVal = false;
            bool naVal = false;
            List<Excel.Cell> checkList = new List<Excel.Cell>();
            for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
            {
                foundVal = false;
                naVal = false;
                checkList.Clear();
                foreach (int c in columnList)
                {
                    checkList.Add(workSheet.InputSheet.Cells[r, c]);
                    if (workSheet.InputSheet.Cells[r, c].StringValue.IsNaOrBlank())
                        naVal = true;
                    else
                        foundVal = true;
                }
                if (foundVal && naVal)
                    LogFileValidationIssue($"3DII: Values are required to be either all N/A or all not N/A in [{String.Join(", ", checkList.Select(x => x.Name))}] of {workSheet.InputSheet.Name} sheet ", checkList);
            }
        }

        /// <summary>
        /// Determines if there is at least 1 year between the supplied dates
        /// </summary>
        /// <param name="dates">List of DateTime values</param>
        /// <returns></returns>
        internal bool TwelveMonthsDiff(List<DateTime> dates)
        {
            // we are making an assumption here that the last date is the maxdate and the mindate is the first value - IE it will either be the inception date if not N/A or the First offering date; - now also may be the PerformanceResetDate
            if (dates.Count < 2)
                return false;

            return dates[dates.Count - 1].ConsecutiveYears(dates[0], 1);

        }

        internal int AgeInCalendarYears(List<DateTime> dates)
        {
            // we are making an assumption here that the last date is the maxdate and the mindate is the first value - IE it will either be the inception date if not N/A or the First offering date; - now also may be the PerformanceResetDate
            if (dates.Count < 2)
                return -1;

            return dates[dates.Count - 1].AgeInCalendarYears(dates[0]);
        }
        /// <summary>
        /// Given a list of dates returns the number of days between the dates
        /// </summary>
        /// <param name="dates">List of DateTime values</param>
        /// <returns>Number of fractional days</returns>
        internal double DaysDiff(List<DateTime> dates)
        {
            // we are making an assumption here that the last date is the maxdate and the mindate is the first value - IE it will either be the inception date if not N/A or the First offering date;
            if (dates.Count < 2)
                return -1;

            return dates[dates.Count - 1].DaysDiff(dates[0]);
        }

        /// <summary>
        /// Given a list of dates returns the number of years covered
        /// </summary>
        /// <param name="dates">List of DateTime values</param>
        /// <returns>Number of years between the dates in the list</returns>
        internal int YearsDiff(List<DateTime> dates)
        {
            return (int)Math.Floor(DaysDiff(dates) / Generic.DAYSPERYEAR);
        }

        //internal int CalendarYearsDiff(List<DateTime> dates)
        //{
        //    // we are making an assumption here that the last date is the maxdate and the mindate is the first value - IE the first value will either be the inception date if not N/A or the First offering date and the last value will be the AsAtDate;
        //    if (dates.Count < 2) // if we don't have at least 2 values then we don't have enough valid dates to do the CalendarYear calculation
        //        return -1; 
        //    return dates[dates.Count - 1].AgeInCalendarYears(dates[0]);
        //}


        /// <summary>
        /// Handles validation type "TwelveMonthValidation" - list contains date columns and if the date range spans a year or more the other columns in the list are required
        /// </summary>
        /// <param name="workSheet">>WorksheetValidation Object</param>
        /// <param name="columnList">List of column indexes to use for validation</param>
        internal void Validate12Months(WorksheetValidation workSheet, List<int> columnList)
        {
            Dictionary<string, int> dateColumns = new Dictionary<string, int>(5) { { "PerformanceResetDate", -1 }, { "InceptionDate", -1 }, {"FirstOfferingDate", -1 }, { "FilingDate", -1 }, {"DataAsAtDate", -1 } };
            
            List<int> avgReturnColumns = new List<int>(2);
            List<DateTime> dates = new List<DateTime>(3);
            List<int> checkColumns = new List<int>(columnList);

            // Using the performance reset date in place of Inception/Filing Date is now a requirement 20210629
            // it's not necessary to check PerformanceResetFlag as it's checked in another validation rule to make sure that it's N/A when the flag isn't checked
            // Need to add PerformanceResetDate to template for "TwelveMonthValidation"
            FindColumnsInList(workSheet, checkColumns, dateColumns );
            foreach(KeyValuePair<string, int> kvp in dateColumns)
                checkColumns.Remove(kvp.Value);
            // this validator needs to be modified to not check if the IsProforma flag is set - this means if the IsProforma flag is part of the columnlist the column should be removed from the checkColumns and the 
            int proformaColumn = FindColumnByName(workSheet, "IsProforma");
            checkColumns.Remove(proformaColumn); // this is in case it's marked Y in the template - but it isn't required to be so this will usually return false
    
            if (dateColumns.Count > 1)
            {
                for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    if (proformaColumn > 0 && workSheet.InputSheet.Cells[r, proformaColumn].StringValue.ToBool())
                        continue; // when the proformaColumn is found and IsProforma is true skip this check

                    dates.Clear();
                    foreach (KeyValuePair<string, int> kvp in dateColumns)
                    {
                        if (kvp.Value >= 0 && !workSheet.InputSheet.Cells[r, kvp.Value].StringValue.IsNaOrBlank())
                            dates.Add(workSheet.InputSheet.Cells[r, kvp.Value].StringValue.ToDate(DateTime.MinValue));
                    }
                    bool twelveMonths = TwelveMonthsDiff(dates);
                    foreach (int c in checkColumns)
                    {
                        if (twelveMonths && workSheet.InputSheet.Cells[r, c].StringValue.IsNaOrBlank())
                            LogFileValidationIssue("3EI1: " + workSheet.InputSheet.Cells[r, c].StringValue + " is not allowed when fund is greater than twelve consecutive months at " + workSheet.InputSheet.Cells[r, c].Name + " in " + workSheet.InputSheet.Name + " sheet", workSheet.InputSheet.Cells[r, c]);
                        else if (!twelveMonths && !workSheet.InputSheet.Cells[r, c].StringValue.IsNaOrBlank()) 
                            LogFileValidationIssue("3EII1: " + workSheet.InputSheet.Cells[r, c].StringValue + " must be N/A when fund is less than twelve consecutive months at " + workSheet.InputSheet.Cells[r, c].Name + " in " + workSheet.InputSheet.Name + " sheet", workSheet.InputSheet.Cells[r, c]);
                    }
                }
            }
        }

        /// <summary>
        /// Handles validation type "CalendarYearValidation" - If there is table data check if the number of 22b table columns matches the number of years in the date range (max 10) and check other required columns. 
        /// If there is no 22b data then check the alternate range of columns - searches InputSheet - MATCHES SPECIFIC COLUMN NAMING CONVENTIONS
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="columnList">The List of column indexes</param>
        internal void ValidateCalendarYears(WorksheetValidation workSheet, List<int> columnList)
        {
            List<DateTime> dates = new List<DateTime>(3);
            List<int> checkColumns = new List<int>(columnList);
            //DateTime inceptionDate = DateTime.MinValue;
            Dictionary<string, int> dateColumns = new Dictionary<string, int>(5) { { "PerformanceResetDate", -1 }, { "InceptionDate", -1 }, { "FirstOfferingDate", -1 }, { "FilingDate", -1 }, { "DataAsAtDate", -1 } };
            FindColumnsInList(workSheet, columnList, dateColumns); // 20200907 - Modifying to handle PerformanceReset and maintaining order using dictionary

            List<int> tableColumns = FindColumnsInList(workSheet, columnList, "year minus");
            int yearMinusOne = FindColumnInList(workSheet, tableColumns, "year minus 1 ");

            List<int> bestWorstReturns = FindColumnsInList(workSheet, checkColumns, "BestReturn").Concat(FindColumnsInList(workSheet, checkColumns, "WorstReturn")).ToList();

            List<Excel.Cell> cells = new List<Excel.Cell>(10);

            // this validator needs to be modified to not check if the IsProforma flag is set - this means if the IsProforma flag is part of the columnlist the column should be removed from the checkColumns and the 
            int proformaColumn = FindColumnByName(workSheet, "IsProforma");
            checkColumns.Remove(proformaColumn); // this is in case it's marked Y in the template - but it isn't required to be so this will usually return false

            if (dateColumns.Count > 1)
            {
                for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    if (proformaColumn > 0 && workSheet.InputSheet.Cells[r, proformaColumn].StringValue.ToBool())
                        continue; // when the proformaColumn is found and IsProforma is true skip this check

                    cells.Clear();
                    int count = 0;

                    dates.Clear();
                    foreach (KeyValuePair<string, int> kvp in dateColumns)
                    {
                        if (kvp.Value >= 0 && !workSheet.InputSheet.Cells[r, kvp.Value].StringValue.IsNaOrBlank())
                            dates.Add(workSheet.InputSheet.Cells[r, kvp.Value].StringValue.ToDate(DateTime.MinValue));
                    }

                    int calYearsDiff = AgeInCalendarYears(dates); // determines which dates to send to the string extension AgeInCalendarYears

                    if (calYearsDiff < 0) // there is an error with the available date columns and the validation can't be performed
                        continue;
                    else if (calYearsDiff >= 1) // 3.f.i.
                    {
                        if (yearMinusOne >= 0) //this just checks that we found the year minus one column 
                        {
                            if (workSheet.InputSheet.Cells[r, yearMinusOne].Type != Excel.CellValueType.IsNull && !workSheet.InputSheet.Cells[r, yearMinusOne].StringValue.IsNaOrBlank()) // 3.f.1.i. Columns Q-Z should have at least 1 non N/A value
                            {
                                foreach (int c in tableColumns)
                                {
                                    cells.Add(workSheet.InputSheet.Cells[r, c]);
                                    if (!workSheet.InputSheet.Cells[r, c].StringValue.IsNaOrBlank())
                                        count++;
                                }
                                if (calYearsDiff != count) // AgeInCalendarYears returns a max of 10 already
                                    LogFileValidationIssue($"3FI1C: Row {r + 1} number of calendar years ({count}) does not match calculated calendar years ({calYearsDiff}) in " + workSheet.InputSheet.Name + " sheet", cells);

                                // with a valid 22b table check best and worst return columns - AF-AK
                                RequiredColumns(workSheet, r, bestWorstReturns, "3FI2");
                            }
                            else
                            {
                                LogFileValidationIssue($"3FI1: Year minus table requires at least one value at row {r + 1} in " + workSheet.InputSheet.Name + " sheet", cells);
                            }
                        }
                        else
                            LogFileValidationIssue($"3FI1: Unable to locate year minus table at row {r + 1} in " + workSheet.InputSheet.Name + " sheet", cells);
                    }
                    else // 3.f.ii.
                    {
                        RequiredNAColumns(workSheet, r, tableColumns, "3FII1");
                        RequiredNAColumns(workSheet, r, bestWorstReturns, "3FII2");
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the provided list of column indexes on the provided row all have values - searches InputSheet
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="row">The zero based row index to check</param>
        /// <param name="foundColumns">The List of column indexes</param>
        /// <returns>boolean determining if there was a failed check</returns>
        internal bool RequiredColumns(WorksheetValidation workSheet, int row, List<int> foundColumns, string errorCode)
        {
            //3F1II/3F2I/3G
            bool retVal = true;
            foreach (int c in foundColumns)
            {
                if (workSheet.InputSheet.Cells[row, c].Type == Excel.CellValueType.IsNull || workSheet.InputSheet.Cells[row, c].StringValue.IsNaOrBlank())
                {
                    LogFileValidationIssue(errorCode + ": " +  workSheet.InputSheet.Cells[row, c].StringValue + " is not allowed at " + workSheet.InputSheet.Cells[row, c].Name + " in " + workSheet.InputSheet.Name + " sheet", workSheet.InputSheet.Cells[row, c]);
                    retVal = false;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Determine if the provided list of column indexes on the provided row all have N/A values - searches InputSheet
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="row">The zero based row index to check</param>
        /// <param name="foundColumns">The List of column indexes</param>
        /// <returns>boolean determining if there was a failed check</returns>
        internal bool RequiredNAColumns(WorksheetValidation workSheet, int row, List<int> foundColumns, string errorCode)
        {
            bool retVal = true;
            foreach (int c in foundColumns)
            {
                if (workSheet.InputSheet.Cells[row, c].Type == Excel.CellValueType.IsNull || !workSheet.InputSheet.Cells[row, c].StringValue.IsNaOrBlank())
                {
                    LogFileValidationIssue(errorCode + ": " + workSheet.InputSheet.Cells[row, c].StringValue + " must be N/A at " + workSheet.InputSheet.Cells[row, c].Name + " in " + workSheet.InputSheet.Name + " sheet", workSheet.InputSheet.Cells[row, c]);
                    retVal = false;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Determine if the provided list of column indexes have unique values on all rows - searches InputSheet
        /// </summary>
        /// <param name="workSheet">WorksheetValidation Object</param>
        /// <param name="foundColumns">The List of column indexes</param>
        internal void ValidateUniqueColumnContents(WorksheetValidation workSheet, List<int> foundColumns)
        {
            Excel.Cell cell = null;
            List<string> columnContents = new List<string>(Math.Max(workSheet.InputSheet.Cells.MaxDataRow - workSheet.FirstDataRow, 0));
            foreach (int c in foundColumns)
            {
                columnContents.Clear();
                for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    cell = workSheet.InputSheet.Cells[r, c];
                    if (columnContents.Contains(cell.StringValue))
                        LogFileValidationIssue("2AV: Duplicate column value " + cell.StringValue + " found at " + cell.Name + " in " + workSheet.InputSheet.Name + " sheet", cell);
                    else
                        columnContents.Add(cell.StringValue);
                }
            }
        }

        /// <summary>
        /// Check if the contents of the columns is valid XML - in this case it's valid if it recognizes all <> tags and the remaining ones are valid XML. Simple strings are also "valid" in this case, though these would not normally be considered so
        /// </summary>
        /// <param name="workSheet">The WorksheetValidation to process</param>
        /// <param name="foundColumns">List of columns in the worksheet to process</param>
        internal void ValidateXMLColumnContents(WorksheetValidation workSheet, List<int> foundColumns)
        {
            Excel.Cell cell = null;
            //List<string> columnContents = new List<string>(workSheet.InputSheet.Cells.MaxDataRow - workSheet.FirstDataRow);
            foreach (int c in foundColumns)
            {
                for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    cell = workSheet.InputSheet.Cells[r, c];
                    if (!paramValidator.IsValidXML(cell.StringValue.ExcelTextClean())) // added the less aggressive excel cleaner to remove common issues (particularly from translations)
                        LogFileValidationIssue($"XML: Invalid XML found at {cell.Name} in \"{workSheet.InputSheet.Name}\" sheet - detailed error: {paramValidator.LastError}", cell);
                }
            }
        }

        /// <summary>
        /// Check if the contents of the columns is a valid scenario identifier - valid if the flags are recognized, values are properly enclosed, flags are separated and no values are blank
        /// </summary>
        /// <param name="workSheet">The WorksheetValidation to process</param>
        /// <param name="foundColumns">List of columns in the worksheet to process</param>
        internal void ValidateScenarioColumnContents(WorksheetValidation workSheet, List<int> foundColumns)
        {
            Excel.Cell cell = null;
            //List<string> columnContents = new List<string>(workSheet.InputSheet.Cells.MaxDataRow - workSheet.FirstDataRow);
            foreach (int c in foundColumns)
            {
                for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    cell = workSheet.InputSheet.Cells[r, c];
                    if (!paramValidator.IsValidScenario(cell.StringValue))
                        LogFileValidationIssue("SCN: Invalid Scenario found in " + cell.StringValue + " found at " + cell.Name + " in " + workSheet.InputSheet.Name + " sheet - detailed error: " + paramValidator.LastError, cell);
                }
            }
        }

        internal void ValidateFieldAttributes(WorksheetValidation workSheet, List<int> foundColumns, bool restricted = true)
        {
            Excel.Cell cell = null;
            //List<string> columnContents = new List<string>(workSheet.InputSheet.Cells.MaxDataRow - workSheet.FirstDataRow);
            DataTable dtFields = new DataTable("Field Attributes");
            if (ProcessStream.PdiFile.DocumentTypeID.HasValue)
            {
                string sql = "SELECT Field_Name FROM[pdi_Publisher_Document_Field_Attribute]";
                if (restricted)
                    sql += " WHERE Document_Type_ID = @docTypeID AND Load_Type <> 'FSMRFP'";

                if (!dBConnection.LoadDataTable(sql, new Dictionary<string, object>(1) { { "@docTypeID", ProcessStream.PdiFile.DocumentTypeID } }, dtFields))
                    return;

                foreach (int c in foundColumns)
                {
                    for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
                    {
                        cell = workSheet.InputSheet.Cells[r, c];
                        if (!cell.StringValue.IsNaOrBlank() && dtFields.Select($"Field_Name = '{cell.StringValue.EscapeSQL()}'").FirstOrDefault() == null)
                            LogFileValidationIssue($"FA: Invalid Field Name '{cell.StringValue}' found at {cell.Name} in {workSheet.InputSheet.Name} sheet - Contact support if this is a valid field name");
                    }
                }
            }
        }

        
        internal void ValidateSheetName(WorksheetValidation workSheet, List<int> foundColumns)
        {
            Excel.Cell cell = null;
            //List<string> columnContents = new List<string>(workSheet.InputSheet.Cells.MaxDataRow - workSheet.FirstDataRow);

            foreach (int c in foundColumns)
            {
                for (int r = workSheet.FirstDataRow; r <= workSheet.InputSheet.Cells.MaxDataRow; r++)
                {
                    cell = workSheet.InputSheet.Cells[r, c];
                    if (!cell.StringValue.IsNaOrBlank() && !WorksheetExist(workSheet.InputSheet.Workbook, cell.StringValue))
                        LogFileValidationIssue("SHEET: Workbook does not contain a worksheet named " + cell.StringValue + " found at " + cell.Name + " in " + workSheet.InputSheet.Name + " sheet");
                }
            }
            
        }
        /// <summary>
        /// Given the validation string indicating the row, build the Dictionary for validation of that type
        /// </summary>
        /// <param name="workSheet">Excel type Worksheet</param>
        /// <param name="validationName">The string of the validation type we are looking for</param>
        /// <returns>List of column indexes</returns>
        internal List<int> LoadValidationFields(Excel.Worksheet workSheet, string validationName)
        {
            List<int> intList = new List<int>();
            string cellValue = string.Empty;
            int maxColumn = workSheet.Cells.MaxDataColumn;
            foreach (Excel.Comment com in workSheet.Comments)
            {
                if (com.Note.IndexOf(validationName) >= 0)
                {
                    for (int c = 0; c <= maxColumn; c++)
                    {
                        cellValue = workSheet.Cells[com.Row, c].StringValue;
                        if (!cellValue.IsNaOrBlank())
                            intList.Add(c);
                    }
                }
            }
            return intList;
        }

        /// <summary>
        /// Collect the column list and run validation for "DateFormat", "MaxTwoDecimals", "WholeNumbers", and "RequiredValues"
        /// </summary>
        /// <param name="worksheet">Excel type Worksheet</param>
        /// <param name="columnList">The List of column indexes</param>
        /// <param name="startRow">The first row of the sheet containing data</param>
        /// <param name="vt">ValidationType being validated</param>
        public void ValidateByType(Excel.Worksheet worksheet, List<int> columnList, int startRow, ValidationType vt)
        {
            Excel.Cell cell;
            string value;
            int scale;
            //string[] formats = { "d/MM/yyyy", "dd/MM/yyyy" };

            for (int r = startRow; r <= worksheet.Cells.MaxDataRow; r++)
            {
                foreach (int column in columnList)
                {
                    cell = worksheet.Cells[r, column];

                    if (cell.Type != Excel.CellValueType.IsNull && !cell.StringValue.IsNaOrBlank())
                    {
                        value = cell.StringValue;

                        switch (vt)
                        {
                            case ValidationType.DateFormat:
                                if (value.ToDate(DateTime.MinValue) == DateTime.MinValue) //!DateTime.TryParseExact(value, formats, new CultureInfo("en-US"), DateTimeStyles.None, out DateTime date)
                                    LogFileValidationIssue("2AIII3: Invalid date format found at cell " + cell.Name + " in " + worksheet.Name + " worksheet", cell);
                                break;

                            case ValidationType.MaxTwoDecimals:
                                scale = value.GetScale();
                                if (scale > 2)
                                     LogFileValidationIssue("2AIII4: More than two decimal places value found at " + cell.Name + " cell in " + worksheet.Name + " worksheet", cell);                            
                                else if (scale < 0)
                                    LogFileValidationIssue("2AIII4: Invalid value \"" + value + "\" found at cell " + cell.Name + " in " + worksheet.Name + " worksheet - should be a decimal number", cell);
                                break;
                            case ValidationType.MaxFourDecimals:
                                scale = value.GetScale();
                                if (scale > 4)
                                    LogFileValidationIssue("2AIII4: More than four decimal places value found at " + cell.Name + " cell in " + worksheet.Name + " worksheet", cell);
                                else if (value != "-" && scale < 0) // allow "-" in decimal number validation
                                    LogFileValidationIssue("2AIII4: Invalid value \"" + value + "\" found at cell " + cell.Name + " in " + worksheet.Name + " worksheet - should be a decimal number", cell);
                                break;
                            case ValidationType.MaxOneDecimal:
                                scale = value.GetScale();
                                if (scale > 1)
                                    LogFileValidationIssue("2AIII4: More than one decimal places value found at " + cell.Name + " cell in " + worksheet.Name + " worksheet", cell);
                                else if (scale < 0)
                                    LogFileValidationIssue("2AIII4: Invalid value \"" + value + "\" found at cell " + cell.Name + " in " + worksheet.Name + " worksheet - should be a decimal number", cell);
                                break;
                            case ValidationType.WholeNumbers:
                                if (value != "-" && !int.TryParse(value, out int result)) // allow "-" in whole number validation
                                    LogFileValidationIssue("2AIII5: Unexpected value \"" + value + "\" found at " + cell.Name + " in " + worksheet.Name + " worksheet - should be a whole number", cell);
                                break;
                        }
                    }
                    else if (vt == ValidationType.RequiredValues)
                        LogFileValidationIssue("3B1/2: Required value not found at " + cell.Name + " in " + worksheet.Name + " worksheet", cell);
                }
            }
        }

        /// <summary>
        /// Main validation loop which looks through comments in the Excel sheet and converts found values to validation types to be checked
        /// </summary>
        /// <param name="currentValidation">WorksheetValidation Object</param>
        internal void TemplateValidation(WorksheetValidation currentValidation, WorksheetValidation dataSheet = null) //Worksheet templateSheet, Worksheet inputSheet, int startRow
        {
            int maxColumn = currentValidation.TemplateSheet.Cells.MaxDataColumn;
            Excel.Cell cell = currentValidation.TemplateSheet.Cells[0, 0];
            string cellValue = string.Empty;

            ValidationType vt = ValidationType.DateFormat;

            List<int> columnList = new List<int>();

            foreach (Excel.Comment com in currentValidation.TemplateSheet.Comments)
            {
                foreach (string validType in Enum.GetNames(typeof(ValidationType)))
                {
                    if (com.Note.IndexOf(validType) >= 0)
                    {
                        columnList.Clear();
                        cellValue = com.Note;
                        vt = (ValidationType)Enum.Parse(typeof(ValidationType), validType);
                        for (int c = 0; c <= maxColumn; c++)
                        {
                            if (!currentValidation.TemplateSheet.Cells[com.Row, c].StringValue.IsNaOrBlank())
                                columnList.Add(c);
                        }

                        switch (vt)
                        {
                            case ValidationType.UnacceptableElements:
                            case ValidationType.AcceptableElements:
                                CheckElementsInColumn(currentValidation, columnList.ToArray(), cellValue.Substring(cellValue.IndexOf(validType) + validType.Length).Trim().Split('\n'), vt);
                                break;

                            case ValidationType.RowSequenceCheck:
                                ValidateRowNumbers(currentValidation, columnList.ToArray());
                                break;
                            case ValidationType.RowWithDateSequenceCheck:
                                ValidateRowWithDateNumbers(currentValidation, columnList.ToArray());
                                break;

                            case ValidationType.FlagValueValidation:
                                ValidateFlagRow(currentValidation, columnList, "Flag");
                                break;

                            case ValidationType.DateFormat:
                            case ValidationType.MaxTwoDecimals:
                            case ValidationType.MaxFourDecimals:
                            case ValidationType.WholeNumbers:
                            case ValidationType.RequiredValues:
                                ValidateByType(currentValidation.InputSheet, columnList, currentValidation.FirstDataRow, vt);
                                break;

                            case ValidationType.AtLeastOneRequired:
                                ValidateOneRequired(currentValidation, columnList);
                                break;
                            case ValidationType.AllOrNone:
                                ValidateAllOrNone(currentValidation, columnList);
                                break;
                            case ValidationType.TwelveMonthValidation:
                                Validate12Months(currentValidation, columnList);
                                break;
                            case ValidationType.CalendarYearValidation:
                                ValidateCalendarYears(currentValidation, columnList);
                                break;
                            case ValidationType.UniqueColumnContents:
                                ValidateUniqueColumnContents(currentValidation, columnList);
                                break;
                            case ValidationType.XMLValidation:
                                ValidateXMLColumnContents(currentValidation, columnList);
                                break;
                            case ValidationType.ScenarioValidation:
                                ValidateScenarioColumnContents(currentValidation, columnList);
                                break;
                            case ValidationType.FieldAttributeCheck:
                                ValidateFieldAttributes(currentValidation, columnList);
                                break;
                            case ValidationType.FieldAttributeAllCheck:
                                ValidateFieldAttributes(currentValidation, columnList, false);
                                break;
                            case ValidationType.SheetNameCheck:
                                ValidateSheetName(currentValidation, columnList);
                                break;
                            case ValidationType.ActiveFilter:
                                LogFilters(currentValidation.InputSheet);
                                break;
                            case ValidationType.MatchingDataSheetRequired:
                                MatchColumnValueToDocumentDataTab(currentValidation, dataSheet, columnList);
                                break;
                            default:
                                // no action for other types here
                                break;
                        }
                    }
                }
            }
        }

        //scan the template until a blank cell is found, or the cell contains "Y" - the previous row was the last header
        internal int CountHeaderRows(Excel.Worksheet templateSheet)
        {
            int maxRow = templateSheet.Cells.MaxDataRow;
            int r;
            for (r = 1; r <= maxRow; r++) // start checking on the second row
            {
                if (templateSheet.Cells[r, 0].StringValue.IsNaOrBlank() || templateSheet.Cells[r, 0].StringValue.Trim() == "Y")
                    return r;
            }
            return r;
        }

        /// <summary>
        /// Helper function to handle single cell error logs by creating a list and using the list version
        /// </summary>
        /// <param name="errorMessage">The error message string</param>
        /// <param name="validationCell">The Excel.Cell to apply comments to</param>
        public void LogFileValidationIssue(string errorMessage, Excel.Cell validationCell = null)
        {
            if (validationCell != null)
                LogFileValidationIssue(errorMessage, new List<Excel.Cell> { validationCell });
            else
                LogFileValidationIssue(errorMessage, new List<Excel.Cell>());
        }

        /// <summary>
        ///  Log error messages to the File validation - also adds comments to the active sheet (in memory) to each cell in the List
        /// </summary>
        /// <param name="errorMessage">The error message string</param>
        /// <param name="validationCellList">The List of Excel.Cell to apply comments to</param>
        public void LogFileValidationIssue(string errorMessage, List<Excel.Cell> validationCellList)
        {
            if (validationCellList != null)
            {
                foreach (Excel.Cell validationCell in validationCellList)
                {
                    Excel.Comment com = validationCell.Worksheet.Comments[validationCell.Name];
                    if (com is null)
                    {
                        int comIndex = validationCell.Worksheet.Comments.Add(validationCell.Name);
                        com = validationCell.Worksheet.Comments[comIndex];
                    }
                    com.Author = "Publisher Validation";
                    if (com.Note != null && com.Note.Length > 0)
                        com.Note = com.Note + "\n\n" + errorMessage;
                    else
                        com.Note = errorMessage;
                    com.CommentShape.IsTextWrapped = true;
                    com.CommentShape.WidthCM = 3;
                    //TODO: figure out comment height based on text content - length of text, number or line breaks, font size
                }
            }
 
            //set file validation status to false
            IsValidData = false;
            Logger.AddError(Log, errorMessage);
            //LogFileValidationIssueToDB(errorMessage);
        }

        

        /// <summary>
        /// Check if the FundCode already exists in the pdi_publisher_Documents table for the provided client - return true if it doesn't
        /// </summary>
        /// <param name="fundCode">The FundCode to check</param>
        /// <param name="clientID">The Client_ID to check</param>
        /// <returns>True if FundCode does not exist</returns>
        public bool IsBrandNewFund(string docCode, string fundCode, int? docTypeID, string filingID, int? clientID)
        {
            //bool retVal = false;
            if (clientID.HasValue && docTypeID.HasValue)
            {
                DataTable dt = new DataTable();
                dBConnection.LoadDataTable("SELECT DISTINCT PD_Doc.FilingReferenceID AS DocFilingReferenceID, PD_Doc.FFDocAgeStatusID as DocFFDocAgeStatusID, PD_Fund.FilingReferenceID, PD_Fund.FFDocAgeStatusID FROM [pdi_Publisher_Documents] PD_Fund FULL OUTER JOIN [pdi_Publisher_Documents] PD_DOC ON PD_Doc.Client_ID = PD_Fund.Client_ID AND PD_DOC.Document_Type_ID = PD_Fund.Document_Type_ID AND PD_DOC.IsActiveStatus = PD_Fund.IsActiveStatus WHERE PD_Doc.Document_Number = @docCode AND PD_Fund.IsActiveStatus = 1 AND PD_Fund.Client_ID = @clientID AND PD_Fund.FundCode = @fundCode AND PD_Fund.Document_Type_ID = @docTypeID ORDER BY 1 DESC, 3 DESC;", new Dictionary<string, object>(4)
                {
                    { "@fundCode", fundCode },
                    { "@clientID", clientID },
                    { "@docCode", docCode },
                    { "@docTypeID", docTypeID }
                }, dt);

                return IsBrandNewFund(dt, filingID);           
            }
            return false;
        }

        /// <summary>
        /// Given a specifically formatted datatable - process it to determine if the fund is brand new or not
        /// </summary>
        /// <param name="dt">The specially formatted DataTable</param>
        /// <param name="filingID">The FilingReferenceID of the current record</param>
        /// <returns>True if brand new False otherwise</returns>
        public static bool IsBrandNewFund(DataTable dt, string filingID)
        {
            if (dt is null || dt.Rows.Count < 1)
                return true;
            foreach(DataRow dr in dt.Rows) // normally only 1 row is expected - multiple rows would indicate that the funds are in different states or have different Filing IDs
            {
                if (dr.Field<string>("DocFilingReferenceID") == filingID && (dr.IsNull("DocFFDocAgeStatusID") || dr.Field<int>("DocFFDocAgeStatusID") == 0)) // Filing Ref ID matches and the status id is null or 0
                    return true;
                else if (dr.IsNull("DocFilingReferenceID")) // no matching document exists - look for matching funds
                {
                    if (dr.IsNull("FilingReferenceID")) // Fund(s) have been inserted by the STATIC file (Do all funds need to be null?) Sorting will push nulls to be last
                        return true;
                    else
                        if (dr.Field<string>("FilingReferenceID") == filingID && (dr.IsNull("FFDocAgeStatusID") || dr.Field<int>("FFDocAgeStatusID") == 0)) // The matching fund(s) are from the same filing ID and have a status ID of null or 0
                            return true;
                }
            }
            return false; // the normal state of the BNF check is false - if none of the rows match any of the BNF conditions
        }

        /// <summary>
        /// Update File Log table with file status and total number of documents
        /// </summary>
        /// <param name="dataID">data id</param>
        /// <param name="documentCount">total number of documents on main data sheet</param>
        public void UpdateFileStatusAndDcoumentCountToLogTable(int dataID, int documentCount)
        {
            if (dataID > 0) 
            {
                if (!dBConnection.ExecuteNonQuery("UPDATE [pdi_File_Log] SET IsValidDataFile = @status, Number_of_Records = @documentCount WHERE Data_ID = @dataID", out int rows, new Dictionary<string, object>(3) {
                    { "@status", IsValidData },
                    { "@documentCount", documentCount },
                    { "@dataID", dataID }
                }))
                {
                    Logger.AddError(Log, $"Failed to update File Status and Count - {dBConnection.LastError}");
                }
            }
        }

        /// <summary>
        /// Load all valid parameters so we can remove them when testing XML string validity
        /// </summary>
        /// <returns>A Dictionary containing all the valid parameters with an empty string for replacement</returns>
        public Dictionary<string, string> loadValidParameters()
        {
            return loadValidParameters(dBConnection);
        }

        public static Dictionary<string, string> loadValidParameters(DBConnection dbCon)
        {
            if (dbCon != null)
            {
                string sql = "SELECT * FROM (SELECT col.name as Parameter_Name FROM sys.tables as tab INNER JOIN sys.columns AS col ON tab.object_id = col.object_id WHERE tab.name = 'pdi_Publisher_Documents' UNION SELECT StaticToken As Parameter_Name FROM pdi_Content_Scenario_Parameters ) T ORDER BY Len(Parameter_Name) DESC"; // WHERE Document_Type_ID = @docTypeID

                DataTable dt = new DataTable("ValidParameters");
                dbCon.LoadDataTable(sql, null, dt);

                return dt.AsEnumerable().ToDictionary<DataRow, string, string>(row => row.Field<string>(0), row => string.Empty);
            }
            return new Dictionary<string, string>();
        }
    }
}

