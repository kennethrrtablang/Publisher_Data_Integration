using System;
using System.Collections.Generic;
using System.Data;
using Excel = Aspose.Cells;
using Publisher_Data_Operations.Extensions;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;

namespace Publisher_Data_Operations.Helper
{

    public class AsposeLoader : IDisposable
    {
        Excel.Workbook workBook = null;
        private bool disposedValue;

        public const string OptionalTableEndTag = "EndTable!"; // 20220510 - added for HSBC - an optional table end tag that denotes the column that extraction ends on.
        /// <summary>
        /// Create a new class based on the passed path to an Excel file - license uses the embedded license file
        /// </summary>
        /// <param name="filePath"></param>
        public AsposeLoader(string filePath) 
        {
            try
            {
                Excel.License license = new Excel.License();
                license.SetLicense("Aspose.Total.lic");
                Excel.LoadOptions opt = new Excel.LoadOptions();
                opt.MemorySetting = Excel.MemorySetting.MemoryPreference; // Load with memory optimizations on - about 1/4 memory use but slightly slower
                if (File.Exists(filePath))
                    workBook = new Excel.Workbook(filePath, opt);
            }
            catch (Exception err)
            {
                throw new Exception("there was an error in AsposeLoader creating the workbook from file path - " + err.Message);
            }
        }

        public AsposeLoader(Stream memStream)
        {
            try
            {
                Excel.License license = new Excel.License();
                license.SetLicense("Aspose.Total.lic");
                Excel.LoadOptions opt = new Excel.LoadOptions();
                opt.MemorySetting = Excel.MemorySetting.MemoryPreference; // Load with memory optimizations on - about 1/4 memory use but slightly slower
                if (memStream != null && memStream.CanRead)
                    workBook = new Excel.Workbook(memStream, opt);
            }
            catch (Exception err)
            {
                throw new Exception("there was an error in AsposeLoader creating the workbook from memory stream- " + err.Message);
            }
        }

        /// <summary>
        /// Does the workBook contain the indicated worksheet
        /// </summary>
        /// <param name="sheetName">The sheetname to find</param>
        /// <returns>bool if the sheet exists</returns>
        public bool HasWorkSheet(string sheetName)
        {
            if (workBook != null)
                return workBook.Worksheets[sheetName] != null;
            return false;
        }

        /// <summary>
        /// Return all the worksheet names in the workbook
        /// </summary>
        /// <returns></returns>
        public List<string> WorksheetNames()
        {
            List<string> worksheetNames = new List<string>();
            if (workBook != null && workBook.Worksheets != null)
            {
                foreach (Excel.Worksheet ws in workBook.Worksheets)
                    worksheetNames.Add(ws.Name);
            }
            return worksheetNames;
        }

        // remove all data from all sheets in provided Excel file except for the header rows
        public bool CleanSheets()
        {
            if (workBook != null)
            {
                foreach (Excel.Worksheet ws in workBook.Worksheets)
                {
                    int firstDataRow = WorksheetValidation.DefaultHeaderRows(WorksheetValidation.GetSheetType(ws.Name)) + 1;
                    if (!ws.Cells.DeleteRows(firstDataRow, ws.Cells.MaxDataRow - firstDataRow))
                        return false;

                    ws.Comments.Clear();
                }     
            }
            return true;
        }

