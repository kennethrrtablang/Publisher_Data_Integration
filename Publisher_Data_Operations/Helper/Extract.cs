using System;
using System.Collections.Generic;
using System.Linq;
using Publisher_Data_Operations.Extensions;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace Publisher_Data_Operations.Helper
{
    public class Extract
    {
        int _jobID = -1;
        AsposeLoader _loader = null;
        PDIStream _processStream = null;
        DBConnection _dbCon = null;
        Generic _gen = null;
        Logger _log = null;

        public Extract(PDIStream processStream, object con, Logger log, int jobID = -1)
        {
            _processStream = processStream;
            _loader = new AsposeLoader(_processStream.SourceStream);
            _log = log;
            if (con.GetType() == typeof(DBConnection))
                _dbCon = (DBConnection)con;
            else
                _dbCon = new DBConnection(con);

            _gen = new Generic(_dbCon, _log);

            if (_processStream.PdiFile.JobID.HasValue)
                _jobID = (int)_processStream.PdiFile.JobID;
            else
            {
                _jobID = jobID;
                _processStream.PdiFile.JobID = _jobID;
            }
            //TODO: can't extract if FileName doesn't have a Job_ID
        }

        public bool RunExtract()
        {
            bool retVal = false;
            if (_loader != null)
            {

                switch (_processStream.PdiFile.GetDataType)
                {
                    case DataTypeID.STATIC:
                        retVal = ExtractStatic();
                        break;
                    case DataTypeID.FSMRFP:
                        retVal = ExtractFSMRFP();
                        break;
                    case DataTypeID.BAU:
                        retVal = ExtractBAU();
                        break;
                    case DataTypeID.BNY:
                        retVal = ExtractBNY();
                        break;
                    default:
                        Logger.AddError(_log, $"The Data Type '{Enum.GetName(typeof(DataTypeID), _processStream.PdiFile.GetDataType)}' extract is not implemented");
                        retVal = false;
                        break;
                }
            }
            return retVal;
        }

        /// <summary>
        /// Confirm that the rowtype is where it's expected
        /// </summary>
        /// <param name="dt"></param>
        /// <param name="sourceHasRowType"></param>
        /// <param name="formatColumnName"></param>
        /// <returns></returns>
        public bool CheckRowTypeColumn(DataTable dt, bool sourceHasRowType, string formatColumnName)
        {
            if (dt is null || dt.Columns.Count < 1)
                return false;

            // look in the last column and see if it has any "Level_." text
            DataRow[] rowTypes = dt.Select($"{dt.Columns[dt.Columns.Count - 1].ColumnName} LIKE 'Level%'"); //bug17112 - there are too many rowtype variations - just look for Level - this might cause an issue with no rowtype required but it's detected due to the word Level being first in some row in the last column
            if (rowTypes.Length < 1 && sourceHasRowType)
            {
                Logger.AddError(_log, $"RowType was not detected in sheet {dt.TableName} but rowType was expected - skipping extract.");
                return false;
            }
            else if (rowTypes.Length > 0 && !sourceHasRowType)
            {
                Logger.AddError(_log, $"RowType detected in sheet {dt.TableName} but rowType was not expected - skipping extract.");
                return false;
            }
            else if (rowTypes.Length > 0 && dt.Columns[dt.Columns.Count - 1].ColumnName != formatColumnName)
            {
                Logger.AddError(_log, $"RowType detected in sheet {dt.TableName} but column has incorrect name - skipping extract.");
                return false;
            }
            return true;
        }

        private bool ExtractFSMRFP()
        {
            if (_processStream.PdiFile.JobID.HasValue && _processStream.PdiFile.JobID > 0 && _processStream.PdiFile.ClientID.HasValue && _processStream.PdiFile.IsValid)
            {
                //_jobID = (int)pdiStream.PdiFile.JobID;
                //AsposeLoader excelLoader = new AsposeLoader(processStream.SourceStream);
                DataTable dtClientSheets = LoadClientSheets((int)_processStream.PdiFile.ClientID, (int)_processStream.PdiFile.DocumentTypeID);
                DataTable dtDataStaging = LoadDataStaging((int)_processStream.PdiFile.JobID);

                string formatColumnName = Generic.FSMRFP_ROWTYPE_COLUMN;
                int sheetCount = 0;
                foreach (DataRow dr in dtClientSheets.Rows) // loop through all available sheets for the client
                {
                    string sheetName = dr.Field<string>("Sheet_Name");
                    string fieldName = dr.Field<string>("Field_Name");
                    bool hasRowType = dr.IsNull("Has_RowType") ? false : dr.Field<bool>("Has_RowType");
                    bool clearUnused = dr.IsNull("Clear_Unused_Fields") ? false : dr.Field<bool>("Clear_Unused_Fields");
                    bool disableFormatting = dr.IsNull("Disable_Format_Extraction") ? false : dr.Field<bool>("Disable_Format_Extraction");
                    int firstDataRow = 1;
                    if (!dr.IsNull("First_Data_Row"))
                        firstDataRow = dr.Field<int>("First_Data_Row");

                    DataTable dt = null;
                    if (disableFormatting)
                    {
                        dt = _loader.ExtractTables(sheetName, false, true, firstDataRow);
                        if (dt != null && dt.Columns.Count > 0 && hasRowType)
                            dt.Columns[dt.Columns.Count - 1].ColumnName = formatColumnName;
                    }

                    else
                        dt = _loader.ExtractWithFormatting(sheetName, hasRowType, true, true, firstDataRow, formatColumnName, _log); //excelLoader.ExtractTables(sheetName, false, true, firstDataRow);

                    DataTable dtSub = dt;
                    //excelLoader.ConsolePrintTable(dt); //debugging info

                    if (dt != null && dt.Rows.Count > 0)
                    {
                        sheetCount++;
                        if (!CheckRowTypeColumn(dt, hasRowType, formatColumnName))
                        {
                            //Logger.AddError(_log, $"RowType column is required but was not found in last column in {sheetName}");
                            continue;
                        }

                        /********************************************************************************************************************************************************
                        * Rules for Field_Name
                        *   Single field - single field name string - "F16"
                        *   Multiple fields - separated by commas
                        *       Field Format - [Field]|[From]|[To - Blank if unused]|[Option - Optional]
                        *           [Field] - Either a single field or a field with a range {{a-z}} - range can be a maximum length of 26
                        *               will cause a duplicate range for each generated character to be created before processing 
                        *               - range can loop - {{i-e}} will loop from z to a and stop at e (skip f and h)
                        *               - can now have multiple ranges M11~M12{{a-q}} will alternate two fields and range (skip f and h)
                        *               - When the [Option] is SplitPairs the duplicate range will not be generated but be used as needed
                        *               
                        *           [From] or [To] - can be either a row number, or an Excel A1C1 address, or a text string to search for
                        *               Row number - the matching row number of the DataTable NOT the Excel source    
                        *               Text - This option searches for the provided from and to text in the extracted data table and creates a sub DataTable using the found range 
                        *                   text can have offsets appended to them with {{+n}} or {{-n}} like "Series{{-1}}" for the row before the first row containing "Series"
                        *                   Multiple search text can be separated with ~ "Series{{-1}}~Letter{{-1}}
                        *               Address - in A1C1 notation and can include a single cell address "A3" - will be a single cell if [To] is blank otherwise a range
                        *                   Note that the First_Data_Row must be 0 when using address notation or the rows will not match Excel
                        *               Text replacement - If the text "{{cMax}}" or "{{rMax}}" is included in the address it will be replaced with the Excel column or DataTable row count respectively
                        *                   if the text {{blank}} {{nonBlank}} {{lastRow}} and the +n or -n like {{blankrow-2}} version of each are 
                        *                   present the appropriate values will be determined and the address updated
                        *                   if the replaced text is part of an A1C1 address the row will be increased by 1 to compensate for the 1 based nature of Excel rows
                        *               Mix and match - it's possible to use combinations of any one of the three options in the two fields
                        *               
                        *           [Option] 
                        *               Text - indicates that the data should be treated as a text field and an xml table will not be created - output will be wrapped in <p></p> tags for each row unless there is only one row
                        *               Group - indicates that the data has a specific format requirement and will be grouped (SOI Bond specific)    
                        *               Greedy - When searching for a value or values - if not found return the max row
                        *               FromTop - Restart the search from the beginning of the file for the current section
                        *               SplitPairs - Special for F8a-c (currently) split the incoming data based on the number of column pairs {{maxColumnPairs}}
                        *               SplitSeries - Update for F8a-c split the data into tables based on series and keep the total column with other columns  and series pairs (2 years) together while allowing single year (new) series {{maxColumnsPerTable~HeaderRow~Total Column Text}} - HeaderRow is the Excel row (1 based)
                        *               
                        *           Appending fields is possible and will assume that the first row of any following tables should NOT be included when appending to an existing field of the same name
                        *           
                        *       Note that number of sections is 3 and the maximum is 4 - the 4th can be omitted but the third must be included even if blank
                        *           
                        *           The options must be input from top to bottom - first to last unless using FromTop [Option]
                        *           The option sections using lookups can't overlap beyond using the +1 and -1 offsets
                        *           Putting in an address without lookups will reset {{blank}} {{nonBlank}} {{lastRow}} to search starting at the end of the address
                        *       
                        ********************************************************************************************************************************************************/
                        fieldName = ConvertRange(fieldName); // if a range is included in the address it will be fully expanded before processing - added handling for expanding a range that is already in a multipart field_name
                        int prevRow = 0;
                        foreach (string fieldList in fieldName.Split(','))
                        { 
                            string[] fieldParts = fieldList.Split('|');
                            string fieldNamePart = fieldParts[0].Trim();
                            if (fieldNamePart == "M12fb")
                            {
                                _ = string.Empty;
                            }

                            if (fieldParts.Length == 4 && fieldParts[3] == "FromTop")
                                prevRow = 0;    // reset the starting point when the option is FromTop

                            if (prevRow >= dt.Rows.Count) // this should only happen when repeating an unknown number of sections
                                break;

                            if (fieldParts.Length >= 3)
                            {
                                // to contains the last row the previous section ended at so we use that to determine the next blank (and blank-1), last row + 1, and non blank
                                int lastRowPlusOne = prevRow + 1;
                                int? blank = null;
                                int? nonBlank = null;
                                object[] items = null;

                                for (int r = lastRowPlusOne; r < dt.Rows.Count; r++)
                                {
                                    items = dt.Rows[r].ItemArray;
                                    if (!blank.HasValue && items.All(dCol => dCol is null || dCol.ToString().Trim() == string.Empty))
                                        blank = r; // +1 to convert to Excel 1 based row address - now done in object setup
                                    if (!nonBlank.HasValue && items.Any(dCol => dCol != null && dCol.ToString().Trim() != string.Empty))
                                        nonBlank = r;

                                    if (blank.HasValue && nonBlank.HasValue)
                                        break;
                                }

                                object rep = new { cMax = _loader.ConvertIndexToAddress(-1, dt.Columns.Count), blank = blank ?? dt.Rows.Count, nonBlank = nonBlank ?? dt.Rows.Count, lastRow = prevRow, rMax = dt.Rows.Count - 1 }; // object to handle replacement names and values - Cmax is left at 1 based index but all other are 0 based
                                string val1 = ReplaceAddress(fieldParts[1].Trim(), rep);
                                string val2 = ReplaceAddress(fieldParts[2].Trim(), rep);

                                int fromRow = -1;
                                int fromCol = 0;
                                int toRow = -1;
                                int toCol = dt.Columns.Count;

                                int plusOffset = 0;
                                // Now filedParts 1 and optionally fieldParts 2 contain either addresses, text or row number
                                ConvertAddress(val1, _loader, dt, out fromRow, out fromCol, prevRow);
                                PlusOffset(fieldParts[1], out plusOffset);
                                ConvertAddress(val2, _loader, dt, out toRow, out toCol, Math.Max(prevRow, fromRow + (plusOffset * -1) + 1), true, (fieldParts.Length == 4 && fieldParts[3] == "Greedy")); // bug17108 fix caused another bug which is that when doing the older style text search where the header is a possible end value the fromRow is already on the header so searching there results in the header being found and no table produced.

                                if (fromRow > -1 && fromCol > -1 && toRow < 0) // single Address
                                {
                                    dtSub = new DataTable($"{dt.TableName}-{fromRow}.{fromCol}"); // even if it's out of range setup the new dtSub or the old one could be used
                                    if (fromCol < dt.Columns.Count && fromRow < dt.Rows.Count)
                                    {
                                        dtSub.Columns.Add(dt.Columns[fromCol].ColumnName, dt.Columns[fromCol].DataType);
                                        dtSub.Rows.Add(dt.Rows[fromRow][fromCol]);
                                        prevRow = fromRow;
                                    }
                                    else
                                        Logger.AddError(_log, $"Could not locate address on sheet: {sheetName} address: {fieldParts[1]} field: {fieldNamePart}");// - Out of Rows or Columns at {toRow}_{fromCol}");
                                }
                                else if (fromRow >= 0 && toRow >= 0) // from - to address
                                {
                                    dtSub = new DataTable($"{dt.TableName}-{fromRow}.{fromCol}:{toRow}.{toCol}");
                                    for (int c = fromCol; c <= toCol; c++)
                                        if (c < dt.Columns.Count)
                                            dtSub.Columns.Add(dt.Columns[c].ColumnName, dt.Columns[c].DataType);
                                        //else
                                        //{
                                        //    Logger.AddError(_log, $"Could not locate address on sheet {sheetName} {fieldParts[1]}{fieldParts[2]} for {fieldNamePart} - Out of Columns at {c}");
                                        //    c = toCol + 1;
                                        //}
                                            

                                    if (dt.Columns.Contains(formatColumnName) && !dtSub.Columns.Contains(formatColumnName))
                                        dtSub.Columns.Add(dt.Columns[formatColumnName].ColumnName, dt.Columns[formatColumnName].DataType);

                                    List<string> itemRow = new List<string>(toCol - fromCol);

                                    for (int r = fromRow; r <= toRow; r++)
                                    {
                                        if (r >= dt.Rows.Count)
                                        {
                                            Logger.AddError(_log, $"Could not locate row on sheet: {sheetName} address: {fieldParts[1]}|{fieldParts[2]} field: {fieldNamePart} - Out of rows at {r}");
                                            break;
                                        }
                                        itemRow.Clear();
                                        for (int c = fromCol; c <= toCol; c++)
                                            if (c < dt.Columns.Count)
                                                itemRow.Add(dt.Rows[r][c].ToString());
                                            else
                                            {
                                                Logger.AddError(_log, $"Could not locate column on sheet: {sheetName} address: {fieldParts[1]}|{fieldParts[2]} field: {fieldNamePart} - Out of columns at {c} on row {r}");
                                                c = toCol + 1;
                                            }
                                                

                                        if (dtSub.Columns.Contains(formatColumnName) && dt.Columns[formatColumnName].Ordinal > toCol)
                                            itemRow.Add(dt.Rows[r][formatColumnName].ToString());

                                        dtSub.Rows.Add(itemRow.ToArray());
                                    }
                                    prevRow = Math.Max(fromRow, toRow); // changed to max due to CurrentStartTable! EndTable! potentially having 0 rows which results in the to row being less than the from row - trying to prevent potential loops
                                }
                                else // section not found
                                    dtSub = new DataTable($"{dt.TableName}-NotFound");
                            }
                            // if dtSub is dt - OR we have a customized dtSub add it to the data staging DataTable
                            if (dtSub.Rows.Count > 0)
                            {
                                Tuple<bool, bool, int, bool> settings = new Tuple<bool, bool, int, bool>((fieldParts.Length == 4 && fieldParts[3] == "Group"), dr.IsNull("Keep_Blank_Columns") ? false : dr.Field<bool>("Keep_Blank_Columns"), dr.IsNull("Minimum_Output_Columns") ? 0 : dr.Field<int>("Minimum_Output_Columns"), dr.IsNull("Keep_Blank_Rows") ? false : dr.Field<bool>("Keep_Blank_Rows"));

                                if (fieldParts.Length == 4 && fieldParts[3].StripTagsExceptAlpha() == "text") // current field is a text field - don't process it through BuildTable
                                { // special processing for M12fb done
                                    dtDataStaging.Rows.Add(new object[] { _processStream.PdiFile.JobID, _processStream.PdiFile.Code, sheetName, fieldNamePart, 0, 0, fieldParts[0] == "M12fb" ? BuildText(dtSub).Replace("&amp;", "&") : BuildText(dtSub) });
                                }
                                else if (fieldParts.Length == 4 && fieldParts[3].Contains("SplitPairs")) // handling for split pairs - does not support appending
                                {
                                    // the incoming table should be considered to contain pairs of columns and depending on the number we will need to create different tables
                                    // the values on SplitPairs will indicate the maximum number of paired columns per table and the special handling column indicator (Total)
                                    bool localRowType = dtSub.Columns.Contains(Generic.FSMRFP_ROWTYPE_COLUMN);
                                    Match m = Regex.Match(fieldParts[3], @"^[\S]*" + Regex.Escape(Generic.FLAGPRE) + @"([\d]+)" + Regex.Escape(Generic.FLAGPOST));
                                    if (m.Success)
                                    {
                                        int maxColumnPairs = Convert.ToInt32(m.Groups[1].Value);

                                        int dataColumns = dtSub.Columns.Count - 1 - (localRowType ? 1 : 0);
                                        int dataColumnPairs = dataColumns / 2;

                                        if (dataColumns % 2 != 0) // there shouldn't be a remainder as the columns are in pairs
                                            Logger.AddError(_log, $"SP01: There was an unexpected number of columns in the data when working on {fieldNamePart} from sheet {sheetName} - extraction may be incorrect");

                                        string[] fieldNames = ConvertRange(fieldNamePart + "|").Replace("|", string.Empty).Split(','); // convertRange requires the "|" so add it and then remove it before splitting to get the fields names for SplitPairs

                                        if (dataColumnPairs <= maxColumnPairs) // only a single table so no fancy work needed
                                            dtDataStaging.Rows.Add(new object[] { _processStream.PdiFile.JobID, _processStream.PdiFile.Code, sheetName, fieldNames[0], 0, 0, BuildTable(dtSub, settings) });
                                        else
                                        {
                                            int tablesRequired = ((dataColumnPairs - 1) / maxColumnPairs) + 1; // http://www.cs.nott.ac.uk/~rcb/G51MPC/slides/NumberLogic.pdf
                                            // we now know how many tables we need so edit copies of the dtSub table to have the required columns.
                                            if (tablesRequired > fieldNames.Count())
                                                Logger.AddError(_log, $"SP02: There were more column pairs included than output fields available when working on {fieldNamePart} from sheet {sheetName} - output will end with field {fieldNames[fieldNames.Count() - 1]}");

                                            for (int t = 1; t <= tablesRequired && t <= fieldNames.Count(); t++)
                                            {
                                                DataTable dtCopy = dtSub.Copy();
                                                for (int c = dtSub.Columns.Count - 1; c > 0; c--) // iterate backwards through the table deleting every column not needed in the current table except for rowtype (if it exists) and the first column
                                                {
                                                    if (dtCopy.Columns[c].ColumnName != Generic.FSMRFP_ROWTYPE_COLUMN)
                                                    {
                                                        if (c > t * maxColumnPairs * 2 || c <= (t - 1) * maxColumnPairs * 2)
                                                            dtCopy.Columns.Remove(dtSub.Columns[c].ColumnName);
                                                    }
                                                }
                                                dtDataStaging.Rows.Add(new object[] { _processStream.PdiFile.JobID, _processStream.PdiFile.Code, sheetName, fieldNames[t - 1], 0, 0, BuildTable(dtCopy, settings) });
                                            }
                                        }
                                    }
                                    else
                                        Logger.AddError(_log, $"SP03: Could not parse SplitPairs extraction rule {fieldParts[3]} for sheet {sheetName} - unable to extract {fieldNamePart}");
                                }
                                else if (fieldParts.Length == 4 && fieldParts[3].Contains("SplitSeries")) // handling for split series - does not support appending
                                {
                                    bool localRowType = dtSub.Columns.Contains(Generic.FSMRFP_ROWTYPE_COLUMN);
                                    Match m = Regex.Match(fieldParts[3], @"^[\S]*" + Regex.Escape(Generic.FLAGPRE) + @"(.+)" + Regex.Escape(Generic.FLAGPOST));
                                    if (m.Success)
                                    {
                                        int maxColumns = 6; // set default options
                                        int headerRow = 1;
                                        string totalText = "Total Series";
                                        if (m.Groups[1].Value.Contains("~")) // try to extract options from the option modifier
                                        {
                                            string[] configSections = m.Groups[1].Value.Split('~');
                                            if (configSections.Length > 0) int.TryParse(configSections[0], out maxColumns);
                                            if (configSections.Length > 1) int.TryParse(configSections[1], out headerRow);
                                            if (configSections.Length > 2) totalText = configSections[2];
                                        }
                                        else
                                            int.TryParse(m.Groups[1].Value, out maxColumns); // assume if there is no separator that the maxColumns option was entered

                                        if (headerRow > 0) headerRow--; // convert from 1 based to 0 based;

                                        // before we do the DataColumn counting make clean up the source DataTable columns according to the settings otherwise the blank columns could be removed after splitting into tables and the division of series will not be as planned
                                        if (!settings.Item2)
                                            dtSub.RemoveBlankColumns(settings.Item3 + (localRowType ? 1 : 0)); // if the table has a rowtype keep an extra column

                                        int dataColumns = dtSub.Columns.Count - 1 - (localRowType ? 1 : 0); // assumes 1 row of non-series data

                                        string[] fieldNames = ConvertRange(fieldNamePart + "|").Replace("|", string.Empty).Split(','); // convertRange requires the "|" so add it and then remove it before splitting to get the fields names for SplitPairs

                                        if (dataColumns <= maxColumns) // only a single table so no fancy work needed
                                            dtDataStaging.Rows.Add(new object[] { _processStream.PdiFile.JobID, _processStream.PdiFile.Code, sheetName, fieldNames[0], 0, 0, BuildTable(dtSub, settings) });
                                        else
                                        {
                                            // we now need to split up the data into multiple tables
                                            // Series can be 1 or 2 columns
                                            // Series cannot be split across tables
                                            // Total cannot be orphaned
                                            int tablesRequired = ((dataColumns - 1) / maxColumns) + 1; // http://www.cs.nott.ac.uk/~rcb/G51MPC/slides/NumberLogic.pdf // this might not be totally accurate due to the above rules but if this number is exceeded it's for sure impossible

                                            if (tablesRequired > fieldNames.Count())
                                                Logger.AddError(_log, $"SS01: There were more column pairs included than output fields available when working on {fieldNamePart} from sheet {sheetName} - output will end with field {fieldNames[fieldNames.Count() - 1]}");

                                            int tableSpace = maxColumns;
                                            int curTable = 0;
                                            for (int c = 1; c < dtSub.Columns.Count - (localRowType ? 1 : 0); c++)
                                            {
                                                // making use of the extended properties we are going to scan the table to identify single or series groups and assign them to tables until full
                                                if (c + 1 < dtSub.Columns.Count - (localRowType ? 1 : 0) && dtSub.Rows[headerRow][c] == dtSub.Rows[headerRow][c + 1] || dtSub.Rows[headerRow][c + 1].ToString().IsNaOrBlank()) //determine if these two columns are paired - either the headerRow text is the same or the second headerRow is blank
                                                {
                                                    if (tableSpace - 2 < 0) // if there is less than 2 columns left in the current table move on to the next
                                                    {
                                                        curTable++;
                                                        tableSpace = maxColumns;
                                                    }
                                                    dtSub.Columns[c].ExtendedProperties.Add("table", curTable);
                                                    dtSub.Columns[c].ExtendedProperties.Add("series", dtSub.Rows[headerRow][c]);
                                                    dtSub.Columns[c + 1].ExtendedProperties.Add("table", curTable);
                                                    dtSub.Columns[c + 1].ExtendedProperties.Add("series", dtSub.Rows[headerRow][c]);
                                                    tableSpace -= 2; // adjust the number of columns available
                                                    c++; // skip over the paired column
                                                }
                                                else
                                                {
                                                    if (tableSpace - 1 < 0) // there has to be at least one column left in the table
                                                    {
                                                        curTable++;
                                                        tableSpace = maxColumns;
                                                    }
                                                    dtSub.Columns[c].ExtendedProperties.Add("table", curTable);
                                                    dtSub.Columns[c].ExtendedProperties.Add("series", dtSub.Rows[headerRow][c]);
                                                    tableSpace--; // adjust the number of columns available
                                                }
                                            }

                                            string totalTable = (fieldNames.Count() - 1).ToString(); // default the totalTable to the last available table
                                            for (int c = dtSub.Columns.Count - 1 - (localRowType ? 1 : 0); c > 0; c--) // look backwards through the tables extended properties to find series with the totalText
                                            {
                                                if (dtSub.Columns[c].ExtendedProperties.Contains("series") && dtSub.Columns[c].ExtendedProperties["series"].ToString().IndexOf(totalText, StringComparison.OrdinalIgnoreCase) >= 0)
                                                    totalTable = dtSub.Columns[c].ExtendedProperties["table"].ToString(); // the totalText has been found so set the totalTable to the current table
                                                else if (dtSub.Columns[c].ExtendedProperties["table"].ToString() != totalTable) // the table series is no longer the totalText so if the table extended property is not the same as the totalTable then the total has been orphaned
                                                {
                                                    dtSub.Columns[c].ExtendedProperties["table"] = totalTable; // the current column is moved to the totalTable 
                                                    if (dtSub.Columns[c].ExtendedProperties["series"] == dtSub.Columns[c - 1].ExtendedProperties["series"]) // if the series is a pair it's pair is also moved to the total column
                                                        dtSub.Columns[c - 1].ExtendedProperties["table"] = totalTable;
                                                    break;
                                                }
                                                else
                                                    break;
                                            }

                                            bool keepTable = false;
                                            for (int t = 0; t < fieldNames.Count(); t++) // iterate through all the available field names
                                            {
                                                keepTable = false;
                                                DataTable dtCopy = dtSub.Copy();
                                                for (int c = dtSub.Columns.Count - 1 - (localRowType ? 1 : 0); c > 0; c--) // iterate backwards through the table deleting every column not needed in the current table except for rowtype (if it exists) and the first column - backwards not necessary since we are iterating through the source and deleting in the copy but I'll leave it that way
                                                {

                                                    if (dtSub.Columns[c].ExtendedProperties["table"].ToString() != t.ToString()) // check the current column table property compared to the current table
                                                        dtCopy.Columns.Remove(dtSub.Columns[c].ColumnName); // it didn't match so remove it from the table copy
                                                    else
                                                        keepTable = true; // only keep tables that have at least one data column

                                                }
                                                if (keepTable)
                                                    dtDataStaging.Rows.Add(new object[] { _processStream.PdiFile.JobID, _processStream.PdiFile.Code, sheetName, fieldNames[t], 0, 0, BuildTable(dtCopy, settings) });
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //excelLoader.ConsolePrintTable(dtSub); //debugging info
                                    DataRow appendRow = dtDataStaging.Select($"Job_ID = '{_processStream.PdiFile.JobID}' AND Code = '{_processStream.PdiFile.Code}' AND Sheet_Name = '{sheetName}' AND Item_Name = '{fieldNamePart}'").SingleOrDefault();
                                    if (appendRow != null)
                                        appendRow["Value"] = BuildTable(dtSub, settings, appendRow.Field<string>("Value"));
                                    else
                                        dtDataStaging.Rows.Add(new object[] { _processStream.PdiFile.JobID, _processStream.PdiFile.Code, sheetName, fieldNamePart, 0, 0, BuildTable(dtSub, settings) });
                                }
                            }
                            else if (clearUnused) // M11 and M12 are required to blank any non-present data - DB configuration field Clear_Unused_Fields
                            {
                                if (fieldParts.Length == 4 && fieldParts[3] == "Text")
                                    dtDataStaging.Rows.Add(new object[] { _processStream.PdiFile.JobID, _processStream.PdiFile.Code, sheetName, fieldNamePart, 0, 0, Transform.EmptyText });
                                else
                                    dtDataStaging.Rows.Add(new object[] { _processStream.PdiFile.JobID, _processStream.PdiFile.Code, sheetName, fieldNamePart, 0, 0, Transform.EmptyTable });
                            }

                        }
                    }
                }


                UpdateRecordCount(_jobID, sheetCount);

                if (dtDataStaging.Rows.Count > 0) // check if there are any rows in the data stating DataTable
                {

                    if (!_dbCon.BulkCopy("dbo.pdi_Data_Staging", dtDataStaging))
                    {
                        Logger.AddError(_log, $"Transform Failed for Job_ID: {_processStream.PdiFile.JobID} - Error: {_dbCon.LastError}");
                        return false;
                    }
                    //using (SqlBulkCopy bulkCopy = new SqlBulkCopy(_dbCon.GetSqlConnection()))
                    //{
                    //    bulkCopy.DestinationTableName = "dbo.pdi_Data_Staging";

                    //    try
                    //    {
                    //        // Write from the source to the destination.
                    //        bulkCopy.WriteToServer(dtDataStaging);
                    //        return true;
                    //    }
                    //    catch (Exception ex)
                    //    {
                    //        Console.WriteLine("Transform Failed for Job_ID: {0} - Error: {1}", _processStream.PdiFile.JobID, ex.Message);
                    //        return false;
                    //    }
                    //}
                }
                else
                {
                    Logger.AddError(_log, $"There were no sheets with extractable data in the submitted file - sheets found include: {(_loader.WorksheetNames() != null && _loader.WorksheetNames().Count > 0 ? string.Join(", ", _loader.WorksheetNames()): "no sheets found")}" );
                    return false; // fail if there are no rows
                }
                _loader.Dispose();

            }
            return true;
        }

        /// <summary>
        /// Check for a character range in the Field Name portion of F12{{i-s}}|Series{{-1}}|{{blank-1}} - non matching sections are passed back as is, in their defined location (ahead or behind of expanded sections(s)).
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ConvertRange(string value)
        {
            List<string> expand = new List<string>();
            foreach (string valSection in value.Split(','))
            {
                if (valSection.Contains("SplitPairs") || valSection.Contains("SplitSeries")) // when SplitPairs is specified don't expand the range ahead of time as the tables will be created all at once.
                    expand.Add(valSection);
                else
                {
                    Match m = Regex.Match(valSection, @"^([\S]*)(" + Regex.Escape(Generic.FLAGPRE) + @"(\w+)-(\w+)" + Regex.Escape(Generic.FLAGPOST) + @")(?=\|)"); // changed from \D for a single character to \w+ for word characters - added field capture
                    if (m.Success)
                    {
                        string field = m.Groups[1].Value;   // the field portion without letter code
                        string start = m.Groups[3].Value;   // the start letter code
                        string end = m.Groups[4].Value;     // the last valid letter code
                        char startChar = 'a';
                        string current = string.Empty;
                        if (start.ToLower() != start)       // if the start is uppercase use 'A'
                            startChar = 'A';

                        while ((current = current.IncrementFieldLetter(start, end, true, startChar)) != null)
                        {
                            if (field.Contains("~")) // there is an alternating field present - add them in order.
                            {
                                foreach (string section in field.Split('~'))
                                    expand.Add(section + current + valSection.Substring(valSection.IndexOf('|')));
                            }
                            else
                                expand.Add(valSection.Replace(m.Groups[2].Value, current)); 
                        }
                    }
                else
                    expand.Add(valSection);
                }
                
            }
            return string.Join(",", expand); ;

        }
        /// <summary>
        /// Try to replace occurrences of the properties of the supplied object - possibly with + or - integers - in the provided string
        /// </summary>
        /// <param name="value">The string to replace properties in</param>
        /// <param name="rep">The object containing properties</param>
        /// <returns>The string with replaced properties</returns>
        public static string ReplaceAddress(string value, object rep)
        {
            foreach (System.Reflection.PropertyInfo pi in rep.GetType().GetProperties())
            {
                Match m = Regex.Match(value, @"^([A-Z]*)(" + Regex.Escape(Generic.FLAGPRE + pi.Name) + @"([\+-])?(\d)?" + Regex.Escape(Generic.FLAGPOST) + ")"); // {{blank+1}}
                if (m.Success)
                {
                    if (pi.GetValue(rep).GetType() == typeof(int))
                    {
                        int curVal = (int)pi.GetValue(rep) + (m.Groups[1].Value != string.Empty ? +1 : 0); // if a column is in the capture group then add one to the DataTable row for the Excel row equivalent (0 based to 1 based)
                        if (m.Groups.Count == 5)
                        {
                            if (int.TryParse(m.Groups[4].Value, out int inc))
                            {
                                switch (m.Groups[3].Value)
                                {
                                    case "+":
                                        curVal += inc;
                                        break;
                                    case "-":
                                        curVal -= inc;
                                        break;
                                }
                            }
                        }
                        value = value.Replace(m.Groups[2].Value, curVal.ToString());
                    }
                    else // the passed value wasn't an integer so just replace the match with it
                        value = value.Replace(m.Groups[2].Value, pi.GetValue(rep).ToString());
                }
            }
            return value;
        }

        /// <summary>
        /// Extracts an offset from a search string "SERIES{{-1}}" - would have a -1 offset
        /// </summary>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static string PlusOffset(string value, out int offset)
        {
            offset = 0;
            Match m = Regex.Match(value, Regex.Escape(Generic.FLAGPRE) + @"([\+-])+(\d)+" + Regex.Escape(Generic.FLAGPOST)); // {{blank+1}}
            if (m.Success)
            {
                if (m.Groups.Count == 3)
                {
                    if (int.TryParse(m.Groups[2].Value, out int inc))
                    {
                        switch (m.Groups[1].Value)
                        {
                            case "+":
                                offset = inc;
                                break;
                            case "-":
                                offset = inc * -1;
                                break;
                        }
                    }
                }
                return value.Replace(m.Value, string.Empty);
            }
            return value;
        }

        /// <summary>
        /// Once the replacement values {{ }} have been taken care of see if the address is a row, A1C1, or search type
        /// </summary>
        /// <param name="value"></param>
        /// <param name="al"></param>
        /// <param name="dt"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <param name="startRow"></param>
        /// <param name="end"></param>
        public static void ConvertAddress(string value, AsposeLoader al, DataTable dt, out int row, out int col, int startRow = 0, bool end = false, bool greedy = false)
        {
            col = end ? dt.Columns.Count - 1 : 0; // Convert to 0 base for the column since the column address is considered inclusive
            value = value.Trim();
            if (!int.TryParse(value, out row))
            {
                if (!al.ConvertAddress(value, out row, out col))
                {
                    if (value.Length > 0)
                    {
                        col = end ? dt.Columns.Count - 1 : 0; // Convert to 0 base for the column since the column address is considered inclusive
                        //string val = PlusOffset(value, out int offSet);

                        Dictionary<string, int> values = SplitText(value);

                        for (int r = startRow; r < dt.Rows.Count; r++)
                        {
                            foreach (KeyValuePair<string, int> kv in values)
                            {
                                if (dt.Rows[r].ItemArray.Any(ia => ia.ToString().StripTagsExceptAlpha().IndexOf(kv.Key) == 0)) // must be the beginning of the string - StripTags takes care of case
                                {
                                    row = r + kv.Value;
                                    r = dt.Rows.Count;
                                    break;
                                }
                            }
                        }
                        if (row < 0 && greedy)
                            row = dt.Rows.Count - 1;
                    }
                }
            }
        }

        public static Dictionary<string, int> SplitText(string value)
        {
            Dictionary<string, int> values = new Dictionary<string, int>(value.CountOccurances("~") + 1);
            foreach (string s in value.Split('~'))
                values.Add(PlusOffset(s, out int offSet).StripTagsExceptAlpha(), offSet);

            return values;
        }

        private string BuildText(DataTable dt)
        {
            //List<string> text = new List<string>(1);
            StringBuilder text = new StringBuilder();
            bool col = dt.RemoveBlankColumns();
            bool row = dt.RemoveBlankRows();

            for (int r = 0; r < dt.Rows.Count; r++)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                    if (dt.Columns[c].ColumnName != Generic.FSMRFP_ROWTYPE_COLUMN)
                        if (!dt.Rows[r][c].ToString().IsNaOrBlank())
                            if (dt.Rows.Count > 1)
                                text.Append($"<p>{dt.Rows[r][c]}</p>");
                            else
                                text.Append(dt.Rows[r][c].ToString());
            }
            return text.ToString(); //string.Join("<br />", text);
        }

        private string BuildTable(DataTable dt, Tuple<bool, bool, int, bool> settings, string appendTo = "<table>")
        {
            if (appendTo.Contains("</table>") && dt.Rows.Count > 0) // if we are appending to an existing table 
            {
                dt.Rows.RemoveAt(0); //then we need to remove the first row of the incoming table if it's blank or has a header row - TODO: right now it's just stripping the first one regardless
                appendTo = appendTo.Replace("</table>", ""); // and the closing table tag
            }
            bool hasRowType = dt.Columns.Contains(Generic.FSMRFP_ROWTYPE_COLUMN);
            if (!settings.Item2)
                dt.RemoveBlankColumns(settings.Item3 + (hasRowType ? 1 : 0)); // if the table has a rowtype keep an extra column
            if (!settings.Item4)
                dt.RemoveBlankRows();

            //bool groupOn = dt.TableName.Contains("SOI") && dt.Columns.Count == 7; // this is specific code for the SOI table to do grouping - this is an alternative to doing this in the proper "transform" but that would require a lot of deconstruction and reconstruction of the table
            string curGroup = string.Empty;
            StringBuilder sb = new StringBuilder(appendTo);

            for (int r = 0; r < dt.Rows.Count; r++)
            {
                sb.Append("<row");
                //if (r == 0)
                //    sb.Append(" header=\"true\"");
                if (hasRowType && !dt.Rows[r][Generic.FSMRFP_ROWTYPE_COLUMN].ToString().IsNaOrBlank())
                    sb.Append($" rowtype=\"{dt.Rows[r][Generic.FSMRFP_ROWTYPE_COLUMN]}\"");

                sb.Append(">");

                if (settings.Item1 && dt.Columns.Count >= 6) // If the SOI is already formatted for output then there will be 4 or 5 columns as the extra columns are removed as blank columns
                {
                    if (!dt.Rows[r][1].ToString().IsNaOrBlank() && !dt.Rows[r][2].ToString().IsNaOrBlank()) // SOI SPECIFIC - when the second and third columns are not blank we can do the special grouping rows
                    {
                        if (curGroup != dt.Rows[r][0].ToString()) // enter a new heading row
                        {
                            curGroup = dt.Rows[r][0].ToString();
                            sb.Append($"<cell>{curGroup}</cell>");
                            for (int c = 3; c < dt.Columns.Count; c++)
                                if (dt.Columns[c].ColumnName != Generic.FSMRFP_ROWTYPE_COLUMN)
                                    sb.Append("<cell></cell>");

                            // output another row for the data from this new group row 
                            sb.Append("</row><row");
                            if (hasRowType && !dt.Rows[r][Generic.FSMRFP_ROWTYPE_COLUMN].ToString().IsNaOrBlank())
                                sb.Append($" rowtype=\"{dt.Rows[r][Generic.FSMRFP_ROWTYPE_COLUMN]}\"");
                            sb.Append(">");
                        }
                        sb.Append($"<cell>{dt.Rows[r][1]}, {dt.Rows[r][2]}</cell>"); // in or out of a group we output the combined column 2 and 3 data

                    }
                    else // output the first column and then skip 2
                        sb.Append($"<cell>{dt.Rows[r][0]}</cell>");

                    for (int c = 3; c < dt.Columns.Count; c++)     // after the group row and grouped columns are output follow up with the remaining columns
                        if (dt.Columns[c].ColumnName != Generic.FSMRFP_ROWTYPE_COLUMN)
                            sb.Append($"<cell>{dt.Rows[r][c]}</cell>");
                }
                else
                {
                    for (int c = 0; c < dt.Columns.Count; c++)
                        if (dt.Columns[c].ColumnName != Generic.FSMRFP_ROWTYPE_COLUMN)
                            sb.Append($"<cell>{dt.Rows[r][c]}</cell>");

                }
                sb.Append("</row>");
            }

            sb.Append("</table>");
            return sb.ToString();
        }

        private DataTable LoadClientSheets(int clientID, int documentTypeID)
        {
            DataTable dt = new DataTable("SheetIndex");
            _dbCon.LoadDataTable("SELECT * FROM [pdi_Data_Type_Sheet_Index] WHERE Client_ID = @clientID AND Document_Type_ID = @documentTypeID",
                new Dictionary<string, object>(1) {
                    { "@clientID", clientID },
                    { "documentTypeID", documentTypeID }
                }, dt);

            return dt;
        }

        private DataTable LoadDataStaging(int jobID)
        {
            DataTable dt = new DataTable("DataStaging");  //ds.Tables["DataStaging"];
            _dbCon.LoadDataTable("SELECT * FROM pdi_Data_Staging WHERE Job_ID = @jobID",
                new Dictionary<string, object>(1) {
                    { "@jobID", jobID }
                }, dt);

            return dt;
        }



        internal bool ExtractBAU()
        {
            DataTable worksheet = null;

            DataTable dtStaging = LoadDataStaging(_jobID);
            worksheet = _loader.ExtractTableDoubleHeader("DocumentData");
            if (worksheet != null && worksheet.Rows.Count > 0)
            {
                foreach (DataRow row in worksheet.Rows)
                {
                    string code = row.GetPartialColumnStringValue("DocumentCode");
                    if (code != null && code.Length > 0)
                    {
                        TableList table22b = new TableList(row.GetPartialColumnStringValue("FilingDate").FilingYear()); //row.GetPartialColumnStringValue("FilingDate").ToDate(DateTime.MaxValue).Year
                        foreach (DataColumn dc in worksheet.Columns)
                        {
                            if (dc.ColumnName.IndexOf("DocumentCode", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                if (dc.ColumnName.IndexOf("22b", StringComparison.OrdinalIgnoreCase) >= 0) // instead of adding 22b fields directly to Data_Staging add them to the current 22b TableList
                                    table22b.AddValidation(row[dc].ToString().Trim());
                                else
                                    dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", AsposeLoader.GetColumnName(dc.ColumnName), AsposeLoader.GetNumberFromName(dc.ColumnName), 0, row[dc].ToString().Trim() });
                            }
                        }

                        if (table22b.Count > 0)
                        {
                            // extract the 22b table and add the calculated fields
                            string field22b = table22b.GetTableString(out int calendarYears, out int negativeYears);
                            dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF ? "FF" : "E") + "22b", 0, 0, field22b });
                            dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", "AgeCalendarYears", 0, 0, calendarYears });
                            dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", "NegativeReturnCalendarYears", 0, 0, negativeYears });
                        }

                        // Extract empty tables for IsProforma records
                        if (row.GetPartialColumnBoolValue("IsProforma") && (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF || _processStream.PdiFile.GetDocumentType == DocumentTypeID.ETF))
                        {
                            // when the document is IsProforma then add a blank table record for FF16, FF17 and FF40
                            dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF ? "FF" : "E") + "16_EN", 0, 0, Transform.EmptyTable });
                            dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF ? "FF" : "E") + "16_FR", 0, 0, Transform.EmptyTable });
                            dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF ? "FF" : "E") + "17_EN", 0, 0, Transform.EmptyTable });
                            dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF ? "FF" : "E") + "17_FR", 0, 0, Transform.EmptyTable });
                           
                            if (!row.GetPartialColumnStringValue(PDIFile.FILE_DELIMITER + (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF ? "FF" : "E") + "40sh").IsNaOrBlank())
                            {
                                dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF ? "FF" : "E") + "40_EN", 0, 0, Transform.EmptyTable });
                                dtStaging.Rows.Add(new object[] { _jobID, code, "DocumentData", (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FF ? "FF" : "E") + "40_FR", 0, 0, Transform.EmptyTable });
                            }
                        }
                    }
                }
            }


            if (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FP || _processStream.PdiFile.GetDocumentType == DocumentTypeID.EP)
            {
                worksheet = _loader.BuildAllocationTable();
                if (worksheet != null && worksheet.Rows.Count > 0)
                {
                    string sql = "SELECT DISTINCT FundCode, [FIELD_NAME], [english], [french] FROM [dbo].[view_pdi_Fund_Profile_Data] vFPD INNER JOIN [pdi_Publisher_Documents] PD ON PD.Client_ID = vFPD.Client_ID AND PD.Document_Type_ID = vFPD.Document_Type_ID AND PD.Document_Number = vFPD.DOCUMENT_NUMBER WHERE Field_Name LIKE '%P3_h' AND  vFPD.Client_ID = @clientID AND vFPD.DOCUMENT_TYPE_ID = @docTypeID ORDER BY 1,2;";

                    DataTable dtB = new DataTable("Match");
                    _dbCon.LoadDataTable(sql, new Dictionary<string, object>(2)
                    {
                        { "@clientID", _processStream.PdiFile.ClientID },
                        { "@docTypeID", _processStream.PdiFile.DocumentTypeID }
                    }, dtB, false);

                    //DataTable dtA = worksheet; //.DefaultView.ToTable();

                    AllocationList listAT = new AllocationList(_dbCon, _processStream.PdiFile, _log);
                 
                    // add all the incoming Allocations to the table
                    foreach (DataRow dr in worksheet.Rows) 
                    {
                        listAT.AddCode(dr.GetExactColumnStringValue("FundCode"), dr.GetExactColumnStringValue("AllocationType"), dr.GetExactColumnStringValue("en-CA"), dr.GetExactColumnStringValue("fr-CA"), dr.GetExactColumnStringValue("Field_Name"), dr.GetExactColumnStringValue("English"), dr.GetExactColumnStringValue("French"), _processStream.PdiFile.GetDocumentType.ToString() + "3xh");
                    }

                    // add all the existing allocations to the table - will match on headers where possible : 20220310 - Filter for only documents that are in the file being processed
                    foreach (DataRow dr in dtB.Rows)
                    {
                        if (worksheet.Select($"FundCode = '{dr.GetExactColumnStringValue("FundCode")}'").Length > 0)
                            listAT.AddCode(dr.GetExactColumnStringValue("FundCode"), dr.GetExactColumnStringValue("english"), null, null, dr.GetExactColumnStringValue("Field_Name"), dr.GetExactColumnStringValue("english"), dr.GetExactColumnStringValue("french"));
                    }

                    listAT.AppendToDataTable(dtStaging, _jobID, "Allocation Tables", _processStream.PdiFile.DocumentType, true); // when pulling directly from PROD override QA headers
                }

                AppendSheet(_loader.ExtractTableDoubleHeader("NAVPU - MER"), dtStaging);

                AppendSheet(_loader.ExtractTables("Distributions"), dtStaging);

            }
            //else if (_processStream.PdiFile.GetDocumentType == DocumentTypeID.FS || _processStream.PdiFile.GetDocumentType == DocumentTypeID.MRFP)
            //{
            //    AppendSheet(_loader.ExtractTables("Distributions"), dtDocument);
            //}
            _loader.AddTables(dtStaging, _processStream.PdiFile, _gen, _jobID);

            if (!_dbCon.BulkCopy("dbo.pdi_Data_Staging", dtStaging))
            {
                Logger.AddError(_log, $"Transaction Rolled Back for Job_ID: {_jobID} - Error: {_dbCon.LastError}");
                return false;
            }
            return true;
        }

        internal bool ExtractStatic()
        {
            Dictionary<string, bool> updateList = _loader.TableUpdate(); //"Table UPDATE"
            DataTable worksheet = null;
            DataSet ds = new DataSet();

            foreach (KeyValuePair<string, bool> curKey in updateList)
            {
                if (curKey.Value)
                {
                    worksheet = _loader.ExtractTables(curKey.Key);
                    // this should be checked in validation but for now record a proper error and exit the extract - 20220215 - added validation check in template but it needs to be added to the template sheets
                    if (worksheet is null)
                    {
                        Logger.AddError(_log, $"ExtractStatic Error: 'Table UPDATE' sheet indicates that '{curKey.Key}' should be updated but a corresponding sheet in {_processStream.PdiFile.OnlyFileName} could not be found.");
                        return false;
                    }

                    if (curKey.Key.IndexOf("Client Translation Language", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        DataTable dtTemp = ds.Tables.Add("Translation");
                        _dbCon.LoadDataTable("SELECT * FROM pdi_Data_Staging_STATIC_Translation_Language WHERE Job_ID = @jobID;", new Dictionary<string, object>(1) {
                            { "@JobID", _jobID }
                        }, dtTemp);

                        foreach (DataRow row in worksheet.Rows)
                        {
                            ds.Tables["Translation"].Rows.Add(new object[] { null, _jobID, row["English"].ToString().Trim(), row["French"].ToString().Trim().ExcelTextClean() }); // added clean up of incoming text to reduce replace processing later
                        }
                    }
                    else
                    {
                        if (!ds.Tables.Contains("Scenario"))
                        {
                            DataTable dtTemp = ds.Tables.Add("Scenario");
                            _dbCon.LoadDataTable("SELECT * FROM pdi_Data_Staging_STATIC_Content_Scenario WHERE Job_ID = @jobID;", new Dictionary<string, object>(1) {
                                { "@JobID", _jobID }
                            }, dtTemp);
                        }

                        foreach (DataRow row in worksheet.Rows)
                        {
                            if (!row["Field Name"].ToString().IsNaOrBlank() && !row["Scenario"].ToString().IsNaOrBlank())
                                ds.Tables["Scenario"].Rows.Add(new object[] { _jobID, curKey.Key, row["Field Name"].ToString().Trim(), row["Scenario"].ToString().Trim(), row["Scenario Description"].ToString().Trim(), row["English"].ToString().Trim(), row["French"].ToString().Trim(), row.GetPartialColumnStringValue("Field Description").IsNaOrBlank() ? row["Field Name"].ToString().Trim() : row["Field Description"].ToString().Trim(), Guid.NewGuid() });
                        }
                    }
                }
            }

            worksheet = _loader.ExtractTables("Field UPDATE");
            if (worksheet != null && worksheet.Rows.Count > 0)
            {
                DataTable dtTemp = ds.Tables.Add("Field");
                _dbCon.LoadDataTable("SELECT * FROM pdi_Data_Staging_STATIC_Field_Update WHERE Job_ID = @jobID;", new Dictionary<string, object>(1) {
                    { "@JobID", _jobID }
                }, dtTemp);

                foreach (DataRow row in worksheet.Rows)
                {
                    if (row["Load Field"].ToString().ToBool() && !row["Field Name"].ToString().IsNaOrBlank())
                        ds.Tables["Field"].Rows.Add(new object[] { _jobID, row["Field Name"].ToString().Trim() });
                }
            }

            worksheet = _loader.ExtractTableDoubleHeader("Document UPDATE");
            if (worksheet != null && worksheet.Rows.Count > 0)
            {
                DataTable dtTemp = LoadDataStaging(_jobID);
                dtTemp.TableName = "Document";
                ds.Tables.Add(dtTemp);

                foreach (DataRow row in worksheet.Rows)
                {
                    string code = row.GetPartialColumnStringValue("DocumentCode");
                    if (code != null && code.Length > 0)
                    {
                        foreach (DataColumn dc in worksheet.Columns)
                        {
                            if (dc.ColumnName.IndexOf("DocumentCode", StringComparison.OrdinalIgnoreCase) < 0)
                                ds.Tables["Document"].Rows.Add(new object[] { _jobID, code, "Document UPDATE", AsposeLoader.GetColumnName(dc.ColumnName), AsposeLoader.GetNumberFromName(dc.ColumnName), 0, row[dc].ToString().Trim() });
                        }
                    }
                }
            }

            foreach (DataTable dt in ds.Tables)
            {
                string destTable = string.Empty;
                switch (dt.TableName)
                {
                    case "Translation":
                        destTable = "dbo.pdi_Data_Staging_STATIC_Translation_Language";
                        break;
                    case "Scenario":
                        destTable = "dbo.pdi_Data_Staging_STATIC_Content_Scenario";
                        break;
                    case "Field":
                        destTable = "dbo.pdi_Data_Staging_STATIC_Field_Update";
                        break;
                    case "Document":
                        destTable = "dbo.pdi_Data_Staging";
                        break;

                    default:
                        destTable = string.Empty;
                        break;
                }
                if (destTable != string.Empty)
                {
                    if (!_dbCon.BulkCopy(destTable, dt))
                    {
                        Logger.AddError(_log, _dbCon.LastError);
                        return false;
                    }
                }
            }
            ds.Dispose();
            return true;
        }

        internal bool ExtractBNY()
        {
            DataTable worksheet = null;

            DataTable dtStaging = LoadDataStaging(_jobID);
            worksheet = _loader.ExtractTables("BNY_Data"); // use wsBNY?

            int rowCount = 0;
            if (worksheet != null && worksheet.Rows.Count > 0)
            {
                for (int r = 0; r < worksheet.Rows.Count; r++)
                {

                    rowCount++;

                    DataRow row = worksheet.Rows[r];
                    if (!row.GetExactColumnStringValue("Include").ToBool()) // skip rows not marked for inclusion
                        continue;

                    string primaryIndex = row.GetExactColumnStringValue("Primary Index Label");
                    string docCode = row.GetExactColumnStringValue("Document Code");
                    string series = row.GetExactColumnStringValue("Series Name");
                    string cycle = row.GetExactColumnStringValue("Annual Semi-Annual Flag");
                    string asAtDate = row.GetExactColumnStringValue("eglPan-EndEffectiveDate");
                    string inceptionDate = row.GetExactColumnStringValue("Inception Date (USRDATE1)");

                    //M17
                    if (primaryIndex.IndexOf("Not Applicable", StringComparison.OrdinalIgnoreCase) < 0 && !row.GetExactColumnStringValue("MRFP 1Y Return").IsNaOrBlank() && cycle.IndexOf("Semi-Annual", StringComparison.OrdinalIgnoreCase) < 0) // No M17 for Semi-Annual or Not Applicable
                    {
                        PDI_DataTable dtM17 = new PDI_DataTable($"M17{series.ToLower()}"); // as per US 18027 series field name should be in lower case

                        
                        dtM17.ExtendedProperties.Add("SeriesLetter", series);
                        dtM17.ExtendedProperties.Add("PrimaryIndex", primaryIndex);
                        dtM17.ExtendedProperties.Add("InceptionDate", inceptionDate);
                        dtM17.ExtendedProperties.Add("Cycle", cycle);
                        dtM17.ExtendedProperties.Add("AsAtDate", asAtDate);

                        dtM17.Columns.Add("1Year");
                        dtM17.Columns.Add("3Year");
                        dtM17.Columns.Add("5Year");
                        dtM17.Columns.Add("10Year");
                        dtM17.Columns.Add("10YearOrLess");

                        // Keep things simple in the extract by adding the sections in order with all necessary data
                        dtM17.Rows.Add(new string[] { row[13 - 1].ToString(), row[14 - 1].ToString(), row[15 - 1].ToString(), row[16 - 1].ToString(), row[18 - 1].ToString() }); // MRFP
                        dtM17.Rows.Add(new string[] { row[21 - 1].ToString(), row[22 - 1].ToString(), row[23 - 1].ToString(), row[24 - 1].ToString(), row[26 - 1].ToString() }); // BM1
                        dtM17.Rows.Add(new string[] { row[29 - 1].ToString(), row[30 - 1].ToString(), row[31 - 1].ToString(), row[32 - 1].ToString(), row[34 - 1].ToString() }); // BM2
                        dtM17.Rows.Add(new string[] { row[37 - 1].ToString(), row[38 - 1].ToString(), row[39 - 1].ToString(), row[40 - 1].ToString(), row[42 - 1].ToString() }); // BM3
                        dtM17.Rows.Add(new string[] { row[45 - 1].ToString(), row[46 - 1].ToString(), row[47 - 1].ToString(), row[48 - 1].ToString(), row[50 - 1].ToString() }); // BM4

                        dtM17.ExtendedProperties.Add("AgeCalendarYears", AgeForHeader(dtM17.Rows[0]));

                        dtStaging.Rows.Add(new object[] { _jobID, docCode, worksheet.TableName, dtM17.TableName, 0, 0, dtM17.DataTabletoXML() });
                    }

                    //M15
                    PDI_DataTable dtM15 = new PDI_DataTable($"M15{series.ToLower()}");
                    dtM15.ExtendedProperties.Add("AgeCalendarYears", asAtDate.AgeInYearsBNY(inceptionDate).ToString()); //
                    dtM15.ExtendedProperties.Add("SeriesLetter", series);
                    dtM15.ExtendedProperties.Add("PrimaryIndex", primaryIndex);
                    dtM15.ExtendedProperties.Add("InceptionDate", inceptionDate);
                    dtM15.ExtendedProperties.Add("Cycle", cycle);
                    dtM15.ExtendedProperties.Add("AsAtDate", asAtDate);

                    dtM15.Columns.Add("Value");
                    dtM15.Columns.Add("Date");

                    string data = null;
                    if (cycle.IndexOf("Semi", StringComparison.OrdinalIgnoreCase) >= 0) // semi-annual cycle
                        data = row.GetExactColumnStringValue("MFRP 6M Return");
                    else // annual cycle
                        data = row.GetExactColumnStringValue("MRFP 1Y Return");

                    if (data.IsNaOrBlank()) // In this situation we try loading the MRFP ITD Ann
                        data = row.GetExactColumnStringValue("MRFP ITD Ann");

                    if (!data.IsNaOrBlank()) // now if the semi or annual amount was blank we should have the value from the ITD
                    { 
                        if (data.Contains("(") && data.Contains(")"))
                            data = "-" + data.Replace("(", string.Empty).Replace(")", string.Empty); // convert bracketed value to -ve only for M15

                        dtM15.Rows.Add(new string[] { row.GetExactColumnStringValue("MRFP Date"), data });
                        dtStaging.Rows.Add(new object[] { _jobID, docCode, worksheet.TableName, dtM15.TableName, 0, 0, dtM15.DataTabletoXML() });
                    }
                    
                }
            }

            UpdateRecordCount(_jobID, rowCount);

            if (!_dbCon.BulkCopy("dbo.pdi_Data_Staging", dtStaging))
            {
                Logger.AddError(_log, $"Transaction Rolled Back for Job_ID: {_jobID} - Error: {_dbCon.LastError}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Specifically for the M17 calculation - determine the number of available years based on the populated columns
        /// </summary>
        /// <param name="dr">The M17 DataRow to check</param>
        /// <returns>The highest number of years available</returns>
        internal int AgeForHeader(DataRow dr)
        {
            if (!dr.GetExactColumnStringValue("10Year").IsNaOrBlank())
                return 10;
            else if (!dr.GetExactColumnStringValue("5Year").IsNaOrBlank())
                return 5;
            else if (!dr.GetExactColumnStringValue("3Year").IsNaOrBlank())
                return 3;
            else
                return 1;
        }
        /// <summary>
        /// Appends every column in the incoming table to the output table (assumed structure) except for the DocumentCode and Row Number columns
        /// </summary>
        /// <param name="dtIn">The input DataTable (Excel Sheet)</param>
        /// <param name="dtOut">The output DataTable (pdi_Data_Staging)</param>
        internal void AppendSheet(DataTable dtIn, DataTable dtOut)
        {
            foreach (DataRow dr in dtIn.Rows)
            {
                string documentCode = dr.GetPartialColumnStringValue("DocumentCode");
                int rowNum = dr.GetPartialColumnIntValue("Row number");

                if (documentCode != null && documentCode.Length > 0 && rowNum > 0)
                {
                    foreach (DataColumn dc in dtIn.Columns)
                    {
                        if (dc.ColumnName.IndexOf("DocumentCode", StringComparison.OrdinalIgnoreCase) < 0 && dc.ColumnName.IndexOf("Row number", StringComparison.OrdinalIgnoreCase) < 0) // && !dr.Field<string>(dc).IsNaOrBlank()
                            dtOut.Rows.Add(_jobID, documentCode, dtIn.TableName, AsposeLoader.GetColumnName(dc.ColumnName), rowNum, AsposeLoader.GetNumberFromName(dc.ColumnName), dr.Field<string>(dc));
                    }
                }
            }
        }


        public void UpdateRecordCount(int jobID, int documentCount)
        {
            if (jobID > 0 && _dbCon != null)
            {
                if (!_dbCon.ExecuteNonQuery("UPDATE [pdi_File_Log] SET Number_of_Records = @documentCount WHERE Data_ID = (SELECT Data_ID FROM [pdi_Processing_Queue_Log] WHERE Job_ID = @jobID)", out int rows, new Dictionary<string, object>(3) {
                    { "@documentCount", documentCount },
                    { "@jobID", jobID }
                }))
                {
                    Logger.AddError(_log, $"Failed to update File Status and Count - {_dbCon.LastError}");
                }
            }
        }
    }
}