        // the dataset contains tables that have names which map to the same SheetType - for each table find the matching sheet type (if it exists) and then find the matching columns and import the datatable
        public bool PopulateSheets(DataSet dtSet)
        {
            if (dtSet is null && workBook != null)
                return false;
            foreach (Excel.Worksheet ws in workBook.Worksheets)
            {
                SheetType wsType = WorksheetValidation.GetSheetType(ws.Name);
                foreach (DataTable dt in dtSet.Tables)
                {
                    if (WorksheetValidation.GetSheetType(dt.TableName) == wsType)
                    {
                        string selectColumns = string.Empty;
                        int lastHeaderRow = WorksheetValidation.DefaultHeaderRows(wsType);
                        for (int c = 0; c < ws.Cells.MaxDataColumn; c++)
                        {
                            if (dt.Columns.Contains(ws.Cells[lastHeaderRow, c].StringValue))
                                selectColumns += ws.Cells[lastHeaderRow, c].StringValue + ",";
                            else
                            {
                                string wsColumn = ws.Cells[lastHeaderRow, c].StringValue.RemoveExceptAlpha();
                                for (int i = 0; i < dt.Columns.Count; i++)
                                {
                                    if (wsColumn == dt.Columns[i].ColumnName.RemoveExceptAlpha())
                                        selectColumns += dt.Columns[i].ColumnName + ",";
                                }
                            }
                        }

                        if (selectColumns.Length > 0)
                        {
                            DataRow[] filteredData = dt.Select("SELECT " + selectColumns.Substring(0, selectColumns.Length - 1));
                            if (filteredData.Length > 0)
                                ws.Cells.ImportDataTable(filteredData.CopyToDataTable(), false, lastHeaderRow + 1, 0);
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// For the STATIC workflow check the table UPDATE tab and return a Dictionary of the sheet names with a bool indicating if they should be updated
        /// </summary>
        /// <param name="sheetName">The optional worksheet name</param>
        /// <returns>Dictionary of worksheets to update</returns>
        public Dictionary<string, bool> TableUpdate(string sheetName = "Table UPDATE")
        {
            return TableUpdate(workBook, sheetName);
        }

        public static Dictionary<string, bool> TableUpdate(Excel.Workbook wb, string sheetName = "Table UPDATE")
        {
            Dictionary<string, bool> updateList = new Dictionary<string, bool>();
            if (wb != null && wb.Worksheets[sheetName] != null)
            {
                DataTable worksheet = ExtractTables(wb, sheetName);
                int updateCol = worksheet.FindDataTableColumn("Update");
                int sheetCol = worksheet.FindDataTableColumn("Sheet");
                if (updateCol >= 0 && sheetCol >= 0)
                {
                    foreach (DataRow dr in worksheet.Rows)
                    {
                        if (!updateList.ContainsKey(dr.GetStringValue(sheetCol)))
                            updateList.Add(dr.GetStringValue(sheetCol), dr.GetBoolValue(updateCol));
                        //duplicates aren't supposed to happen here but this isn't a validation check

                    }
                        
                }
            }
            return updateList;
        }
        /// <summary>
        /// Convert the indicated worksheet table to a DataTable
        /// </summary>
        /// <param name="sheetName">The worksheet name</param>
        /// <param name="columnNames">Should the first column be column names (header not data)</param>
        /// <param name="asString">Export all data as string</param>
        /// <returns></returns>
        public DataTable ExtractTables(string sheetName, bool columnNames = true, bool asString = true, int headerRowOffset = 0)
        {
            return ExtractTables(workBook, sheetName, columnNames, asString, headerRowOffset);
        }

        public static DataTable ExtractTables(Excel.Workbook wb, string sheetName, bool columnNames = true, bool asString = true, int headerRowOffset = 0)
        {
            DataTable dt = null;
            if (wb != null && wb.Worksheets[sheetName] != null)
            {
                Excel.Worksheet ws = wb.Worksheets[sheetName];
                TableLimits(ws, out int rowCount, out int colCount);
                if (rowCount < 1 || colCount < 1)
                    return dt;
                if (asString)
                    dt = ws.Cells.ExportDataTableAsString(headerRowOffset, 0, rowCount, colCount, columnNames);
                else
                    dt = ws.Cells.ExportDataTable(headerRowOffset, 0, rowCount, colCount, columnNames);
            }
            if (dt != null)
                dt.TableName = sheetName;

            return dt;
        }

        public static int TableLimits(Excel.Worksheet ws, out int rowCount, out int colCount)
        {
            // For HSBC we are starting to have Start and End table tags - check if the End tag is present and if so extract to that column/row instead of max data

            Excel.FindOptions findOptions = new Excel.FindOptions
            {
                SeachOrderByRows = true,
                LookInType = Excel.LookInType.Values,
                LookAtType = Excel.LookAtType.EntireContent,
                SearchNext = false // look backwards to get the last instance of the end tag (if present)
            };

            Excel.Cell endCell = ws.Cells.Find(OptionalTableEndTag, null, findOptions);

            if (endCell != null)
            {
                rowCount = endCell.Row + 1; // add one to the zero based row index to get the number of rows - this assumes we are always starting at A1
                colCount = endCell.Column + 1;
            }
            else
            {
                rowCount = ws.Cells.MaxDataRow + 1;
                colCount = ws.Cells.MaxDataColumn + 1;
            }
            return 0;
        }
        /// <summary>
        /// Handle source data that contains 2 header rows by creating a combined column name using both rows
        /// </summary>
        /// <param name="sheetName">The name of the worksheet to convert to a datatable</param>
        /// <param name="astring">A boolean indicating if the export should use string type on all data</param>
        /// <returns>DataTable with first 2 columns as column name</returns>
        public DataTable ExtractTableDoubleHeader(string sheetName, bool astring = true)
        {
            DataTable dt = ExtractTables(sheetName, false, astring);
            DataTable dtOut = new DataTable();
            dtOut.TableName = sheetName;
            

            if (dt.Rows.Count >= 2)
            {
                foreach (DataColumn dc in dt.Columns)
                {
                    string colName = string.Empty;
                    if (dt.Rows[0][dc].ToString().Length > 0)
                        colName = dt.Rows[0][dc].ToString() + PDIFile.FILE_DELIMITER + dt.Rows[1][dc].ToString(); // ti.ToTitleCase()
                    else
                        colName = dt.Rows[1][dc].ToString();
                    dtOut.Columns.Add(new DataColumn(colName, dc.DataType));
                }

                for (int r = 2; r < dt.Rows.Count; r++)
                {
                    //dtOut.ImportRow(dt.Rows[r]);
                    dtOut.Rows.Add(dt.Rows[r].ItemArray);
                }
                dt.Dispose();
                return dtOut;
            }
            else
                return ExtractTables(sheetName, true, astring);
        }

        public DataTable ExtractWithFormatting(string sheetName, bool sourceHasRowType = false, bool columnNames = true, bool asString = true, int headerRowOffset = 0, string formatColumnName = Generic.FSMRFP_ROWTYPE_COLUMN, Logger log = null)
        {
            DataTable dt = new DataTable();
            if (workBook != null && workBook.Worksheets[sheetName] != null)
            {
                if (!workBook.Worksheets[sheetName].IsVisible) // Until validation is added rejecting files with hidden sheets.
                {
                    Logger.AddError(log, $"There was a hidden sheet named {sheetName} found - ignoring any data");
                    return dt;
                }
                // Unfortunately ExportAsHtmlString is not available in Aspose.Cells below version 17.4.5 and we are using 8.8.2 released 6/3/2016 (version numbers jumped to 16 in 2016) so we need to do the work
                Excel.Worksheet ws = workBook.Worksheets[sheetName];

                dt.TableName = ws.Name;
                // setup the DataTable

                TableLimits(ws, out int rowCount, out int colCount);

                for (int c = 0; c < colCount; c++)
                    dt.Columns.Add(new DataColumn("Column_" + ConvertIndexToAddress(-1, c), typeof(string)));

                //if (dt.Columns.Count < 1)
                //    Logger.AddError(log, $"No columns found in {sheetName}");
                if (sourceHasRowType && dt.Columns.Count > 1)
                    dt.Columns[dt.Columns.Count - 1].ColumnName = formatColumnName; // the last data column is assumed to be the formatColumn


                List<string> rowType = new List<string>(20);
                for (int r = headerRowOffset; r < rowCount; r++)
                {
                    DataRow dr = dt.NewRow();
                    rowType.Clear();
                    
                    for (int c = 0; c < colCount; c++) // Aspose row and column counts are 0 based so use <= (MaxDataColumn 8 = 9 columns of data)
                    {
                        Excel.Cell cell = ws.Cells[r, c];
                        string val = cell.StringValue.Trim().ExcelTextClean(); //.CleanXML();
                        if (!sourceHasRowType || c != dt.Columns.Count - 1) // if there is a sourceRowType and we are on that column don't try to extract the formatting - just pass through the value
                        {
                            Excel.Style style = cell.GetStyle(false);
                            Excel.BorderCollection bc = style.Borders;
                            Excel.FontSetting[] chars = cell.GetCharacters();
                            //Console.Write("Text: '" + cell.StringValue.Trim() + "' Formating: ");

                            if (style.IndentLevel > 0)
                                rowType.Add($"{c}|Indent|{style.IndentLevel}"); // Console.Write("Indent " + style.IndentLevel.ToString() + " ");

                            //foreach (Excel.BorderType b in Enum.GetValues(typeof(Excel.BorderType)))
                            //{
                            //    if (bc[b].LineStyle != Excel.CellBorderType.None)
                            //        rowType.Add($"{c}|{b}|{bc[b].LineStyle}");
                            //}
                           
                            if (chars != null) // we have multiple formatted sections
                            {
                                val = string.Empty; // clear the cell contents value as we will be building it in order
                                string cellString = cell.StringValue;
                                foreach (Excel.FontSetting fs in chars)
                                    val += GetFonts(fs, style) + cellString.Substring(fs.StartIndex, fs.Length) + GetFonts(fs, style, true); //.CleanXML()

                                val = val.Trim().ExcelTextClean();
                                //Console.Write(val);
                            }
                            // now that we aren't adding entire cell formatting in the chars section we need to add it regardless of chars or not
                            if (val != null && val.Length > 0) // if the value is empty we don't need to record the cell formatting
                            {
                                if (style.Font.IsBold)
                                {
                                    rowType.Add($"{c}|Bold|");
                                    val = $"<strong>{val}</strong>";
                                }
                                if (style.Font.IsItalic)
                                {
                                    rowType.Add($"{c}|Italic|");
                                    val = $"<em>{val}</em>";
                                }
                                if (style.Font.Underline == Excel.FontUnderlineType.Single) // other underline types?
                                {
                                    rowType.Add($"{c}|Underline|{style.Font.Underline}");
                                    val = $"<u>{val}</u>";
                                }
                                if (style.Font.IsSubscript)
                                {
                                    rowType.Add($"{c}|Subscript|");
                                    val = $"<sub>{val}</sub>";
                                }
                                if (style.Font.IsSuperscript)
                                {
                                    rowType.Add($"{c}|Superscript|");
                                    val = $"<sup>{val}</sup>";
                                }
                                
                            }
                        }
                        dr[c] = val; //.OutgoingHTMLtoXML();
                    }
                    //if (!sourceHasRowType)
                    //    dr[formatColumnName] = string.Join(",", rowType);
                    dt.Rows.Add(dr);
                }
            }

            // before returning the DataTable check that the rowtype exists if expected and that there aren't any "blank" rows at the end

            if (dt != null && dt.Columns.Count > 1 && dt.RemoveBlankColumnsAtEnd(sourceHasRowType, formatColumnName))
                Logger.AddError(log, $"Removed a blank column at the end of {sheetName}");


            return dt;
        }

        /// <summary>
        /// Apply HTML tags to embedded formatting in Excel cell - called twice, once before value and once after with endTag true
        /// </summary>
        /// <param name="fonts">The FontSetting to check</param>
        /// <param name="endTag">True if the tags should be end tags</param>
        /// <returns>a string of formatting tags</returns>
        private string GetFonts(Excel.FontSetting fonts, Excel.Style cellStyle, bool endTag = false)
        {
            string tag = endTag ? "</": "<";
   
            string ret = string.Empty;
            // using independent if statements instead of switch to catch all those bold italic underlined superscripts - when applying the end tag the order is reversed to close inner to outer
            if (fonts.Font.IsBold && !cellStyle.Font.IsBold)
                ret = endTag ? $"{tag}strong>" + ret : ret + $"{tag}strong>";
            if (fonts.Font.IsItalic && !cellStyle.Font.IsItalic)
                ret = endTag ? $"{tag}em>" + ret : ret + $"{tag}em>";
            if (fonts.Font.IsSubscript && !cellStyle.Font.IsSubscript)
                ret = endTag ? $"{tag}sub>" + ret : ret + $"{tag}sub>";
            if (fonts.Font.IsSuperscript && !cellStyle.Font.IsSuperscript)
                ret = endTag ? $"{tag}sup>" + ret : ret + $"{tag}sup>";
            if (fonts.Font.IsStrikeout && !cellStyle.Font.IsStrikeout)
                ret = endTag ? $"{tag}s>" + ret : ret + $"{tag}s>";
            if (fonts.Font.Underline == Excel.FontUnderlineType.Single && cellStyle.Font.Underline != Excel.FontUnderlineType.Single) // anything other than a single underline will need to be css or use a special tag for Publisher composition
                ret = endTag ? $"{tag}u>" + ret : ret + $"{tag}u>";

            return ret;
        }

        /// <summary>
        /// Build the data for the Allocation Table
        /// </summary>
        /// <returns></returns>
        public DataTable BuildAllocationTable()
        {
            DataTable outputTable = new DataTable("AllocationTables");
            TableList tableBuilder = new TableList();

            outputTable.PrimaryKey = new DataColumn[] { outputTable.Columns.Add("FundCode", typeof(string)), outputTable.Columns.Add("AllocationType", typeof(string)) };
            outputTable.Columns.Add("en-CA", typeof(string));
            outputTable.Columns.Add("fr-CA", typeof(string));

            if (workBook is null)
                return outputTable;

            string sheetName = "Allocation Tables";
            tableBuilder.Clear();
            DataTable table = ExtractTables(sheetName); //curSheet.Cells.ExportDataTableAsString(0, 0, curSheet.Cells.MaxDataRow + 1, curSheet.Cells.MaxDataColumn + 1, true);
            if (table is null)
                return outputTable;

            table.TableName = sheetName;

            //add the new columns to the outputTable
            //outputTable.Columns.Add(curSheet.Name + FileName.FILE_DELIMITER + "EN", typeof(string));
            //outputTable.Columns.Add(curSheet.Name + FileName.FILE_DELIMITER + "FR", typeof(string));

            int fundCodeCol = table.FindDataTableColumn("FundCode");
            int numberCol = table.FindDataTableColumn("number");
            int allocationCol = table.FindDataTableColumn("Allocation");
            int valueCol, enCol, frCol, levelCol;

            if (fundCodeCol >= 0 && numberCol >= 0 && allocationCol >= 0)
            {
                //find the rest of the columns
                valueCol = table.FindDataTableColumn("Value");
                enCol = table.FindDataTableColumn("en-CA");
                frCol = table.FindDataTableColumn("fr-CA");
                levelCol = table.FindDataTableColumn("Level");

                string fundCode = string.Empty;
                string allocation = string.Empty;
                foreach (DataRow dataRow in table.Rows)
                {
                    if (fundCode != dataRow.Field<string>(fundCodeCol) || allocation != dataRow.Field<string>(allocationCol))
                    {
                        if (fundCode != string.Empty && allocation != string.Empty)
                        {
                            outputTable.Rows.Add(fundCode, allocation, tableBuilder.GetTableString(), tableBuilder.GetTableStringFrench());
                            //AddTableOutput(outputTable, fundCode, tableBuilder, table.TableName);
                            tableBuilder.Clear();
                        }
                        fundCode = dataRow.Field<string>(fundCodeCol);
                        allocation = dataRow.Field<string>(allocationCol);
                    }
                    tableBuilder.AddValidation(dataRow.Field<string>(valueCol), dataRow.Field<string>(enCol), dataRow.Field<string>(frCol), dataRow.Field<string>(numberCol), (levelCol >= 0 ? dataRow.Field<string>(levelCol) : null), (allocationCol >= 0 ? dataRow.Field<string>(allocationCol) : null)); // replaced level for bug 12830
                }
                outputTable.Rows.Add(fundCode, allocation, tableBuilder.GetTableString(), tableBuilder.GetTableStringFrench());
            }
            outputTable.DefaultView.Sort = "FundCode";
            //ConsolePrintTable(outputTable.DefaultView.ToTable());
            return outputTable.DefaultView.ToTable();
        }

        /// <summary>
        /// Adds supported sheets to the datatable as English and French formatted tables
        /// </summary>
        /// <param name="dt">The DataTable to add rows to</param>
        /// <param name="jobID">The current Job_ID</param>
        /// <param name="sheetName">The name of the sheet to process</param>
        /// <param name="fieldName">The partial field name to use when adding rows</param>
        /// <param name="shortMonths">The optional Dictionary of short month names</param>
        /// <returns></returns>
        public bool AddGenericTable(DataTable dt, Generic gen, RowIdentity rowIdentity, int jobID, string sheetName, string fieldName, Dictionary<string, string[]> shortMonths = null , bool trimAt436 = false)
        {          
            if (workBook is null)
                return false;

            TableList tableBuilder = new TableList();
            if (shortMonths != null)
                tableBuilder.ShortMonths = shortMonths;

            DataTable table = ExtractTables(sheetName); //curSheet.Cells.ExportDataTableAsString(0, 0, curSheet.Cells.MaxDataRow + 1, curSheet.Cells.MaxDataColumn + 1, true);
            table.TableName = sheetName;

            int fundCodeCol = table.FindDataTableColumn("FundCode");
            int numberCol = table.FindDataTableColumn("Row number");
            int valueCol, enCol, frCol, levelCol, distributionCol;

            if (fundCodeCol < 0)
                fundCodeCol = table.FindDataTableColumn("DocumentCode");

            if (fundCodeCol >= 0 && numberCol >= 0)
            {
                //find the rest of the columns
                valueCol = table.FindDataTableColumn("Value");
                if (valueCol < 0)
                {
                    valueCol = table.FindDataTableColumn("Number of Investments");
                    tableBuilder.TableType = TableTypes.Number;
                    if (valueCol < 0)
                    {
                        valueCol = table.FindDataTableColumn("Date");
                        tableBuilder.TableType = TableTypes.Date;
                    }
                }
                else
                    tableBuilder.TableType = TableTypes.Percent;

                enCol = table.FindDataTableColumn("en-CA");
                frCol = table.FindDataTableColumn("fr-CA");
                if (enCol < 0)
                {
                    enCol = table.FindDataTableColumn("Type EN");
                    frCol = table.FindDataTableColumn("Type FR");
                }
                if (enCol < 0)
                {
                    enCol = table.FindDataTableColumn(PDIFile.FILE_DELIMITER + "EN");
                    frCol = table.FindDataTableColumn(PDIFile.FILE_DELIMITER + "FR");
                }
                levelCol = table.FindDataTableColumn("Level");
                distributionCol = table.FindDataTableColumn("Distribution");
                if (distributionCol < 0)
                    distributionCol = table.FindDataTableColumn("Allocation");
                else
                    tableBuilder.TableType = TableTypes.Currency;

                if (valueCol >= 0) //enCol >= 0 && frCol >= 0 && 
                {
                    string fundCode = string.Empty;
                    bool skipFundCode = false;
                    foreach (DataRow dr in table.Rows)
                    {
                        if (fundCode != dr.Field<string>(fundCodeCol))
                        {
                            if (fundCode != string.Empty && !skipFundCode)
                            {
                                // there are no already extracted field information (in English) so output the fund data
                                if(tableBuilder.Any() && tableBuilder.Count() > 436 && trimAt436)
                                {
                                    tableBuilder.RemoveRange(1, tableBuilder.Count() - 436);
                                }
                                dt.Rows.Add(jobID, fundCode, sheetName, fieldName + PDIFile.FILE_DELIMITER + "EN", 0, 0, tableBuilder.GetTableString()); // will grab the first column with _EN
                                dt.Rows.Add(jobID, fundCode, sheetName, fieldName + PDIFile.FILE_DELIMITER + "FR", 0, 0, tableBuilder.GetTableStringFrench()); // will grab the first column with _FR
                            }
                            tableBuilder.Clear();
                            fundCode = dr.Field<string>(fundCodeCol);

                            // Due to the removal of the validation that prevents 16, 17 and 40 table data from being present when IsProforma is set we need to check each FundCode to see if it already has the current field set and if it does set the fund to be skipped
                            //us17798
                            DataRow[] existsDocuments = dt.Select($"Job_ID = '{jobID}' AND Value = '{fundCode}' AND Sheet_Name = 'DocumentData' AND Item_Name = 'FundCode'");
                            if (existsDocuments.Length > 0)
                            {
                                // Now check if there is an existing field value for the first document in the list (We are working at the fund level so any document results should be the same
                                DataRow[] existsRecords = dt.Select($"Job_ID = '{jobID}' AND Code = '{existsDocuments[0].GetExactColumnStringValue("Code")}' AND Sheet_Name = 'DocumentData' AND Item_Name = '{fieldName + PDIFile.FILE_DELIMITER + "EN"}'");
                                if (existsRecords.Length > 0)
                                    skipFundCode = true;
                                else
                                    skipFundCode = false;
                            }
                        }

                        if (skipFundCode)
                            break; // if the fundcode is set to be skipped then break to the next datarow at this point

                      // 20211122 - SK - this was something missed in US 8290 initially - when the French value is N/A a lookup attempt on the English value should be made - now passing in Generic and RowIdenity classes to handle the lookup and associated missing French                        

                        string fr = (frCol >= 0 ? dr.Field<string>(frCol) : null); // This allows the _FR column to be optional as it will return null and then try to lookup the French value in the database
                        string en = (enCol >= 0 ? dr.Field<string>(enCol) : null);
                        if (en != null) // allow lookup even when FR column is not present 20220215 - fr != null &&
                        {
                            if (fr.IsNaOrBlank())
                            {
                                rowIdentity.DocumentCode = fundCode;
                                fr = gen.GenerateFrench(en, rowIdentity, jobID, fieldName);
                            }
                                
                        }                      
                        tableBuilder.AddValidation(dr.Field<string>(valueCol), en, fr, dr.Field<string>(numberCol), (levelCol >= 0 ? dr.Field<string>(levelCol) : null), (distributionCol >= 0 ? dr.Field<string>(distributionCol) : null));
                    }
                    if (table.Rows.Count > 0 && !skipFundCode) // don't add empty table rows and don't add fundcodes set to skip --|| tableBuilder.ValueType == TableTypes.Percent
                    {  
                        dt.Rows.Add(jobID, fundCode, sheetName, fieldName + PDIFile.FILE_DELIMITER + "EN", 0, 0, tableBuilder.GetTableString()); // will grab the first column with _EN
                        dt.Rows.Add(jobID, fundCode, sheetName, fieldName + PDIFile.FILE_DELIMITER + "FR", 0, 0, tableBuilder.GetTableStringFrench()); // will grab the first column with _FR
                    }
                }
                else
                    return false;
            }
            gen.SaveFrench();
            return true;  
        }

        public bool AddAggregationTable(DataTable dtDocument, int jobID, string sheetName)
        {
            if (workBook is null)
                return false;

            DataTable table = ExtractTables(sheetName); //curSheet.Cells.ExportDataTableAsString(0, 0, curSheet.Cells.MaxDataRow + 1, curSheet.Cells.MaxDataColumn + 1, true);
            table.TableName = sheetName;


            int fundCodeCol = table.FindDataTableColumn("DocumentCode");
            int numberCol = table.FindDataTableColumn("Row number");

            if (fundCodeCol >= 0 && numberCol >= 0)
            {
                string fundCode = string.Empty;
                string rowNumber = string.Empty;

                DataTable curTable = new DataTable("Current Table");
                curTable.Columns.Add(table.Columns[table.Columns.Count - 1].ColumnName, table.Columns[table.Columns.Count - 1].DataType);

                foreach (DataRow dr in table.Rows)
                {
                    if (fundCode != dr.GetStringValue(fundCodeCol))
                    {
                        if (fundCode != string.Empty) //add English and French Rows to DataTable
                        {
                            dtDocument.Rows.Add(jobID, fundCode, sheetName, sheetName, 0, 0, curTable.DataTabletoXML());
                            curTable.Clear();
                        }
                        fundCode = dr.GetStringValue(fundCodeCol);
                    }
                    rowNumber = dr.GetStringValue(numberCol);

                    curTable.Rows.Add(dr.GetStringValue(table.Columns.Count - 1));
                }
                if (curTable.Rows.Count > 0) // don't add empty table rows --|| tableBuilder.ValueType == TableTypes.Percent
                    dtDocument.Rows.Add(jobID, fundCode, sheetName, sheetName, 0, 0, curTable.DataTabletoXML());
            }
            else if (table.Rows.Count > 0) // handle extracting FundtoBookTableMap which has no DocumentCode or FundCode column - only if there are values
                dtDocument.Rows.Add(jobID, "All", sheetName, sheetName, 0, 0, table.DataTabletoXML(true));

            return true;
        }

        public bool AddMultiCellTable(DataTable dtDocument, Generic gen, RowIdentity rowIdentity, int jobID, string sheetName, string fieldName, SheetType sheetType = SheetType.wsUnknown)
        {
            if (workBook is null)
                return false;

            // TODO: Consider moving this kind of configuration to the Sheet_Index or similar table so it's not hard coded. Optionally it could be client specific - though that's a can of worms we might not want to open

            TableList tableBuilder = new TableList(TableTypes.MultiText);
            tableBuilder.NAValue = "";
            if (sheetType == SheetType.wsSeriesRedesignation || sheetType == SheetType.wsBrokerageCommissions || sheetType == SheetType.wsInvestmentsFund || sheetType == SheetType.wsSoftDollarComissions)
            {
                tableBuilder.TableType = TableTypes.MultiDecimal;
                tableBuilder.NAValue = "-";
            }
            else if (sheetType == SheetType.wsSubsidiarySummary)
            {
                tableBuilder.TableType = TableTypes.MultiPercent;
                tableBuilder.NAValue = "-";
            }
            else if (sheetType == SheetType.wsRemainingYears)
                tableBuilder.TableType = TableTypes.MultiPercent;
            else if (sheetType == SheetType.wsIncomeTaxes)
            {
                tableBuilder.TableType = TableTypes.MultiYear;
                tableBuilder.NAValue = "-";
            }
            else if (sheetType == SheetType.wsSeedMoney || sheetType == SheetType.wsCommitments)
                tableBuilder.TableType = TableTypes.MultiDecimal;

            DataTable table = ExtractTables(sheetName); //curSheet.Cells.ExportDataTableAsString(0, 0, curSheet.Cells.MaxDataRow + 1, curSheet.Cells.MaxDataColumn + 1, true);
            table.TableName = sheetName;

            int fundCodeCol = table.FindDataTableColumn("FundCode");
            int numberCol = table.FindDataTableColumn("Row number");
            List<int> skipCols = new List<int>(0);

            if (fundCodeCol < 0)
                fundCodeCol = table.FindDataTableColumn("DocumentCode");

            if (fundCodeCol >= 0 && numberCol >= 0)
            {
                //enCol = table.FindDataTableColumn("Series_EN");
                //frCol = table.FindDataTableColumn("Series_FR");
                string fundCode = string.Empty;
                string rowNumber = string.Empty;
                if (numberCol >= 0) //enCol >= 0 && frCol >= 0 && 
                {
                    foreach (DataRow dr in table.Rows)
                    {
                        if (fundCode != dr.GetStringValue(fundCodeCol))
                        {
                            if (fundCode != string.Empty) //add English and French Rows to DataTable
                            {
                                dtDocument.Rows.Add(jobID, fundCode, sheetName, fieldName + PDIFile.FILE_DELIMITER + "EN", 0, 0, tableBuilder.GetTableString());
                                dtDocument.Rows.Add(jobID, fundCode, sheetName, fieldName + PDIFile.FILE_DELIMITER + "FR", 0, 0, tableBuilder.GetTableStringFrench());
                                tableBuilder.Clear();
                            }

                            fundCode = dr.GetStringValue(fundCodeCol);
                        }
                        rowNumber = dr.GetStringValue(numberCol);

                        skipCols.Clear();
                        for (int col = numberCol + 1; col < table.Columns.Count; col++)
                        {
                            if (skipCols.Contains(col))
                                continue;

                            if (table.Columns[col].ColumnName.Contains(PDIFile.FILE_DELIMITER + "EN"))
                            {
                                string frTempValue = null;
                                int frTempCol = dr.FindDataRowColumn(table.Columns[col].ColumnName.Replace(PDIFile.FILE_DELIMITER + "EN", PDIFile.FILE_DELIMITER + "FR")); // look for a matching French Column
                                if (frTempCol > 0)
                                {
                                    skipCols.Add(frTempCol);
                                    frTempValue = dr.GetStringValue(frTempCol);
                                }
                                if (frTempValue.IsNaOrBlank() && !dr.GetStringValue(col).IsNaOrBlank()) // If there was no French column OR the value was N/A then lookup the French value using the English - only if the English value isn't N/A or blank
                                {
                                    rowIdentity.DocumentCode = fundCode;
                                    frTempValue = gen.GenerateFrench(dr.GetStringValue(col), rowIdentity, jobID, fieldName);
                                }
                                tableBuilder.AddMultiCell(rowNumber, dr.GetStringValue(col), frTempValue);
                            }
                            else // the column likely contains a date, whole number or decimal number
                                tableBuilder.AddMultiCell(rowNumber, dr.GetStringValue(col), dr.GetStringValue(col));

                        }
                        // 20211122 - SK - this was something missed in US 8290 initially - when the French value is N/A a lookup attempt on the English value should be made - now passing in Generic and RowIdenity classes to handle the lookup and associated missing French

                        
                        
                        //tableBuilder.AddValidation(dr.Field<string>(valueCol), en, fr, dr.Field<string>(numberCol), (levelCol >= 0 ? dr.Field<string>(levelCol) : null), (distributionCol >= 0 ? dr.Field<string>(distributionCol) : null));

                    }
                    if (table.Rows.Count > 0) // don't add empty table rows --|| tableBuilder.ValueType == TableTypes.Percent
                    {
                        dtDocument.Rows.Add(jobID, fundCode, sheetName, fieldName + PDIFile.FILE_DELIMITER + "EN", 0, 0, tableBuilder.GetTableString());
                        dtDocument.Rows.Add(jobID, fundCode, sheetName, fieldName + PDIFile.FILE_DELIMITER + "FR", 0, 0, tableBuilder.GetTableStringFrench());
                    }
                }

            }
            return true;
        }

        //find the rest of the columns

        /// <summary>
        /// For all sheets in workBook get the SheetType and add any handled table types
        /// </summary>
        /// <param name="dtDocument">The DataTable to add ouput too</param>
        /// <param name="fileName">FileName class</param>
        /// <param name="gen">Generic class</param>
        /// <param name="jobID">The optional jobID</param>
        /// <returns>True if the workbook isn't null</returns>
        public bool AddTables(DataTable dtDocument, PDIFile fileName, Generic gen, int jobID = -1)
        {
            if (workBook is null)
                return false;

            jobID = (jobID >= 0 ? jobID : (int)fileName.JobID);

            SheetType sheetType = SheetType.wsUnknown;
            RowIdentity rowIdentity = new RowIdentity(fileName.DocumentTypeID.HasValue ? (int)fileName.DocumentTypeID : -1, fileName.ClientID.HasValue ? (int)fileName.ClientID : -1, fileName.LOBID.HasValue ? (int)fileName.LOBID : -1, "");
            foreach (Excel.Worksheet curSheet in workBook.Worksheets)
            {
                sheetType = WorksheetValidation.GetSheetType(curSheet.Name);
                switch (sheetType)
                {
                    case SheetType.ws16:
                    case SheetType.ws17:
                    case SheetType.ws40:
                    case SheetType.wsTheFund:
                    case SheetType.wsTerminated:
                    case SheetType.wsNewSeries:
                        AddGenericTable(dtDocument, gen, rowIdentity, jobID, curSheet.Name, curSheet.Name);
                        break;
                    case SheetType.wsNumInvest:
                        AddGenericTable(dtDocument, gen, rowIdentity, jobID, curSheet.Name, fileName.DocumentType + "23");
                        
                        break;
                    case SheetType.ws10K:
                        AddGenericTable(dtDocument, gen, rowIdentity, jobID, curSheet.Name, fileName.DocumentType + "10", gen.loadShortMonths(),true);
                        break;
                    case SheetType.wsManagmentFees:
                    case SheetType.wsFixedAdminFees:
                    case SheetType.wsSoftDollarComissions:
                    case SheetType.wsBrokerageCommissions:
                    case SheetType.wsInvestmentsFund:
                    case SheetType.wsSeriesRedesignation:
                    case SheetType.wsSeriesMerger:
                    case SheetType.wsRemainingYears:
                    case SheetType.wsIncomeTaxes:
                    case SheetType.wsFundMerger:
                    case SheetType.wsFairValue:
                    case SheetType.wsSeedMoney:
                    case SheetType.wsSubsidiarySummary:
                    case SheetType.wsSubsidiaryDetail:
                    case SheetType.wsCommitments:
                    case SheetType.wsFullName:
                    case SheetType.wsFundTransfer:
                    case SheetType.wsNewlyOffered:
                    case SheetType.wsChangeFundName:
                    case SheetType.wsManagementPersonnel:
                    case SheetType.wsKeyManagementPersonnelBrokerageCommissions:
                    case SheetType.wsFundMergers:
                    case SheetType.wsNewFunds:
                    case SheetType.wsNoLongerOffered:
                    case SheetType.wsYearByYear:
                    case SheetType.wsAnnualCompoundReturns:
                    case SheetType.wsUnfundedLoanCommitments:
                    case SheetType.wsComparisonOfNetAsset:
                        AddMultiCellTable(dtDocument, gen, rowIdentity, jobID, curSheet.Name, curSheet.Name, sheetType);
                        break;

                    case SheetType.wsBookFundMap:
                    case SheetType.wsFundtoBookTableMap:
                        AddAggregationTable(dtDocument, jobID, curSheet.Name);
                        break;
                    default:
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Build the data for the 3 tabs 16, 17 and 40 - does not depend on specific column names
        /// </summary>
        /// <returns></returns>
        /// [Obsolete("Use Add Tables")]
        public DataTable BuildTables()
        {
            DataTable outputTable = new DataTable("TableOutput");
            outputTable.PrimaryKey = new DataColumn[] { outputTable.Columns.Add("FundCode", typeof(string)) };
            TableList tableBuilder = new TableList();

            if (workBook is null)
                return outputTable;

            SheetType sheetType = SheetType.wsUnknown;
            foreach (Excel.Worksheet curSheet in workBook.Worksheets)
            {
                sheetType = WorksheetValidation.GetSheetType(curSheet.Name);
                if (sheetType == SheetType.ws16 || sheetType == SheetType.ws17 || sheetType == SheetType.ws40) 
                {

                    tableBuilder.Clear();
                    DataTable table = curSheet.Cells.ExportDataTableAsString(0, 0, curSheet.Cells.MaxDataRow + 1, curSheet.Cells.MaxDataColumn + 1, true);
                    table.TableName = curSheet.Name;

                    //add the new columns to the outputTable
                    outputTable.Columns.Add(curSheet.Name + PDIFile.FILE_DELIMITER + "EN", typeof(string));
                    outputTable.Columns.Add(curSheet.Name + PDIFile.FILE_DELIMITER + "FR", typeof(string));

                    int fundCodeCol = table.FindDataTableColumn("FundCode");
                    int numberCol = table.FindDataTableColumn("number");
                    int valueCol, enCol, frCol, levelCol;
                    
                    if (fundCodeCol >= 0 && numberCol >= 0)
                    {
                        //find the rest of the columns
                        valueCol = table.FindDataTableColumn("Value");
                        enCol = table.FindDataTableColumn("en-CA");
                        frCol = table.FindDataTableColumn("fr-CA");
                        levelCol = table.FindDataTableColumn("Level");

                        string fundCode = string.Empty;
                        foreach (DataRow dataRow in table.Rows)
                        {
                            if (fundCode != dataRow.Field<string>(fundCodeCol))
                            {
                                if (fundCode != string.Empty)
                                    AddTableOutput(outputTable, fundCode, tableBuilder, table.TableName);
                                    

                                fundCode = dataRow.Field<string>(fundCodeCol);
                            }
                            tableBuilder.AddValidation(dataRow.Field<string>(valueCol), dataRow.Field<string>(enCol), dataRow.Field<string>(frCol), dataRow.Field<string>(numberCol), (levelCol >= 0 ? dataRow.Field<string>(levelCol) : null));
                           
                        }
                        if (fundCode != string.Empty)
                            AddTableOutput(outputTable, fundCode, tableBuilder, curSheet.Name); 
                    }
                }
            }
            
            outputTable.DefaultView.Sort = "FundCode";
            return outputTable.DefaultView.ToTable();
        }

        // TODO: This should probably be a join on tables in a data set - or LINQ Cross-Table Queries
        /// <summary>
        /// The FundCode has changed build the tables for English and French and output them into the output table.
        /// </summary>
        /// <param name="outputTable">The output DataTable</param>
        /// <param name="fundCode">The FundCode before it has changed</param>
        /// <param name="tableBuilder">The current TableList class</param>
        /// <param name="tableName">The name of the table - for column names</param>
        private void AddTableOutput(DataTable outputTable, string fundCode, TableList tableBuilder, string tableName)
        {
            //find the fundcode
            DataRow[] foundRows = outputTable.Select($"FundCode = '{fundCode.EscapeSQL()}'");
            DataRow foundRow;
            if (foundRows.Length == 1)
                foundRow = foundRows[0];
            else
                foundRow = outputTable.Rows.Add(fundCode);

            int tblColEn = outputTable.FindDataTableColumn(tableName + PDIFile.FILE_DELIMITER + "EN");
            int tblColFr = outputTable.FindDataTableColumn(tableName + PDIFile.FILE_DELIMITER + "FR");
            if (tblColEn >= 0 && tblColFr >= 0)
            {
                //foundRow.SetField<string>("FundCode", fundCode);
                foundRow.SetField<string>(tblColEn, tableBuilder.GetTableString());
                foundRow.SetField<string>(tblColFr, tableBuilder.GetTableStringFrench());
            }
            tableBuilder.Clear();
        }

        public bool ConvertAddress(string address, out int row, out int col)
        {
            try
            {
                Excel.CellsHelper.CellNameToIndex(address, out row, out col);
                return true;
            }
            catch (Exception)
            {
                row = -1;
                col = -1;
                return false;
            }  
        }
        
        public string ConvertIndexToAddress(int row, int col)
        {
            try
            {
                if (row < 0) // if the row isn't set just return the column letter
                    return Regex.Replace(Excel.CellsHelper.CellIndexToName(1, col), @"\d", "");
                else if (col < 0) // if the column isn't set just return the row number
                    return (row + 1).ToString();
                else
                    return Excel.CellsHelper.CellIndexToName(row, col);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool AppendDataTable(DataTable dt, string sheetName = null)
        {
            if (workBook != null)
            {
                try
                {
                    Excel.Worksheet ws = workBook.Worksheets.Add(sheetName ?? dt.TableName);
                    ws.Cells.ImportDataTable(dt, true, "A1");
                    ws.AutoFitColumns();

                    Excel.Style style = new Excel.Style();
                    //style.Custom = "yyyy-mm-dd";
           
                    
                    //for (int i = 0; i <= ws.Cells.MaxDataColumn; i++)
                    //{
                    //    if (dt.Columns[i].DataType == typeof(DateTime))
                    //    {
                    //        ws.Cells.Columns[i].ApplyStyle(style, new Excel.StyleFlag());
                    //    }

                    //}

                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to add DataTable to WorkBook - {e.Message}");
                    return false;
                }
            }
            else
                return false;

        }

        public bool SaveChanges(Stream stream)
        {
            if (workBook != null)
            {
                workBook.Save(stream, Excel.SaveFormat.Auto);
                return true;
            }
            return false;
        }
       

        public int FindColumn(Excel.Worksheet ws, string searchValue)
        {
            Excel.Cell cell = FindValue(ws, searchValue);
            if (cell != null)
                return cell.Column;

            return -1;
        }

        public Excel.Cell FindValue(Excel.Worksheet ws, string searchValue)
        {
            Excel.FindOptions findOptions = new Excel.FindOptions
            {
                SeachOrderByRows = true,
                LookInType = Excel.LookInType.Values,
                LookAtType = Excel.LookAtType.Contains
            };

            Excel.CellArea ca = new Excel.CellArea
            {
                StartRow = 0,
                StartColumn = 0,
                EndRow = ws.Cells.MaxDataRow,
                EndColumn = ws.Cells.MaxDataColumn
            };

            findOptions.SetRange(ca);
            return ws.Cells.Find(searchValue, null, findOptions);
        }
       
        /// <summary>
        /// Create a column name based on the combination of two header rows
        /// </summary>
        /// <param name="fullColumn">The combined header rows</param>
        /// <returns>The constructed column name</returns>
        public static string GetColumnName(string fullColumn)
        {
            if (fullColumn.IndexOf(PDIFile.FILE_DELIMITER) >= 0 && fullColumn.IndexOf("<") >= 0)
                return fullColumn.Substring(fullColumn.IndexOf(PDIFile.FILE_DELIMITER) + 1).Replace("<", "").Replace(">", "");
            
           TextInfo ti = CultureInfo.CurrentCulture.TextInfo;
            string temp = Regex.Replace(ti.ToTitleCase(fullColumn), @"\s+", "");
            string temp2 = Regex.Replace(temp, @"(?<=Row|Field|minus)([0-9])+", m => m.Value.PadLeft(2, '0'), RegexOptions.IgnoreCase);
            return temp2;
        }

        public static int GetNumberFromName(string fullColumn)
        {
            //string temp = Regex.Replace(fullColumn, @"\s+", "");
            Match tempMatch = Regex.Match(fullColumn, @"(?<=Row|Field|Minus)\s*([0-9])+", RegexOptions.IgnoreCase);
            if (tempMatch != null)
            {
                if (int.TryParse(tempMatch.Value, out int val))
                    return val;
            }

            return 0;
        }


        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    workBook.Dispose();
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~AsposeLoader()
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

