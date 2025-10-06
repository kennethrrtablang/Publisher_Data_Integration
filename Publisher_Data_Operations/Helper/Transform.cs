using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualBasic;
using Publisher_Data_Operations.Extensions;

namespace Publisher_Data_Operations.Helper
{
    public class Static3Collection
    {
        public Dictionary<string, string> Collection { get; set; }
        public int Row { get; set; }

        public Static3Collection()
        {
            Collection = new Dictionary<string, string>(6);
        }
    }
    public class Transform
    {
        int _jobID = -1;
        PDIFile _fileName = null;
        DBConnection _dbCon = null;
        DBConnection _pubDbCon = null;
        //Processing _proc = null;
        Generic _gen = null;
        List<string> fieldList = new List<string>();
        RowIdentity rowIdentity = new RowIdentity();
        Dictionary<string, string> documentFields;
        Dictionary<string, string> rowFields; // For FS documents
        DataTable _transTable = new DataTable("TransformedData");
        DataTable dt;
        Tuple<string, string, string> scenario;
        Logger _log = null;

        public static string EmptyTable = "<table />";
        public static string EmptyText = "<p />";
        //public List<string> HistoricalDataRequiredFields = new List<string>() { "M11","M12","M15" };
        private MergeTables _mergeTables = null;

        public DataTable GetTransTable { get { return _transTable; } }

        string[] BulletInvestments, BulletPercent, BulletNumber, BulletCurrency, BestWorstSentenceRise, BestWorstSentenceSame, BestWorstSentenceDrop;
        string[] month = new string[2];

        Dictionary<string, string[]> LongMonthNames;

        public Transform(PDIFile fileName, object con, Logger log = null, object pubDbCon = null)
        {
            _fileName = fileName;
            if (con.GetType() == typeof(DBConnection))
                _dbCon = (DBConnection)con;
            else
                _dbCon = new DBConnection(con);

            _dbCon.Transaction = null;



            if (pubDbCon != null && pubDbCon.GetType() == typeof(DBConnection))
                _pubDbCon = (DBConnection)pubDbCon;
            else
                _pubDbCon = new DBConnection(pubDbCon);

            _jobID = (int)_fileName.JobID;

            //_proc = new Processing(_jobID, con);

            _log = log;
            if (_log is null)
                _log = new Logger(con, _fileName);

            _gen = new Generic(_dbCon, _log);

            //Load all of the global Bullets
            BulletInvestments = _gen.searchGlobalScenarioText("BulletInvestments");
            BulletPercent = _gen.searchGlobalScenarioText("BulletPercent");
            BulletNumber = _gen.searchGlobalScenarioText("BulletNumber");
            BulletCurrency = _gen.searchGlobalScenarioText("BulletCurrency");
            BestWorstSentenceRise = _gen.searchGlobalScenarioText("BestWorstSentenceRise");
            BestWorstSentenceSame = _gen.searchGlobalScenarioText("BestWorstSentenceSame");
            BestWorstSentenceDrop = _gen.searchGlobalScenarioText("BestWorstSentenceDrop");

            //Month names
            LongMonthNames = _gen.loadMonths();
        }

        public bool RunTransform()
        {
            bool retVal = false;
            //if (_loader != null)
            //{

            switch (_fileName.GetDataType)
            {
                case DataTypeID.STATIC:
                    retVal = TransformSTATIC();
                    break;
                case DataTypeID.FSMRFP:
                    retVal = TransformFSMRFP();
                    break;
                case DataTypeID.BAU:
                    retVal = TransformBAU();
                    break;
                case DataTypeID.BNY:
                    retVal = TransformBNY();
                    break;
                default:
                    Logger.AddError(_log, $"The Data Type '{Enum.GetName(typeof(DataTypeID), _fileName.GetDataType)}' transform is not implemented");
                    retVal = false;
                    break;
            }
            if (_log != null)
                _log.WriteErrorsToDB();

            _gen.SaveFrench();

            return retVal;
        }

        private bool TransformFSMRFP()
        {
            DataTable docTable = DocumentProcessing.GetStagingPivotTable((int)_fileName.JobID, _dbCon);
            DataTable fieldTable = LoadFieldAttributes("FSMRFP");


            //SqlDataAdapter da = TransformedData();

            //da.Fill(_transTable);
            _transTable = TransformedData();



            foreach (DataRow documentRow in docTable.Rows)
            {
                var docCode = documentRow.GetExactColumnStringValue("Code");
                rowIdentity.Update((int)_fileName.DocumentTypeID, (int)_fileName.ClientID, (int)_fileName.LOBID, docCode);
                if (rowIdentity.IsChanged)
                {
                    // If specific document fields are needed load them here
                    rowIdentity.AcceptChanges();
                    if (_mergeTables is null) // load a new MergeTables object the first time the rowIdentiy changes - but only when null as we don't care if docCode changes
                        _mergeTables = new MergeTables(rowIdentity, (_fileName.DataTypeID.HasValue ? (int)_fileName.DataTypeID : -1), _dbCon, _log);

                }

                foreach (DataRow fieldRow in fieldTable.Rows)
                {
                    string fieldName = fieldRow.GetExactColumnStringValue("Field_Name");
                    string english = documentRow.GetExactColumnStringValue(fieldName);

                    if (_mergeTables.GetMergeFieldNamePrefix.Any(fieldName.Contains) && !english.IsNaOrBlank() && !fieldRow.Field<bool>("isTextField")) // check if the current field needs to be merged and is not blank
                        english = _mergeTables.MergeTableData(english, fieldName, rowIdentity, _pubDbCon); // merge the field

                    if (english != null && english.Length > 0)
                    {
                        CreateEnglishRecord(fieldRow, rowIdentity, english);
                        CreateFrenchRecord(fieldRow, rowIdentity, _gen.GenerateFrench(english, rowIdentity, _jobID, fieldName));
                    }
                }
            }

            if (_transTable != null && _transTable.Rows.Count > 0)
            {
                if (!_dbCon.BulkCopy("dbo.pdi_Transformed_Data", _transTable))
                {
                    Logger.AddError(_log, $"Transform Failed for Job_ID: {_jobID} - Error: {_dbCon.LastError}");
                    return false;
                }
            }
            return true;
        }


        private bool TransformBNY()
        {
            DataTable docTable = new DataTable("document");
            if (!_dbCon.LoadDataTable("SELECT Code, Item_Name, Value FROM pdi_Data_Staging WHERE job_id = @jobID ORDER BY Sheet_Name, Code, Item_Name;", new Dictionary<string, object>(1) { { "jobID", _fileName.JobID } }, docTable)) //DocumentProcessing.GetStagingPivotTable((int)_fileName.JobID, _dbCon);
            {
                Logger.AddError(_log, $"Failed to load staging table: {_dbCon.LastError}");
                return false;
            }

            _transTable = TransformedData();
            DataTable fieldTable = LoadFieldAttributes("BNY"); // this will load the structure only as there are no fields with a BNY Load_Type

            DataRow tempRow = fieldTable.NewRow();

            tempRow["Field_Name"] = "";
            tempRow["isTextField"] = false;
            tempRow["isTableField"] = true;
            tempRow["isChartField"] = false;

            foreach (DataRow documentRow in docTable.Rows)
            {
                string docCode = documentRow.GetExactColumnStringValue("Code");
                rowIdentity.Update((int)_fileName.DocumentTypeID, (int)_fileName.ClientID, (int)_fileName.LOBID, docCode);
                if (rowIdentity.IsChanged)
                {
                    // If specific document fields are needed load them here
                    rowIdentity.AcceptChanges();
                    if (_mergeTables is null) // load a new MergeTables object the first time the rowIdentiy changes - but only when null as we don't care if docCode changes
                        _mergeTables = new MergeTables(rowIdentity, (_fileName.DataTypeID.HasValue ? (int)_fileName.DataTypeID : -1), _dbCon, _log);
                }

                string fieldName = documentRow.GetExactColumnStringValue("Item_Name");

                if (fieldName.Contains("M17"))
                {
                    PDI_DataTable dt = documentRow.GetExactColumnStringValue("Value").XMLtoDataTable();
                    // the extraction converted the data into a row based table but did not determine the headers we did add an extended property with the age in calendar years
                    if (dt != null)
                    {
                        dt.TableName = fieldName;
                        Dictionary<string, string> props = new Dictionary<string, string>(dt.ExtendedProperties.Count);
                        foreach (System.Collections.DictionaryEntry key in dt.ExtendedProperties)
                            props.Add(key.Key.ToString(), key.Value.ToString());

                        scenario = _gen.searchClientScenarioText(rowIdentity, "M17", props);
                        if (scenario != null)
                        {
                            string englishHeader = scenario.Item2;
                            string frenchHeader = scenario.Item3;
                            if (frenchHeader.IsNaOrBlank())
                                frenchHeader = _gen.GenerateFrench(englishHeader, rowIdentity, _jobID, fieldName);


                            TableList tableBuilder = new TableList(TableTypes.MultiDecimal);

                            // There are a lot of specific text entries that work for IAF but may not work for other clients, if anyone else ever uses a Bank of New York Mellon file this will need to be addressed
                            // An option was to include a "Text" spreadsheet that contained this data but there would need to be rules to apply them and it was deemed not worth the effort at this time - SK 20230209

                            if (props["SeriesLetter"].IndexOf("ETF", StringComparison.OrdinalIgnoreCase) >= 0)
                                tableBuilder.AddMultiCell("1", $"{props["SeriesLetter"]} Series", $"FNB Série"); // First row is Series Name - ETF version reverses the letter and Series text and Uses FNB instead of ETF
                            else
                                tableBuilder.AddMultiCell("1", $"Series {props["SeriesLetter"]}", $"Série {props["SeriesLetter"]}"); // First row is Series Name

                            M17_AddRow(tableBuilder, "1", dt.Rows[0]);

                            tableBuilder.AddMultiCell("2", props["PrimaryIndex"], _gen.GenerateFrench(props["PrimaryIndex"], rowIdentity, _jobID, fieldName)); // Second Row is "Primary Index Label" - use lookup for the French value


                            if (props["PrimaryIndex"].IndexOf("Benchmark Index", StringComparison.OrdinalIgnoreCase) >= 0) // Benchmark Index Scenario
                            {
                                M17_AddRow(tableBuilder, "2", dt.Rows[1]); // BM1 is the second data row

                                // add the previous Benchmark index in BM3 - Row[3] and the 1st year is the first column
                                if (!dt.Rows[3][0].ToString().IsNaOrBlank())
                                {
                                    tableBuilder.AddMultiCell("3", $"Previous {props["PrimaryIndex"]}", _gen.GenerateFrench($"Previous {props["PrimaryIndex"]}", rowIdentity, _jobID, fieldName)); // Third Row is Previous Index - use lookup for the French value
                                    M17_AddRow(tableBuilder, "3", dt.Rows[3]);
                                }
                                // a narrow benchmark index apparently cannot have a broad based index so we are done
                            }

                            else if (props["PrimaryIndex"].IndexOf("Broad-based Index", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                int rowNumber = 2;

                                M17_AddRow(tableBuilder, rowNumber.ToString(), dt.Rows[2]); // BM2 is the 2nd data row


                                // output the previous Broad based if available in BM4
                                if (!dt.Rows[4][0].ToString().IsNaOrBlank())
                                {
                                    rowNumber++;
                                    tableBuilder.AddMultiCell(rowNumber.ToString(), $"Previous {props["PrimaryIndex"]}", _gen.GenerateFrench($"Previous {props["PrimaryIndex"]}", rowIdentity, _jobID, fieldName)); // Next Row is Previous Primary Index - use lookup for the French value
                                    M17_AddRow(tableBuilder, rowNumber.ToString(), dt.Rows[4]); // BM3 has the Previous Broad Based Index 
                                }

                                // ouput the narrow benchmark if available in BM1
                                if (!dt.Rows[1][0].ToString().IsNaOrBlank())
                                {
                                    rowNumber++;
                                    tableBuilder.AddMultiCell(rowNumber.ToString(), "Benchmark Index", _gen.GenerateFrench("Benchmark Index", rowIdentity, _jobID, fieldName)); // Next Row is Benchmark Index - use lookup for the French value
                                    M17_AddRow(tableBuilder, rowNumber.ToString(), dt.Rows[1]); // BM1 has the Benchmark Index 
                                }

                                // ouput the previous narrow benchmark if available in BM3
                                if (!dt.Rows[3][0].ToString().IsNaOrBlank())
                                {
                                    rowNumber++;
                                    tableBuilder.AddMultiCell(rowNumber.ToString(), "Previous Benchmark Index", _gen.GenerateFrench("Previous Benchmark Index", rowIdentity, _jobID, fieldName)); // Fifth Row is Previous Benchmark Index - use lookup for the French value
                                    M17_AddRow(tableBuilder, rowNumber.ToString(), dt.Rows[3]); // BM4 has the Previous Broad Based Index
                                }
                            }

                            tempRow["Field_Name"] = fieldName; // update the fake fieldRow with the current column name
                            tempRow["isTextField"] = false;
                            tempRow["isTableField"] = true;
                            tempRow["isChartField"] = false;

                            // format the inception date 
                            string[] formatDates = _gen.shortFormDateUS(props["InceptionDate"], "iA"); // iA[Month2Digit] is a new "Global" format for handling M17's odd date label format.

                            props["InceptionDate"] = formatDates[0];
                            CreateEnglishRecord(tempRow, rowIdentity, tableBuilder.GetTableString().InsertHeaderRow(englishHeader, DataRowInsert.FirstRow).ReplaceByDictionary(props));
                            props["InceptionDate"] = formatDates[1];
                            CreateFrenchRecord(tempRow, rowIdentity, tableBuilder.GetTableString(true).InsertHeaderRow(frenchHeader, DataRowInsert.FirstRow).ReplaceByDictionary(props));

                        }


                    }
                }
                // M15
                else if (fieldName.Contains("M15"))
                {
                    string english = documentRow.GetExactColumnStringValue("Value");

                    if (english != null && english.Length > 0)
                    {

                        PDI_DataTable dtPDI = english.XMLtoDataTable();
                        string french = _gen.GenerateFrench(english, rowIdentity, _jobID, fieldName);

                        if (_mergeTables.GetMergeFieldNamePrefix.Any(fieldName.Contains)) // check if the current field needs to be merged
                        {
                            english = _mergeTables.MergeTableData(english, fieldName, rowIdentity, _pubDbCon); // merge the field
                            french = _mergeTables.MergeTableData(french, fieldName, rowIdentity, _pubDbCon, true);
                        }
                        //Logger.AddError(null, $"Merging {dc.ColumnName} for {rowIdentity.DocumentCode}");

                        if (english != null && english.Length > 0) // check again after the merge - if a failure occurred the value will be null 
                        {
                            tempRow["Field_Name"] = fieldName;
                            tempRow["isTextField"] = false;
                            tempRow["isTableField"] = false;
                            tempRow["isChartField"] = true; // M15 is a chart field
                            CreateEnglishRecord(tempRow, rowIdentity, english);
                            CreateFrenchRecord(tempRow, rowIdentity, french);

                            if (dtPDI.ExtendedProperties.ContainsKey("SeriesLetter"))
                            {
                                string englishHeaderHistoric = _mergeTables.GetHistoricHeaderString(fieldName);
                                if (englishHeaderHistoric is null || !englishHeaderHistoric.Contains(dtPDI.ExtendedProperties["SeriesLetter"].ToString())) // generate the headers if they are missing or different
                                {

                                    tempRow["Field_Name"] = fieldName + "h";
                                    tempRow["isTextField"] = true; // M15h is a header field
                                    tempRow["isTableField"] = false;
                                    tempRow["isChartField"] = false;

                                    string englishHeader = $"Series {dtPDI.ExtendedProperties["SeriesLetter"]}";
                                    string frenchHeader = $"Série {dtPDI.ExtendedProperties["SeriesLetter"]}";


                                    if (dtPDI.ExtendedProperties["SeriesLetter"].ToString().IndexOf("ETF", StringComparison.OrdinalIgnoreCase) >= 0)
                                    {
                                        englishHeader = $"{dtPDI.ExtendedProperties["SeriesLetter"]} Series";
                                        frenchHeader = $"FNB Série";
                                    }

                                    if (englishHeaderHistoric != null && englishHeaderHistoric.Length > 0)
                                        Logger.AddError(_log, $"Headers did not match historic header value when adding {fieldName}h - was {englishHeaderHistoric} now {englishHeader}");

                                    CreateEnglishRecord(tempRow, rowIdentity, englishHeader);
                                    CreateFrenchRecord(tempRow, rowIdentity, frenchHeader);
                                }

                            }
                        }
                    }
                }

            }
            if (_transTable != null && _transTable.Rows.Count > 0)
            {
                if (!_dbCon.BulkCopy("dbo.pdi_Transformed_Data", _transTable))
                {
                    Logger.AddError(_log, $"Transform Failed for Job_ID: {_jobID} - Error: {_dbCon.LastError}");
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// Standard rule definition for adding M17 data values where the last two values determine if it is a 10 year or less table and then the remaining 3 values our output if not blank
        /// </summary>
        /// <param name="tableBuilder">The TableList being constructed</param>
        /// <param name="rowNumber">The current row number to add the data too</param>
        /// <param name="dr">The source data row (expecting 5 columns of data)</param>
        public void M17_AddRow(TableList tableBuilder, string rowNumber, DataRow dr)
        {
            if (dr.Table.Columns.Count < 5)
            {
                Logger.AddError(_log, $"M17 row could not be added to row {rowNumber} as it had less than 5 columns");
                return;
            }

            if (!dr.GetStringValue(3).IsNaOrBlank())
                tableBuilder.AddMultiCell(rowNumber, dr.GetStringValue(3), dr.GetStringValue(3)); // Scenario 4
            else
                tableBuilder.AddMultiCell(rowNumber, dr.GetStringValue(4), dr.GetStringValue(4)); // Scenario 1, 2, 3
            for (int c = 2; c >= 0; c--)
            {
                if (!dr.GetStringValue(c).IsNaOrBlank()) // Scenario 1, 4    
                    tableBuilder.AddMultiCell(rowNumber, dr.GetStringValue(c), dr.GetStringValue(c));
            }
        }


        public bool TransformBAU()
        {
            string dataAsAtDate_EN = string.Empty, dataAsAtDate_FR = string.Empty, dataAsAtDate_Raw = string.Empty;

            fieldList.Clear();
            fieldList.Add("FFDocAgeStatusID");
            fieldList.Add("IsProforma");
            fieldList.Add("SeriesDesignationEN");
            fieldList.Add("SeriesDesignationFR");
            fieldList.Add("DisplaySeriesNameEN");
            fieldList.Add("DisplaySeriesNameFR");
            fieldList.Add("SwitchToSeries");
            fieldList.Add("Switching");
            fieldList.Add("IsPool");
            fieldList.Add("InceptionDate");
            fieldList.Add("IsMERAvailable");
            fieldList.Add("MerFeeWaiver");
            fieldList.Add("AgeCalendarYears");
            fieldList.Add("NegativeReturnCalendarYears");
            fieldList.Add("PerformanceReset");
            fieldList.Add("PerformanceResetDate");
            fieldList.Add("IsUnderlying");
            fieldList.Add("UnderlyingIndexNameEN");
            fieldList.Add("UnderlyingIndexNameFR");
            fieldList.Add("Last_Filing_Date");
            fieldList.Add("SeriesLetter");
            fieldList.Add("SeriesLetterFR");
            fieldList.Add("FundFamilyNameEN");
            fieldList.Add("FundFamilyNameFR");

            DataTable docTable = DocumentProcessing.GetStagingPivotTable((int)_fileName.JobID, _dbCon);
            DataTable fieldTable = LoadFieldAttributes();

            DataTable dataStaging = LoadDataStaging();

            DataTable NumberOfInvestments23 = null;

            _transTable = TransformedData();
            foreach (DataRow documentRow in docTable.Rows)
            {
                if (!documentRow.GetExactColumnBoolValue("IsActiveStatus"))
                    continue; // skip current row if the status is not active - no transformed output for documents that are not active

                string code = documentRow.GetExactColumnStringValue("Code");
                if (code.IsNaOrBlank())
                    code = documentRow.GetExactColumnStringValue("Code");

                // update the current row identifiers
                rowIdentity.Update((int)_fileName.DocumentTypeID, (int)_fileName.ClientID, (int)_fileName.LOBID, code);
                if (rowIdentity.IsChanged)
                {
                    // if there have been changes update the documentFields from the database
                    documentFields = _gen.getPublisherDocumentFields(rowIdentity, fieldList);
                    if (_fileName.GetDocumentType == DocumentTypeID.FS)
                        rowFields = documentRow.GetDataRowDictionary(documentFields);

                    rowIdentity.AcceptChanges();

                    // it won't take much to verify the DataAsAtDate each time the identity changes
                    if (!documentRow.GetExactColumnStringValue("DataAsAtDate").IsNaOrBlank())
                    {
                        dataAsAtDate_Raw = documentRow.GetExactColumnStringValue("DataAsAtDate");
                        month = _gen.longFormDate(dataAsAtDate_Raw, LongMonthNames);
                        dataAsAtDate_EN = month[0];
                        dataAsAtDate_FR = month[1];
                    }
                    else
                    {
                        // this is an error condition unless we are running FS/MRFP/SFS/SC
                        if (_fileName.GetDocumentType != DocumentTypeID.FS && _fileName.GetDocumentType != DocumentTypeID.MRFP && _fileName.GetDocumentType != DocumentTypeID.SFS && _fileName.GetDocumentType != DocumentTypeID.SFSBOOK && _fileName.GetDocumentType != DocumentTypeID.QPDBOOK)
                            Logger.AddError(_log, $"Critical Error in Transform - {code} is missing a valid DataAsAtDate");
                        dataAsAtDate_Raw = string.Empty;
                        dataAsAtDate_EN = string.Empty;
                        dataAsAtDate_FR = string.Empty;
                    }

                    if (_mergeTables is null) // load a new MergeTables object the first time the rowIdentiy changes - but only when null as we don't care if docCode changes
                        _mergeTables = new MergeTables(rowIdentity, (_fileName.DataTypeID.HasValue ? (int)_fileName.DataTypeID : -1), _dbCon, _log);
                }

                foreach (DataRow fieldRow in fieldTable.Rows)
                {
                    //ETF Fields
                    string inputDate = string.Empty;
                    string fundCode = string.Empty;
                    string ticker = string.Empty;
                    string english = string.Empty;
                    string french = string.Empty;
                    int FFDocAgeID = -1;
                    int ageCalendar = -1;
                    string fieldName = fieldRow.GetExactColumnStringValue("Field_Name");
                    switch (fieldName)
                    {
                        #region Funds
                        case "FF4": //Prepare FF4 English & French Records
                        case "E4": //Prepare E4 English & French Records
                            inputDate = documentRow.GetExactColumnStringValue("FilingDate");
                            // this uses the function created for FF4 but works the same for E4 and is a wrapper for the more generic document field query
                            if (_gen.usePrelimDate(rowIdentity))
                                inputDate = documentRow.GetExactColumnStringValue("PrelimDate");

                            if (!(inputDate is null))
                            {
                                month = _gen.longFormDate(inputDate, LongMonthNames);

                                CreateEnglishRecord(fieldRow, rowIdentity, month[0]); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, month[1]); //French Record
                            }
                            break;

                        case "FF9h":
                        case "FF15b":
                        case "FF16h":
                        case "FF17h":
                        case "E7hb":  //Prepare E7hb, E7hc, E9h, E16h, E15b, E17h English and French fields
                        case "E7hc":
                        case "E9h":
                        case "E15b":
                        case "E16h":
                        case "E17h":
                            english = string.Empty;
                            french = string.Empty;

                            //Extract scenarios
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null && scenario.Item1 != null)
                            {
                                english = scenario.Item2.ReplaceCI("<DataAsAtDate>", dataAsAtDate_EN);
                                french = scenario.Item3.ReplaceCI("<DataAsAtDate>", dataAsAtDate_FR);
                            }
                            CreateEnglishRecord(fieldRow, rowIdentity, english);  //English Record
                            CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record

                            break;

                        case "FF8": //Prepare FF8 English & French Records
                        case "E8": //Prepare E8 English & French Records
                            inputDate = documentFields["InceptionDate"]; // generic.getPublisherDocumentField(Row.DocumentTypeID, Row.ClientID, Row.LOBID, Row.DocumentCode, "InceptionDate");//generic.getIneptionDate(Row.DocumentTypeID, Row.ClientID, Row.LOBID, Row.DocumentCode);

                            if (!(inputDate is null))
                            {
                                month = _gen.longFormDate(inputDate, LongMonthNames);

                                CreateEnglishRecord(fieldRow, rowIdentity, month[0]); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, month[1]); //French Record
                            }
                            break;

                        case "FF9": //Prepare FF9 field
                        case "E9":  //Prepare E9 field
                            FFDocAgeID = -1;
                            if (int.TryParse(documentFields["FFDocAgeStatusID"], out FFDocAgeID))
                            {
                                if (FFDocAgeID == 0 && !documentFields["PerformanceReset"].ToBool())
                                {
                                    //the scenario lookup handles the IsPool business rule
                                    scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                                    if (scenario != null)
                                    {
                                        CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2); //English Record
                                        CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3); //French Record
                                    }
                                }
                                else if ((FFDocAgeID > 0 && FFDocAgeID <= 5) || documentFields["PerformanceReset"].ToBool())
                                {
                                    if (documentFields["IsProforma"].ToBool())
                                    {
                                        CreateEnglishRecord(fieldRow, rowIdentity, BulletCurrency[0]); //English Record
                                        CreateFrenchRecord(fieldRow, rowIdentity, BulletCurrency[1]);  //French Record
                                    }
                                    else
                                    {
                                        if (!documentRow.GetPartialColumnStringValue(fieldName + "_EN").IsNaOrBlank()) //if upper conditions don't meet, there must be a value in the cell
                                        {
                                            CreateEnglishRecord(fieldRow, rowIdentity, documentRow.GetPartialColumnStringValue(fieldName + "_EN")); //English Record
                                            CreateFrenchRecord(fieldRow, rowIdentity, documentRow.GetPartialColumnStringValue(fieldName + "_FR")); //French Record
                                        }
                                        else
                                        {
                                            CreateEnglishRecord(fieldRow, rowIdentity, Generic.MISSING_EN_TEXT + "Total value of fund"); //English Record
                                            CreateFrenchRecord(fieldRow, rowIdentity, Generic.MISSING_FR_TEXT + "Total value of fund"); //French Record
                                        }
                                    }
                                }
                            }
                            else
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, "TryParsing error"); //English Record 
                                CreateFrenchRecord(fieldRow, rowIdentity, "TryParsing error");  //French Record
                            }
                            break;

                        case "FF10": //Prepare FF10 field
                        case "E10": //Prepare E10 field
                            FFDocAgeID = -1;
                            if (int.TryParse(documentFields["FFDocAgeStatusID"], out FFDocAgeID))
                            {
                                if (!documentFields["IsMERAvailable"].ToBool())
                                {
                                    scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                                    if (scenario != null)
                                    {
                                        if (FFDocAgeID == 0 || FFDocAgeID == 2 || FFDocAgeID == 5)
                                        {
                                            CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2); //English Record
                                            CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3); //French Record
                                        }
                                        else if (FFDocAgeID == 1 || FFDocAgeID == 3)
                                        {
                                            english = scenario.Item2.ReplaceCI("<DisplaySeriesNameEN>", documentFields["DisplaySeriesNameEN"]);
                                            french = scenario.Item3.ReplaceCI("<DisplaySeriesNameFR>", documentFields["DisplaySeriesNameFR"]);

                                            CreateEnglishRecord(fieldRow, rowIdentity, english); //English Record
                                            CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record
                                        }
                                    }
                                }
                                else
                                {
                                    if (documentFields["IsProforma"].ToBool())
                                    {
                                        CreateEnglishRecord(fieldRow, rowIdentity, BulletPercent[0]); //English Record
                                        CreateFrenchRecord(fieldRow, rowIdentity, BulletPercent[1]); //French Record
                                    }
                                    else
                                    {
                                        int maxScale = Math.Max(Math.Max(documentRow.GetExactColumnStringValue("MerPercent").GetScale(), documentRow.GetExactColumnStringValue("TerPercent").GetScale()), documentRow.GetExactColumnStringValue("TotalFundExpensePercent").GetScale());

                                        CreateEnglishRecord(fieldRow, rowIdentity, documentRow.GetExactColumnStringValue("MerPercent").ToPercent("en-CA", maxScale)); //English Record
                                        CreateFrenchRecord(fieldRow, rowIdentity, documentRow.GetExactColumnStringValue("MerPercent").ToPercent("en-CA", maxScale)); //French Record
                                    }
                                }
                            }
                            else
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, "TryParsing error"); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, "TryParsing error"); //French Record
                            }
                            break;


                        case "FF16": //Prepare FF16, FF17 and FF40 English & French Records
                        case "FF17":
                        case "FF40":
                        case "E16":  //Prepare E16, E17 and E40 English & French Records
                        case "E17":
                        case "E40":
                            // grab the English version of the field and check for Null 

                            english = documentRow.GetExactColumnStringValue(fieldName + "_EN");

                            // the extract has been changed to always output the 16/17/40 tables when isProforma and IsActiveStatus
                            if (!english.IsNaOrBlank())
                            {
                                if (int.TryParse(documentFields["FFDocAgeStatusID"], out int ffDocAgeID3))
                                {
                                    if (ffDocAgeID3 != 0)
                                    {
                                        if (!documentFields["IsProforma"].ToBool())
                                        {
                                            // get the FR versions of the FieldName
                                            french = documentRow.GetExactColumnStringValue(fieldName + "_FR");
                                            CreateEnglishRecord(fieldRow, rowIdentity, english); //English Record
                                            CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record
                                        }
                                        else
                                        {
                                            CreateEnglishRecord(fieldRow, rowIdentity, BulletInvestments[0]); //English Record
                                            CreateFrenchRecord(fieldRow, rowIdentity, BulletInvestments[1]);  //French Record
                                        }
                                    }
                                }
                                else
                                {
                                    CreateEnglishRecord(fieldRow, rowIdentity, "TryParsing Error on FFDocAgeStatusID"); //English Record
                                    CreateFrenchRecord(fieldRow, rowIdentity, "TryParsing Error on FFDocAgeStatusID"); //French Record
                                }
                            }
                            break;

                        case "FF17sh": //Prepare FF17sh English & French Records
                        case "E17sh":  //Prepare E17sh English & French Records
                        case "FF40sh": //Prepare FF40sh English & French Records
                        case "E40sh": //Prepare E40sh English & French Records
                            english = documentRow.GetPartialColumnStringValue(fieldName);
                            if (!english.IsNaOrBlank())
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, english);  //English Record  
                                french = _gen.verifyFrenchTableText("", english, rowIdentity, _jobID, fieldName);
                                CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record
                            }
                            break;

                        case "FF18": //Prepare FF18 record
                        case "E18": //Prepare E18 record


                            if (int.TryParse(documentFields["FFDocAgeStatusID"], out FFDocAgeID) && FFDocAgeID != 0) //&& !numInvestments.IsNaOrBlank() && !top10InvestTotalPercent.IsNaOrBlank()
                            {
                                scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                                if (scenario != null)
                                {
                                    english = scenario.Item2;
                                    french = scenario.Item3;

                                    if (documentFields["IsProforma"].ToBool())
                                    {
                                        english = english.ReplaceCI("<TotalNumberOfInvestments>", BulletNumber[0]);
                                        french = french.ReplaceCI("<TotalNumberOfInvestments>", BulletNumber[1]);

                                        english = english.ReplaceCI("<Top10InvestTotalPercent>", BulletPercent[0]);
                                        french = french.ReplaceCI("<Top10InvestTotalPercent>", BulletPercent[1]);
                                    }
                                    else
                                    {
                                        string numInvestments = documentRow.GetPartialColumnStringValue("NumberOfInvestments"); // due to misspelling Totabl
                                        string top10InvestTotalPercent = documentRow.GetExactColumnStringValue("Top10InvestTotalPercent");

                                        english = english.ReplaceCI("<TotalNumberOfInvestments>", numInvestments.ToDecimal()); // Added formatting for number > 999 as per Wajeb request SK 2021-05-14
                                        french = french.ReplaceCI("<TotalNumberOfInvestments>", numInvestments.ToDecimal("fr-CA"));

                                        english = english.ReplaceCI("<Top10InvestTotalPercent>", top10InvestTotalPercent.ToPercent());
                                        french = french.ReplaceCI("<Top10InvestTotalPercent>", top10InvestTotalPercent.ToPercent()); //US7287 US8219 - no French formatting
                                    }
                                    CreateEnglishRecord(fieldRow, rowIdentity, english); //English Record
                                    CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record
                                }
                            }
                            break;

                        case "FF20": //Prepare FF20 field
                        case "E20": //Prepare E20 field    
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null)
                            {
                                english = scenario.Item2;
                                french = scenario.Item3;

                                //<InceptionDate>
                                string[] localDate = _gen.longFormDate(documentFields["InceptionDate"]);
                                english = english.ReplaceCI("<InceptionDate>", localDate[0]);
                                french = french.ReplaceCI("<InceptionDate>", localDate[1]);

                                //<PerformanceResetDate>
                                localDate = _gen.longFormDate(documentFields["PerformanceResetDate"]);
                                english = english.ReplaceCI("<PerformanceResetDate>", localDate[0]);
                                french = french.ReplaceCI("<PerformanceResetDate>", localDate[1]);

                                //<UnderlyingIndexName> E20
                                english = english.ReplaceCI("<UnderlyingIndexNameEN>", documentFields["UnderlyingIndexNameEN"]);
                                french = french.ReplaceCI("<UnderlyingIndexNameFR>", documentFields["UnderlyingIndexNameFR"]);

                                ProxySeriesSubstitution(documentRow, ref english, ref french);

                                if (documentFields["IsProforma"].ToBool())
                                {
                                    //<AgeCalendarYears>
                                    english = english.ReplaceCI("<AgeCalendarYears>", BulletNumber[0]);
                                    french = french.ReplaceCI("<AgeCalendarYears>", BulletNumber[1]);
                                }

                                CreateEnglishRecord(fieldRow, rowIdentity, english.ReplaceByDictionary(documentFields));  //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, french.ReplaceByDictionary(documentFields)); //French Record
                            }
                            break;

                        case "FF27"://Prepare FF27
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null)
                            {
                                english = scenario.Item2;
                                french = scenario.Item3;
                                ProxySeriesSubstitution(documentRow, ref english, ref french);

                                CreateEnglishRecord(fieldRow, rowIdentity, english.ReplaceByDictionary(documentFields));  //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, french.ReplaceByDictionary(documentFields)); //French Record
                            }

                            break;


                        case "FF21": //Prepare FF21
                        case "E21":  //Prepare E21                          {
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null)
                            {
                                inputDate = documentFields["InceptionDate"];


                                if (!inputDate.IsNaOrBlank()) // use extension rather than isnullorblank
                                {
                                    string[] inceptionDate = _gen.longFormDate(inputDate, LongMonthNames);

                                    english = scenario.Item2.ReplaceCI("<DisplaySeriesNameEN>", documentFields["DisplaySeriesNameEN"]).ReplaceCI("<InceptionDate>", inceptionDate[0]).ReplaceCI("<DataAsAtDate>", dataAsAtDate_EN);
                                    french = scenario.Item3.ReplaceCI("<DisplaySeriesNameFR>", documentFields["DisplaySeriesNameFR"]).ReplaceCI("<InceptionDate>", inceptionDate[1]).ReplaceCI("<DataAsAtDate>", dataAsAtDate_FR);

                                    string firstOffDate = documentRow.GetExactColumnStringValue("FirstOfferingDate");
                                    if (!firstOffDate.IsNaOrBlank())
                                    {
                                        string[] firstOffDateFormatted = _gen.longFormDate(firstOffDate, LongMonthNames);

                                        english = english.ReplaceCI("<FirstOfferingDate>", firstOffDateFormatted[0]);
                                        french = french.ReplaceCI("<FirstOfferingDate>", firstOffDateFormatted[1]);
                                    }

                                    if (documentFields["IsProforma"].ToBool())
                                    {
                                        english = english.ReplaceCI("<AvgReturnPercent>", BulletPercent[0]).ReplaceCI("<AvgReturnAmount>", BulletCurrency[0]);
                                        french = french.ReplaceCI("<AvgReturnPercent>", BulletPercent[1]).ReplaceCI("<AvgReturnAmount>", BulletCurrency[1]);
                                    }
                                    else
                                    {
                                        english = english.ReplaceCI("<AvgReturnPercent>", documentRow.GetExactColumnStringValue("AvgReturnPercent").ToPercent()).ReplaceCI("<AvgReturnAmount>", documentRow.GetExactColumnStringValue("AvgReturnAmount").ToCurrency());
                                        french = french.ReplaceCI("<AvgReturnPercent>", documentRow.GetExactColumnStringValue("AvgReturnPercent").ToPercent("fr-CA")).ReplaceCI("<AvgReturnAmount>", documentRow.GetExactColumnStringValue("AvgReturnAmount").ToCurrency("fr-CA"));
                                    }
                                    CreateEnglishRecord(fieldRow, rowIdentity, english.ReplaceByDictionary(documentFields)); //English Record
                                    CreateFrenchRecord(fieldRow, rowIdentity, french.ReplaceByDictionary(documentFields)); //French Record
                                }
                            }
                            break;

                        case "FF22a": //Prepare FF22a field
                        case "E22a": //Prepare E22a field
                        case "FF36": // Prepare FF36 field
                        case "E36": // Prepare E36 field
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null)
                            {
                                english = scenario.Item2;
                                french = scenario.Item3;

                                if (documentFields["IsProforma"].ToBool())
                                {
                                    //<AgeCalendarYears>
                                    english = english.ReplaceCI("<AgeCalendarYears>", BulletNumber[0]);
                                    french = french.ReplaceCI("<AgeCalendarYears>", BulletNumber[1]);

                                    //<NegativeReturnCalendarYears>
                                    english = english.ReplaceCI("<NegativeReturnCalendarYears>", BulletNumber[0]);
                                    french = french.ReplaceCI("<NegativeReturnCalendarYears>", BulletNumber[1]);
                                }

                                CreateEnglishRecord(fieldRow, rowIdentity, english.ReplaceByDictionary(documentFields)); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, french.ReplaceByDictionary(documentFields));  //French Record
                            }
                            break;

                        case "FF22b": //Prepare FF22b English & French Records
                        case "E22b": //Prepare E22b English & French Records
                            //TODO: The table is not currently being created in extract
                            if (!documentRow.GetExactColumnStringValue(fieldName).IsNaOrBlank())
                            {
                                if (int.TryParse(documentFields["FFDocAgeStatusID"], out FFDocAgeID) && int.TryParse(documentFields["AgeCalendarYears"], out ageCalendar))
                                {
                                    if (FFDocAgeID == 4 && ageCalendar > 0)
                                    {
                                        CreateEnglishRecord(fieldRow, rowIdentity, documentRow.GetExactColumnStringValue(fieldName)); //English Record
                                        CreateFrenchRecord(fieldRow, rowIdentity, documentRow.GetExactColumnStringValue(fieldName)); //French Record
                                    }
                                }
                            }
                            break;
                        case "FF24": // only FF24 BAU so far no ETF US14503
                            english = string.Empty;
                            french = string.Empty;

                            //Extract scenarios
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null && scenario.Item1 != null)
                            {
                                english = scenario.Item2.ReplaceCI("<DataAsAtDate>", dataAsAtDate_EN);
                                french = scenario.Item3.ReplaceCI("<DataAsAtDate>", dataAsAtDate_FR);

                                CreateEnglishRecord(fieldRow, rowIdentity, english.ReplaceByDictionary(documentFields));  //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, french.ReplaceByDictionary(documentFields)); //French Record
                            }
                            break;
                        case "FF29": //Prepare FF29 field
                        case "E29": //Prepare E29 field

                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null)
                            {
                                english = scenario.Item2;
                                french = scenario.Item3;

                                //<<MerDate>>
                                if (documentFields["IsMERAvailable"].ToBool())
                                {
                                    string[] month = _gen.longFormDate(documentRow.GetExactColumnStringValue("MerDate"), LongMonthNames);

                                    english = english.ReplaceCI("<MerDate>", month[0]);
                                    french = french.ReplaceCI("<MerDate>", month[1]);
                                }

                                ProxySeriesSubstitution(documentRow, ref english, ref french);

                                if (documentFields["IsProforma"].ToBool())
                                {
                                    //<MgtFeePercent>
                                    english = english.ReplaceCI("<MgtFeePercent>", BulletPercent[0]);
                                    french = french.ReplaceCI("<MgtFeePercent>", BulletPercent[1]);

                                    //<AdminFeePercent> 
                                    english = english.ReplaceCI("<AdminFeePercent>", BulletPercent[0]);
                                    french = french.ReplaceCI("<AdminFeePercent>", BulletPercent[1]);

                                    //<TotalFundExpensePercent> 
                                    english = english.ReplaceCI("<TotalFundExpensePercent>", BulletPercent[0]);
                                    french = french.ReplaceCI("<TotalFundExpensePercent>", BulletPercent[1]);

                                    //<TotalFundExpenseAmount> 
                                    english = english.ReplaceCI("<TotalFundExpenseAmount>", BulletNumber[0]);
                                    french = french.ReplaceCI("<TotalFundExpenseAmount>", BulletNumber[1]);

                                    // new Switch MerTer - us18186
                                    //<TotalSwitchFundExpensePercent> 
                                    english = english.ReplaceCI("<TotalSwitchFundExpensePercent>", BulletPercent[0]);
                                    french = french.ReplaceCI("<TotalSwitchFundExpensePercent>", BulletPercent[1]);

                                    //<TotalSwitchFundExpenseAmount> 
                                    english = english.ReplaceCI("<TotalSwitchFundExpenseAmount>", BulletNumber[0]);
                                    french = french.ReplaceCI("<TotalSwitchFundExpenseAmount>", BulletNumber[1]);
                                }
                                else
                                {

                                    int maxScale = Math.Max(Math.Max(Math.Max(documentRow.GetExactColumnStringValue("MgtFeePercent").GetScale(), documentRow.GetExactColumnStringValue("AdminFeePercent").GetScale()), documentRow.GetExactColumnStringValue("TotalFundExpensePercent").GetScale()), documentRow.GetExactColumnStringValue("TotalSwitchFundExpensePercent").GetScale());
                                    //<MgtFeePercent>
                                    english = english.ReplaceCI("<MgtFeePercent>", documentRow.GetExactColumnStringValue("MgtFeePercent").ToPercent("en-CA", maxScale));
                                    french = french.ReplaceCI("<MgtFeePercent>", documentRow.GetExactColumnStringValue("MgtFeePercent").ToPercent("fr-CA", maxScale));

                                    //<AdminFeePercent> 
                                    english = english.ReplaceCI("<AdminFeePercent>", documentRow.GetExactColumnStringValue("AdminFeePercent").ToPercent("en-CA", maxScale));
                                    french = french.ReplaceCI("<AdminFeePercent>", documentRow.GetExactColumnStringValue("AdminFeePercent").ToPercent("fr-CA", maxScale));

                                    //<TotalFundExpensePercent> 
                                    english = english.ReplaceCI("<TotalFundExpensePercent>", documentRow.GetExactColumnStringValue("TotalFundExpensePercent").ToPercent("en-CA", maxScale));
                                    french = french.ReplaceCI("<TotalFundExpensePercent>", documentRow.GetExactColumnStringValue("TotalFundExpensePercent").ToPercent("fr-CA", maxScale));

                                    //<TotalFundExpenseAmount> 
                                    english = english.ReplaceCI("<TotalFundExpenseAmount>", documentRow.GetExactColumnStringValue("TotalFundExpenseAmount").ToCurrencyDecimal());
                                    french = french.ReplaceCI("<TotalFundExpenseAmount>", documentRow.GetExactColumnStringValue("TotalFundExpenseAmount").ToCurrencyDecimal("fr-CA"));

                                    // new Switch MerTer - us18186
                                    //<TotalSwitchFundExpensePercent> 
                                    english = english.ReplaceCI("<TotalSwitchFundExpensePercent>", documentRow.GetExactColumnStringValue("TotalSwitchFundExpensePercent").ToPercent("en-CA", maxScale));
                                    french = french.ReplaceCI("<TotalSwitchFundExpensePercent>", documentRow.GetExactColumnStringValue("TotalSwitchFundExpensePercent").ToPercent("fr-CA", maxScale));

                                    //<TotalSwitchFundExpenseAmount> 
                                    english = english.ReplaceCI("<TotalSwitchFundExpenseAmount>", documentRow.GetExactColumnStringValue("TotalSwitchFundExpenseAmount").ToCurrencyDecimal());
                                    french = french.ReplaceCI("<TotalSwitchFundExpenseAmount>", documentRow.GetExactColumnStringValue("TotalSwitchFundExpenseAmount").ToCurrencyDecimal("fr-CA"));

                                }
                                CreateEnglishRecord(fieldRow, rowIdentity, english.ReplaceByDictionary(documentFields)); //English Record - 20210907 - Wajeb requested DisplaySeriesNameEN/FR replacement - agreed to add all publisher fields
                                CreateFrenchRecord(fieldRow, rowIdentity, french.ReplaceByDictionary(documentFields));  //French Record
                            }
                            break;

                        case "FF34": //Prepare FF34 field
                        case "E34": //Prepare E34 field
                            if (documentFields["IsMERAvailable"].ToBool())
                            {
                                scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                                if (scenario != null)
                                {
                                    if (documentFields["IsProforma"].ToBool())
                                    {
                                        // new Switch MerTer - us18186
                                        english = scenario.Item2.ReplaceCI("<MerPercent>", BulletPercent[0]).ReplaceCI("<TerPercent>", BulletPercent[0]).ReplaceCI("<TotalFundExpensePercent>", BulletPercent[0]).ReplaceCI("<SwitchMerPercent>", BulletPercent[0]).ReplaceCI("<SwitchTerPercent>", BulletPercent[0]).ReplaceCI("<TotalSwitchFundExpensePercent>", BulletPercent[0]);
                                        french = scenario.Item3.ReplaceCI("<MerPercent>", BulletPercent[1]).ReplaceCI("<TerPercent>", BulletPercent[1]).ReplaceCI("<TotalFundExpensePercent>", BulletPercent[1]).ReplaceCI("<SwitchMerPercent>", BulletPercent[1]).ReplaceCI("<SwitchTerPercent>", BulletPercent[1]).ReplaceCI("<TotalSwitchFundExpensePercent>", BulletPercent[1]);

                                        // add FundExpenseAmount - us18186
                                        //<TotalFundExpenseAmount> 
                                        english = english.ReplaceCI("<TotalFundExpenseAmount>", BulletNumber[0]).ReplaceCI("<TotalSwitchFundExpenseAmount>", BulletNumber[0]);
                                        french = french.ReplaceCI("<TotalFundExpenseAmount>", BulletNumber[1]).ReplaceCI("<TotalSwitchFundExpenseAmount>", BulletNumber[1]);
                                    }
                                    else
                                    {
                                        int maxScale = Math.Max(Math.Max(documentRow.GetExactColumnStringValue("MerPercent").GetScale(), documentRow.GetExactColumnStringValue("TerPercent").GetScale()), documentRow.GetExactColumnStringValue("TotalFundExpensePercent").GetScale());

                                        english = scenario.Item2.ReplaceCI("<MerPercent>", documentRow.GetExactColumnStringValue("MerPercent").ToPercent("en-CA", maxScale)).ReplaceCI("<TerPercent>", documentRow.GetExactColumnStringValue("TerPercent").ToPercent("en-CA", maxScale)).ReplaceCI("<TotalFundExpensePercent>", documentRow.GetExactColumnStringValue("TotalFundExpensePercent").ToPercent("en-CA", maxScale));
                                        french = scenario.Item3.ReplaceCI("<MerPercent>", documentRow.GetExactColumnStringValue("MerPercent").ToPercent("en-CA", maxScale)).ReplaceCI("<TerPercent>", documentRow.GetExactColumnStringValue("TerPercent").ToPercent("en-CA", maxScale)).ReplaceCI("<TotalFundExpensePercent>", documentRow.GetExactColumnStringValue("TotalFundExpensePercent").ToPercent("en-CA", maxScale));

                                        // add Switch replacements - us18186 
                                        int maxScaleSwitch = Math.Max(Math.Max(documentRow.GetExactColumnStringValue("SwitchMerPercent").GetScale(), documentRow.GetExactColumnStringValue("SwitchTerPercent").GetScale()), documentRow.GetExactColumnStringValue("TotalSwitchFundExpensePercent").GetScale());

                                        english = english.ReplaceCI("<SwitchMerPercent>", documentRow.GetExactColumnStringValue("SwitchMerPercent").ToPercent("en-CA", maxScale)).ReplaceCI("<SwitchTerPercent>", documentRow.GetExactColumnStringValue("SwitchTerPercent").ToPercent("en-CA", maxScale)).ReplaceCI("<TotalSwitchFundExpensePercent>", documentRow.GetExactColumnStringValue("TotalSwitchFundExpensePercent").ToPercent("en-CA", maxScale));
                                        french = french.ReplaceCI("<SwitchMerPercent>", documentRow.GetExactColumnStringValue("SwitchMerPercent").ToPercent("en-CA", maxScale)).ReplaceCI("<SwitchTerPercent>", documentRow.GetExactColumnStringValue("SwitchTerPercent").ToPercent("en-CA", maxScale)).ReplaceCI("<TotalSwitchFundExpensePercent>", documentRow.GetExactColumnStringValue("TotalSwitchFundExpensePercent").ToPercent("en-CA", maxScale));

                                        // add FundExpenseAmount - us18186
                                        //<TotalFundExpenseAmount> 
                                        english = english.ReplaceCI("<TotalFundExpenseAmount>", documentRow.GetExactColumnStringValue("TotalFundExpenseAmount").ToCurrencyDecimal());
                                        french = french.ReplaceCI("<TotalFundExpenseAmount>", documentRow.GetExactColumnStringValue("TotalFundExpenseAmount").ToCurrencyDecimal("en-CA"));
                                        //<TotalSwitchFundExpenseAmount> 
                                        english = english.ReplaceCI("<TotalSwitchFundExpenseAmount>", documentRow.GetExactColumnStringValue("TotalSwitchFundExpenseAmount").ToCurrencyDecimal());
                                        french = french.ReplaceCI("<TotalSwitchFundExpenseAmount>", documentRow.GetExactColumnStringValue("TotalSwitchFundExpenseAmount").ToCurrencyDecimal("en-CA"));

                                        // add 
                                    }

                                    ProxySeriesSubstitution(documentRow, ref english, ref french);

                                    if (documentFields["MerFeeWaiver"].ToBool())
                                    {
                                        scenario = _gen.searchClientScenarioText(rowIdentity, fieldName + "c");

                                        if (scenario != null)
                                        {
                                            english = english.ReplaceCI("<MerFeeWaiverText>", scenario.Item2);
                                            french = french.ReplaceCI("<MerFeeWaiverText>", scenario.Item3);
                                        }
                                    }
                                    else
                                    {
                                        english = english.ReplaceCI("<MerFeeWaiverText>", "");
                                        french = french.ReplaceCI("<MerFeeWaiverText>", "");
                                    }
                                    CreateEnglishRecord(fieldRow, rowIdentity, english.ReplaceByDictionary(documentFields)); //English Record // added replace by dictionary for us18186
                                    CreateFrenchRecord(fieldRow, rowIdentity, french.ReplaceByDictionary(documentFields)); //French Record
                                }
                            }
                            break;

                        case "FF37": //Prepare FF37 English & French Records
                        case "E37": //Prepare E37 English & French Records

                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null)
                            {
                                StringBuilder thirtyseven_en = new StringBuilder(scenario.Item2);
                                StringBuilder thirtyseven_fr = new StringBuilder(scenario.Item3);
                                string[] returnText;

                                if (int.TryParse(documentFields["FFDocAgeStatusID"], out FFDocAgeID) && int.TryParse(documentFields["AgeCalendarYears"], out ageCalendar))
                                {
                                    if (FFDocAgeID == 4 && ageCalendar > 0)
                                    {
                                        if (documentFields["IsProforma"].ToBool())
                                        {
                                            //<BestReturnPercent>
                                            thirtyseven_en = thirtyseven_en.Replace("<BestReturnPercent>", BulletPercent[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnPercent>", BulletPercent[1]);

                                            //<WorstReturnPercent>
                                            thirtyseven_en = thirtyseven_en.Replace("<WorstReturnPercent>", BulletPercent[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnPercent>", BulletPercent[1]);


                                            //<BestReturnDate>
                                            thirtyseven_en = thirtyseven_en.Replace("<BestReturnDate>", BulletNumber[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnDate>", BulletNumber[1]);

                                            //<WorstReturnDate>
                                            thirtyseven_en = thirtyseven_en.Replace("<WorstReturnDate>", BulletNumber[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnDate>", BulletNumber[1]);


                                            //<BestReturnAmount>
                                            thirtyseven_en = thirtyseven_en.Replace("<BestReturnAmount>", BulletCurrency[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnAmount>", BulletCurrency[1]);

                                            //<WorstReturnAmount>
                                            thirtyseven_en = thirtyseven_en.Replace("<WorstReturnAmount>", BulletCurrency[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnAmount>", BulletCurrency[1]);


                                            //<BestReturnSentence>
                                            thirtyseven_en = thirtyseven_en.Replace("<BestReturnSentence>", BestWorstSentenceRise[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnSentence>", BestWorstSentenceRise[1]);

                                            //<WorstReturnSentence>
                                            thirtyseven_en = thirtyseven_en.Replace("<WorstReturnSentence>", BestWorstSentenceDrop[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnSentence>", BestWorstSentenceDrop[1]);
                                        }
                                        else
                                        {
                                            int maxScale = Math.Max(documentRow.GetExactColumnStringValue("WorstReturnPercent").GetScale(), documentRow.GetExactColumnStringValue("BestReturnPercent").GetScale());
                                            //BestReturnPercent
                                            thirtyseven_en = thirtyseven_en.Replace("<BestReturnPercent>", documentRow.GetExactColumnStringValue("BestReturnPercent").ToPercent("en-CA", maxScale));
                                            thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnPercent>", documentRow.GetExactColumnStringValue("BestReturnPercent").ToPercent("en-CA", maxScale));

                                            //BestReturnDate
                                            string[] month = new string[2];
                                            month = _gen.longFormDate(documentRow.GetExactColumnStringValue("BestReturnDate"), LongMonthNames);

                                            thirtyseven_en = thirtyseven_en.Replace("<BestReturnDate>", month[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnDate>", month[1]);

                                            //BestReturn[...]
                                            if (int.TryParse(documentRow.GetExactColumnStringValue("BestReturnAmount"), out int returnAmount))
                                            {
                                                returnText = BestWorstSentenceSame;
                                                if (returnAmount > 1000)
                                                    returnText = BestWorstSentenceRise;
                                                else if (returnAmount < 1000)
                                                    returnText = BestWorstSentenceDrop;

                                                //string[] returnText = generic.searchGlobalScenarioText(bestReturnScenario);

                                                thirtyseven_en = thirtyseven_en.Replace("<BestReturnSentence>", returnText[0]);
                                                thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnSentence>", returnText[1]);

                                                thirtyseven_en = thirtyseven_en.Replace("<BestReturnAmount>", documentRow.GetExactColumnStringValue("BestReturnAmount").ToCurrency());
                                                thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnAmount>", documentRow.GetExactColumnStringValue("BestReturnAmount").ToCurrency("fr-CA"));
                                            }
                                            else
                                            {
                                                thirtyseven_en = thirtyseven_en.Replace("<BestReturnSentence>", "TryParse failure on " + returnAmount);
                                                thirtyseven_fr = thirtyseven_fr.Replace("<BestReturnSentence>", "TryParse failure on " + returnAmount);
                                            }

                                            //WorstReturnPersent
                                            thirtyseven_en = thirtyseven_en.Replace("<WorstReturnPercent>", documentRow.GetExactColumnStringValue("WorstReturnPercent").ToPercent("en-CA", maxScale));
                                            thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnPercent>", documentRow.GetExactColumnStringValue("WorstReturnPercent").ToPercent("en-CA", maxScale));

                                            //WorstReturnDate
                                            month = _gen.longFormDate(documentRow.GetExactColumnStringValue("WorstReturnDate"), LongMonthNames);

                                            thirtyseven_en = thirtyseven_en.Replace("<WorstReturnDate>", month[0]);
                                            thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnDate>", month[1]);

                                            //WorstReturn[...]
                                            returnAmount = -1;
                                            if (int.TryParse(documentRow.GetExactColumnStringValue("WorstReturnAmount"), out returnAmount))
                                            {
                                                returnText = BestWorstSentenceSame;
                                                if (returnAmount > 1000)
                                                    returnText = BestWorstSentenceRise;
                                                else if (returnAmount < 1000)
                                                    returnText = BestWorstSentenceDrop;

                                                thirtyseven_en = thirtyseven_en.Replace("<WorstReturnSentence>", returnText[0]);
                                                thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnSentence>", returnText[1]);

                                                thirtyseven_en = thirtyseven_en.Replace("<WorstReturnAmount>", documentRow.GetExactColumnStringValue("WorstReturnAmount").ToCurrency());
                                                thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnAmount>", documentRow.GetExactColumnStringValue("WorstReturnAmount").ToCurrency("fr-CA"));
                                            }
                                            else
                                            {
                                                thirtyseven_en = thirtyseven_en.Replace("<WorstReturnSentence>", "TryParse failure on " + returnAmount);
                                                thirtyseven_fr = thirtyseven_fr.Replace("<WorstReturnSentence>", "TryParse failure on " + returnAmount);
                                            }
                                        }
                                        CreateEnglishRecord(fieldRow, rowIdentity, thirtyseven_en.ToString()); //English Record
                                        CreateFrenchRecord(fieldRow, rowIdentity, thirtyseven_fr.ToString()); //French Record
                                    }
                                }
                            }
                            break;

                        case "FF38":
                        case "E38": //Prepare E38 English & French Records - Currently handled in the Extract? - will be <p></p> and never null
                            if (!documentRow.GetPartialColumnStringValue(fieldName + "_EN").IsNaOrBlank())
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, documentRow.GetPartialColumnStringValue(fieldName + "_EN")); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, documentRow.GetPartialColumnStringValue(fieldName + "_FR")); // French Record
                            }
                            break;

                        case "FF39": //Prepare FF39 English & French Records 
                        case "E39": //Prepare E39 English & French Records
                            {
                                int filingYear = documentFields["Last_Filing_Date"].ToDate(DateTime.MinValue).Year;

                                if (filingYear != DateTime.MinValue.Year)
                                {
                                    //Updated to single scenario search
                                    scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                                    if (scenario != null)
                                    {
                                        CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceCI("<FilingDateYear>", filingYear.ToString())); //English Record
                                        CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceCI("<FilingDateYear>", filingYear.ToString())); //French Record
                                    }
                                }
                            }
                            break;


                        #region Mutual Funds

                        case "FF41a": //Prepare FF41a English and French field
                        case "FF41b": //Prepare FF41b English and French field
                            english = string.Empty;
                            french = string.Empty;

                            //Extract scenarios
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null && scenario.Item1 != null)
                            {
                                english = scenario.Item2;
                                french = scenario.Item3;

                                if (documentFields["IsProforma"].ToBool())
                                {
                                    english = english.ReplaceCI("<SwitchFeeDiffPercent>", BulletPercent[0]);
                                    french = french.ReplaceCI("<SwitchFeeDiffPercent>", BulletPercent[1]);
                                }
                                english = english.ReplaceCI("<SwitchFeeDiffPercent>", documentRow.GetExactColumnStringValue("SwitchFeeDiffPercent").ToPercent()).ReplaceByDictionary(documentFields);
                                french = french.ReplaceCI("<SwitchFeeDiffPercent>", documentRow.GetExactColumnStringValue("SwitchFeeDiffPercent").ToPercent("fr-CA")).ReplaceByDictionary(documentFields); //US8488 - no French formatting UPDATED 20211213 = Now with French Formatting

                                ProxySeriesSubstitution(documentRow, ref english, ref french);
                            }
                            CreateEnglishRecord(fieldRow, rowIdentity, english);  //English Record
                            CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record
                            break;

                        #endregion

                        #region ETF Funds

                        case "E3b": //Prepare E3b and E44
                        case "E44":
                            ticker = documentRow.GetExactColumnStringValue("TickerSymbol");
                            if (!ticker.IsNaOrBlank())
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, ticker); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, ticker); //French Record
                            }
                            break;

                        case "E20f": // Prepare E20f field
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);
                            if (scenario != null)
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3); //French Record
                            }
                            break;

                        case "E47": // Prepare E47, E48, E49, and E50 field
                        case "E48":
                        case "E49":
                        case "E50":

                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null && !scenario.Item2.IsNaOrBlank())
                            {
                                Dictionary<string, string> replaceFieldsEN = new Dictionary<string, string>(4);
                                Dictionary<string, string> replaceFieldsFR = new Dictionary<string, string>(4);
                                bool isProforma = documentFields["IsProforma"].ToBool();

                                switch (fieldName)
                                {
                                    case "E47":
                                        replaceFieldsEN.Add("<AverageDailyVolume>", isProforma ? BulletNumber[0] : documentRow.GetExactColumnStringValue("AverageDailyVolume").ToDecimal()); //20210901 - Wajeb requested number formatting on ADV values
                                        replaceFieldsFR.Add("<AverageDailyVolume>", isProforma ? BulletNumber[1] : documentRow.GetExactColumnStringValue("AverageDailyVolume").ToDecimal("fr-CA"));
                                        break;
                                    case "E48":
                                        replaceFieldsEN.Add("<NumberDaysTraded>", isProforma ? BulletNumber[0] : documentRow.GetExactColumnStringValue("NumberDaysTraded"));
                                        replaceFieldsFR.Add("<NumberDaysTraded>", isProforma ? BulletNumber[1] : documentRow.GetExactColumnStringValue("NumberDaysTraded"));
                                        replaceFieldsEN.Add("<NumberDaysIssued>", isProforma ? BulletNumber[0] : documentRow.GetExactColumnStringValue("NumberDaysIssued"));
                                        replaceFieldsFR.Add("<NumberDaysIssued>", isProforma ? BulletNumber[1] : documentRow.GetExactColumnStringValue("NumberDaysIssued"));
                                        break;
                                    case "E49":
                                        replaceFieldsEN.Add("<MarketPriceLow>", isProforma ? BulletCurrency[0] : documentRow.GetExactColumnStringValue("MarketPriceLow").ToCurrencyDecimal());
                                        replaceFieldsFR.Add("<MarketPriceLow>", isProforma ? BulletCurrency[1] : documentRow.GetExactColumnStringValue("MarketPriceLow").ToCurrencyDecimal("fr-CA"));
                                        replaceFieldsEN.Add("<MarketPriceHigh>", isProforma ? BulletCurrency[0] : documentRow.GetExactColumnStringValue("MarketPriceHigh").ToCurrencyDecimal());
                                        replaceFieldsFR.Add("<MarketPriceHigh>", isProforma ? BulletCurrency[1] : documentRow.GetExactColumnStringValue("MarketPriceHigh").ToCurrencyDecimal("fr-CA"));
                                        break;
                                    case "E50":
                                        replaceFieldsEN.Add("<NetAssetValueLow>", isProforma ? BulletCurrency[0] : documentRow.GetExactColumnStringValue("NetAssetValueLow").ToCurrencyDecimal());
                                        replaceFieldsFR.Add("<NetAssetValueLow>", isProforma ? BulletCurrency[1] : documentRow.GetExactColumnStringValue("NetAssetValueLow").ToCurrencyDecimal("fr-CA"));
                                        replaceFieldsEN.Add("<NetAssetValueHigh>", isProforma ? BulletCurrency[0] : documentRow.GetExactColumnStringValue("NetAssetValueHigh").ToCurrencyDecimal());
                                        replaceFieldsFR.Add("<NetAssetValueHigh>", isProforma ? BulletCurrency[1] : documentRow.GetExactColumnStringValue("NetAssetValueHigh").ToCurrencyDecimal("fr-CA"));
                                        break;
                                }

                                CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceByDictionary(replaceFieldsEN, false)); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceByDictionary(replaceFieldsFR, false)); //French Record
                            }
                            break;

                        case "E51": // Prepare E51

                            Dictionary<string, string> stageFields = new Dictionary<string, string>(1);
                            stageFields.Add("AverageBidAskSpread", documentRow.GetExactColumnStringValue("AverageBidAskSpread"));

                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName, stageFields);

                            if (documentFields["IsProforma"].ToBool())
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceCI("<AverageBidAskSpread>", BulletPercent[0])); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceCI("<AverageBidAskSpread>", BulletPercent[1])); //French Record
                            }
                            else
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceCI("<AverageBidAskSpread>", documentRow.GetExactColumnStringValue("AverageBidAskSpread").ToPercent())); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceCI("<AverageBidAskSpread>", documentRow.GetExactColumnStringValue("AverageBidAskSpread").ToPercent("fr-CA"))); //French Record
                            }
                            break;

                        case "E58": //Prepare E58 and E58h field
                            string cusip = documentRow.GetExactColumnStringValue("Cusip");

                            if (!cusip.IsNaOrBlank())
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, cusip); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, cusip); //French Record
                            }
                            break;
                        case "E58h":

                            if (!documentRow.GetExactColumnStringValue("Cusip").IsNaOrBlank())
                            {
                                scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                                if (scenario != null && !scenario.Item2.IsNaOrBlank())
                                {
                                    CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2); //English Record
                                    CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3); //French Record
                                }
                            }
                            break;
                        #endregion

                        #endregion

                        //****************************************************************************************************************************************************************************

                        #region Profile

                        case "FP4": //Prepare FP4 & EP4 English & French Records
                        case "EP4":
                            CreateEnglishRecord(fieldRow, rowIdentity, dataAsAtDate_EN); //English Record
                            CreateFrenchRecord(fieldRow, rowIdentity, dataAsAtDate_FR); //French Record
                            break;

                        case "FP8":
                        case "EP8":
                            // check for a year of data to see if we need to create the table
                            //inputDate = Generic.GetInceptionDate(documentFields); // documentRow.GetExactColumnStringValue("InceptionDate").IsNaOrBlank() ? documentRow.GetExactColumnStringValue("FirstOfferingDate") : documentRow.GetExactColumnStringValue("InceptionDate");

                            if (dataAsAtDate_Raw.ConsecutiveYears(Generic.GetInceptionDate(documentFields), 1)) // 12 consecutive months as per US 10096
                            {
                                scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);
                                if (scenario != null)
                                {
                                    Dictionary<string, string> replacements = new Dictionary<string, string>(8);
                                    replacements.Add("OneMonth", documentRow.GetExactColumnStringValue("OneMonth"));
                                    replacements.Add("ThreeMonth", documentRow.GetExactColumnStringValue("ThreeMonth"));
                                    replacements.Add("YearToDate", documentRow.GetExactColumnStringValue("YearToDate"));
                                    replacements.Add("OneYear", documentRow.GetExactColumnStringValue("OneYear"));
                                    replacements.Add("ThreeYear", documentRow.GetExactColumnStringValue("ThreeYear"));
                                    replacements.Add("FiveYear", documentRow.GetExactColumnStringValue("FiveYear"));
                                    replacements.Add("TenYear", documentRow.GetExactColumnStringValue("TenYear"));
                                    replacements.Add("SinceInception", documentRow.GetExactColumnStringValue("SinceInception"));

                                    int maxScale = 1; // Log #
                                    foreach (KeyValuePair<string, string> keyValue in replacements) // determine the maximum number of decimal places
                                        maxScale = Math.Max(keyValue.Value.GetScale(), maxScale);

                                    foreach (string key in replacements.Keys.ToList()) // format all numbers to the same number of decimal places
                                    {
                                        if (replacements[key].IsNaOrBlank()) // bug 12726 - show a - for N/A
                                            replacements[key] = "-";
                                        else
                                            replacements[key] = replacements[key].ToDecimal(-1, maxScale);
                                    }

                                    CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceByDictionary(replacements)); //English Record
                                    CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceByDictionary(replacements)); //French Record

                                }
                            }
                            else // blank the field
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, EmptyTable); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, EmptyTable); //French Record
                            }
                            break;

                        case "FP8n":
                        case "EP8n":
                        case "FP9n":
                        case "EP9n":
                        case "FP10n":
                        case "EP10n":
                            // Composition will handle showing the n field or regular field based on the regular fields contents

                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);
                            if (scenario != null)
                            {
                                string[] inceptionDate = _gen.longFormDate(documentFields["InceptionDate"], LongMonthNames);
                                string[] performanceResetDate = _gen.longFormDate(documentFields["PerformanceResetDate"], LongMonthNames);

                                CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceCI("<InceptionDate>", inceptionDate[0]).ReplaceCI("<PerformanceResetDate>", performanceResetDate[0])); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceCI("<InceptionDate>", inceptionDate[1]).ReplaceCI("<PerformanceResetDate>", performanceResetDate[1])); //French Record
                            }
                            break;

                        case "FP9":
                        case "EP9":
                            // check for a year of data to see if we need to create the table
                            //inputDate = documentFields["InceptionDate"]; //Generic.GetInceptionDate(documentFields, documentRow.GetExactColumnStringValue("FirstOfferingDate")); //documentRow.GetExactColumnStringValue("InceptionDate").IsNaOrBlank() ? documentRow.GetExactColumnStringValue("FirstOfferingDate") : documentRow.GetExactColumnStringValue("InceptionDate");

                            if (dataAsAtDate_Raw.AgeInCalendarYears(Generic.GetInceptionDate(documentFields)) >= 1) // 1 calendar year as per US 9909
                            {
                                dataStaging.DefaultView.RowFilter = "Code = '" + rowIdentity.DocumentCode + "' AND Sheet_Name = 'DocumentData' AND Item_Name LIKE '%_" + fieldName + "'";
                                dataStaging.DefaultView.Sort = "Row_Number DESC";
                                dt = dataStaging.DefaultView.ToTable();
                                if (dt.Rows.Count > 0)
                                {
                                    TableList tableBuilder = new TableList(documentRow.GetExactColumnStringValue("FilingDate").FilingYear(), "-"); //documentRow.GetExactColumnStringValue("FilingDate").ToDate(DateTime.MinValue).Year
                                    int valCol = dt.FindDataTableColumn("Value");
                                    foreach (DataRow dr in dt.Rows)
                                        tableBuilder.AddValidation(dr.GetStringValue(valCol));

                                    string tempTable = tableBuilder.GetTablePivotString();
                                    CreateEnglishRecord(fieldRow, rowIdentity, tempTable); //English Record
                                    CreateFrenchRecord(fieldRow, rowIdentity, tempTable); //French Record
                                }
                                dt.Dispose();
                            }
                            else // blank the field
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, EmptyTable); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, EmptyTable); //French Record
                            }
                            break;

                        //case "FP9n":
                        //case "EP9n":
                        //    // check for a year of data to see if we need to create the text instead of the table
                        //    inputDate = documentFields["InceptionDate"]; // Generic.GetInceptionDate(documentFields, documentRow.GetExactColumnStringValue("FirstOfferingDate")); //documentRow.GetExactColumnStringValue("InceptionDate").IsNaOrBlank() ? documentRow.GetExactColumnStringValue("FirstOfferingDate") : documentRow.GetExactColumnStringValue("InceptionDate");

                        //    if (dataAsAtDate_Raw.AgeInCalendarYears(inputDate) < 1)
                        //    {
                        //        scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);
                        //        if (scenario != null)
                        //        {
                        //            string[] inceptionDate = _gen.longFormDate(inputDate, LongMonthNames);
                        //            CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceCI("<InceptionDate>", inceptionDate[0])); //English Record
                        //            CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceCI("<InceptionDate>", inceptionDate[1])); //French Record
                        //        }
                        //    }
                        //    break;



                        case "FP10":
                        case "EP10":
                            if (dataAsAtDate_Raw.ConsecutiveYears(Generic.GetInceptionDate(documentFields), 1)) // if (dataAsAtDate_Raw.AgeInCalendarYears(Generic.GetInceptionDate(documentFields)) >= 1) // not yet clear if calendar year or consecutive months
                            {
                                english = documentRow.GetPartialColumnStringValue(fieldName + PDIFile.FILE_DELIMITER + "EN"); //PDIFile.FILE_DELIMITER + 
                                french = documentRow.GetPartialColumnStringValue(fieldName + PDIFile.FILE_DELIMITER + "FR"); //PDIFile.FILE_DELIMITER + 

                                if (!english.IsNaOrBlank())
                                    CreateEnglishRecord(fieldRow, rowIdentity, english); //English Record
                                else
                                    CreateEnglishRecord(fieldRow, rowIdentity, EmptyTable); //English Record wipe

                                if (!french.IsNaOrBlank())
                                    CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record
                                else
                                    CreateFrenchRecord(fieldRow, rowIdentity, EmptyTable); //French Record wipe
                            }
                            else
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, EmptyTable); //English Record wipe
                                CreateFrenchRecord(fieldRow, rowIdentity, EmptyTable); //French Record wipe
                            }
                            break;

                        case "FP13":
                        case "EP13":
                            english = documentRow.GetPartialColumnStringValue(fieldName + PDIFile.FILE_DELIMITER + "EN"); //PDIFile.FILE_DELIMITER + 
                            french = documentRow.GetPartialColumnStringValue(fieldName + PDIFile.FILE_DELIMITER + "FR"); //PDIFile.FILE_DELIMITER + 

                            if (!english.IsNaOrBlank())
                                CreateEnglishRecord(fieldRow, rowIdentity, english); //English Record

                            if (!french.IsNaOrBlank())
                                CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record

                            break;

                        case "FP14":
                        case "EP14":
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null)
                            {
                                string[] inceptionDate = _gen.shortFormDate(documentFields["InceptionDate"]);
                                string[] performanceResetDate = _gen.shortFormDate(documentFields["PerformanceResetDate"]);

                                CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceCI("<SeriesLetter>", documentFields["SeriesLetter"]).ReplaceCI("<InceptionDate>", inceptionDate[0]).ReplaceCI("<PerformanceResetDate>", performanceResetDate[0]).ReplaceByDictionary(documentFields)); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceCI("<SeriesLetter>", documentFields["SeriesLetter"]).ReplaceCI("<InceptionDate>", inceptionDate[1]).ReplaceCI("<PerformanceResetDate>", performanceResetDate[1]).ReplaceByDictionary(documentFields)); //French Record
                            }
                            break;
                        case "FP14h":
                        case "EP14h":
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);

                            if (scenario != null)
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceByDictionary(documentFields));
                                CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceByDictionary(documentFields));
                            }
                            break;

                        case "FP15":
                        case "FP16":
                        case "EP15":
                        case "EP16":
                            dataStaging.DefaultView.RowFilter = "Code = '" + rowIdentity.DocumentCode + "' AND Sheet_Name = 'NAVPU - MER'";
                            dataStaging.DefaultView.Sort = "Row_Number, Column_Number, Item_Name DESC";
                            dt = dataStaging.DefaultView.ToTable();
                            if (dt.Rows.Count > 0)
                            {
                                // Create a pivot table based on the row numbers
                                Pivot pivot = new Pivot(dt);
                                DataTable dtPivot = pivot.PivotData("Row_Number", "Value", AggregateFunction.First, new string[] { "Item_Name" });
                                //AsposeLoader.ConsolePrintTable(dtPivot);

                                string textStringEN = string.Empty;
                                string textStringFR = string.Empty;

                                foreach (DataRow dr in dtPivot.Rows)
                                {
                                    string series = dr.GetExactColumnStringValue("Series");
                                    string merDate = dr.GetExactColumnStringValue("MerDate");
                                    string merPercent = dr.GetExactColumnStringValue("MerPercent");
                                    string navPU = dr.GetExactColumnStringValue("Navpu");

                                    // lookup the scenario text per row using staging parameters
                                    scenario = _gen.searchClientScenarioText(rowIdentity, fieldName, new Dictionary<string, string>(1) { { "MerDate", merDate }, { "MerPercent", merPercent } });

                                    if (scenario != null)
                                    {
                                        textStringEN += scenario.Item2.ReplaceCI("<SeriesLetter>", series).ReplaceCI("<Navpu>", navPU.ToCurrencyDecimal()).ReplaceCI("<MerPercent>", merPercent.ToPercent()).ReplaceByDictionary(documentFields);
                                        textStringFR += scenario.Item3.ReplaceCI("<SeriesLetter>", series).ReplaceCI("<Navpu>", navPU.ToCurrencyDecimal("fr-CA")).ReplaceCI("<MerPercent>", merPercent.ToPercent("fr-CA")).ReplaceByDictionary(documentFields);
                                    }
                                }


                                textStringEN = textStringEN.Trim();
                                textStringFR = textStringFR.Trim();

                                // if the text string ends with a line break - remove it
                                if (textStringEN.EndsWith("<br />") || textStringEN.EndsWith("<br/>") || textStringEN.EndsWith("<br>"))
                                    textStringEN = textStringEN.Substring(0, textStringEN.LastIndexOf("<"));
                                if (textStringFR.EndsWith("<br />") || textStringFR.EndsWith("<br/>") || textStringFR.EndsWith("<br>"))
                                    textStringFR = textStringFR.Substring(0, textStringFR.LastIndexOf("<"));

                                if (textStringEN.Length > 0)
                                {
                                    CreateEnglishRecord(fieldRow, rowIdentity, textStringEN); //English Record
                                    CreateFrenchRecord(fieldRow, rowIdentity, textStringFR); //French Record
                                }
                            }
                            break;

                        case "FP16f":
                        case "EP16f":
                            dataStaging.DefaultView.RowFilter = "Code = '" + rowIdentity.DocumentCode + "' AND Sheet_Name = 'NAVPU - MER' AND Row_Number='1' AND Item_Name LIKE '%MerDate%'";
                            dataStaging.DefaultView.Sort = "Row_Number, Column_Number, Item_Name DESC";
                            dt = dataStaging.DefaultView.ToTable();
                            if (dt.Rows.Count == 1)
                            {
                                scenario = _gen.searchClientScenarioText(rowIdentity, fieldName);
                                if (scenario != null)
                                {
                                    if (dt.Rows[0].GetExactColumnStringValue("Value").IsDate())
                                    {
                                        string[] merDate = _gen.shortFormDate(dt.Rows[0].GetExactColumnStringValue("Value")); // bug14481 - using MerDate token as well - kept <inceptionDate> token for legacy and added ReplaceByDictionary
                                        CreateEnglishRecord(fieldRow, rowIdentity, scenario.Item2.ReplaceCI("<InceptionDate>", merDate[0]).ReplaceCI("<MerDate>", merDate[0]).ReplaceByDictionary(documentFields)); //English Record
                                        CreateFrenchRecord(fieldRow, rowIdentity, scenario.Item3.ReplaceCI("<InceptionDate>", merDate[1]).ReplaceCI("<MerDate>", merDate[1]).ReplaceByDictionary(documentFields)); //French Record
                                    }
                                }
                            }

                            break;
                        case "FP20":
                        case "EP20":
                            dataStaging.DefaultView.RowFilter = "Code = '" + rowIdentity.DocumentCode + "' AND Sheet_Name = 'Distributions'";
                            dataStaging.DefaultView.Sort = "Row_Number, Column_Number DESC, Item_Name DESC";
                            dt = dataStaging.DefaultView.ToTable();
                            //LoadDataStaging(rowIdentity.DocumentCode, "Distributions"); // for this table the Row_Number is the series number and the column_number is the row number except for the header row
                            if (dt.Rows.Count > 0)
                            {
                                //TODO: Load N/A Value for Distribution table from database?
                                TableList tableBuilder = new TableList("-"); //This sets the table as a Distribution Type with the N/A value being a dash
                                int itemCol = dt.FindDataTableColumn("Item_Name");
                                int colCol = dt.FindDataTableColumn("Column_Number");
                                int valCol = dt.FindDataTableColumn("Value");
                                int rowCol = dt.FindDataTableColumn("Row_Number");

                                scenario = _gen.searchClientScenarioText(rowIdentity, fieldName); //Header Text
                                if (scenario != null)
                                {
                                    DateTime dataAsAtDate = dataAsAtDate_Raw.ToDate(DateTime.MinValue);
                                    DateTime curDate;
                                    string englishDate, frenchDate;
                                    int curRow, prevRow = -1;

                                    string headerTextEN = scenario.Item2;
                                    string headerTextFR = scenario.Item3;
                                    string repeatHeaderTextEN = "<SeriesLetter>";
                                    string repeatHeaderTextFR = "<SeriesLetter>";

                                    if (scenario.Item2.Contains("|"))   // 20210913 - added a pipe to split the left header from repeating headers for FP
                                    {
                                        string[] parts = scenario.Item2.Split('|');
                                        headerTextEN = parts[0];
                                        repeatHeaderTextEN = parts[1];
                                    }

                                    if (scenario.Item3.Contains("|"))
                                    {
                                        string[] parts = scenario.Item3.Split('|');
                                        headerTextFR = parts[0];
                                        repeatHeaderTextFR = parts[1];
                                    }

                                    foreach (DataRow dr in dt.Rows)
                                    {
                                        curRow = dr.GetIntValue(rowCol);
                                        if (curRow != prevRow) // when the row changes output a new header (series or blank ticker)
                                        {
                                            // need to get the series or tickerSymbol first then output all other rows in order.
                                            DataRow header = dt.Select($"Row_Number={curRow} AND ( Item_Name = 'Series' OR Item_Name = 'TickerSymbol' )").FirstOrDefault();
                                            if (header != null)
                                            {
                                                if (header[itemCol].ToString().IndexOf("Series", StringComparison.OrdinalIgnoreCase) >= 0)
                                                    tableBuilder.AddValidationDistrib(repeatHeaderTextEN.ReplaceCI("<SeriesLetter>", header.GetStringValue(valCol)).ReplaceByDictionary(documentFields), repeatHeaderTextFR.ReplaceCI("<SeriesLetter>", header.GetStringValue(valCol)).ReplaceByDictionary(documentFields), headerTextEN, headerTextFR, "-1"); // using -1 as the header row as 0 is in use - there is currently no sorting before table output so the datatable is assumed to be sorted
                                                else
                                                    tableBuilder.AddValidation("&nbsp;", headerTextEN, headerTextFR, "-1");
                                            }
                                            prevRow = curRow;
                                        }

                                        if (dr[itemCol].ToString().IndexOf("Series", StringComparison.OrdinalIgnoreCase) < 0 && dr[itemCol].ToString().IndexOf("TickerSymbol", StringComparison.OrdinalIgnoreCase) < 0) // output a regular row in the order they are sorted UNLESS it's a series or ticker
                                        {
                                            if (int.TryParse(dr.GetStringValue(colCol), out int parsed))
                                            {
                                                curDate = dataAsAtDate.AddMonths(-1 * parsed);
                                                englishDate = LongMonthNames[curDate.Month.ToString("0#")][0] + " " + curDate.Year.ToString();  // month full name and year in English
                                                frenchDate = LongMonthNames[curDate.Month.ToString("0#")][1] + " " + curDate.Year.ToString();   // month full name and year in French
                                                tableBuilder.AddValidation(dr.GetStringValue(valCol), englishDate, frenchDate, dr.GetStringValue(colCol));
                                            }
                                        }
                                    }
                                    CreateEnglishRecord(fieldRow, rowIdentity, tableBuilder.GetTableString()); //English Record
                                    CreateFrenchRecord(fieldRow, rowIdentity, tableBuilder.GetTableStringFrench()); //French Record
                                }
                            }

                            dt.Dispose();
                            break;

                        case "EP22":
                            string marketLow = documentRow.GetExactColumnStringValue("MarketPriceLow");
                            if (!marketLow.IsNaOrBlank())
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, marketLow.ToCurrencyDecimal()); //English Record
                                CreateFrenchRecord(fieldRow, rowIdentity, marketLow.ToCurrencyDecimal("fr-CA")); //French Record
                            }
                            break;

                        case "FP23":
                        case "EP23":
                            // Number of Investments
                            fundCode = documentRow.GetExactColumnStringValue("FundCode"); // Number of Investments are a fundCode based table - make sure we have a fundCode match
                            if (fundCode != null && fundCode != string.Empty)
                            {
                                dataStaging.DefaultView.RowFilter = $"Code = '{fundCode}' AND Sheet_Name = 'Number of Investments' AND Item_Name LIKE '{fieldName}_%'";
                                dataStaging.DefaultView.Sort = "Row_Number, Column_Number, Item_Name";
                                DataTable dt = dataStaging.DefaultView.ToTable();

                                english = null;
                                french = null;
                                foreach (DataRow dr in dt.Rows)
                                {
                                    if (dr.GetExactColumnStringValue("Item_Name").IndexOf("_FR", StringComparison.OrdinalIgnoreCase) >= 0)
                                        french = dr.GetExactColumnStringValue("Value"); // CreateFrenchRecord(fieldRow, rowIdentity, );
                                    else
                                        english = dr.GetExactColumnStringValue("Value"); // CreateEnglishRecord(fieldRow, rowIdentity, );
                                }

                                if (NumberOfInvestments23 is null || NumberOfInvestments23.Rows.Count == 0)
                                {
                                    // load the Publisher Number of investments values
                                    NumberOfInvestments23 = LoadNumberOfInvestments();
                                }

                                DataRow[] dRows = NumberOfInvestments23.Select($"DOCUMENT_NUMBER = '{rowIdentity.DocumentCode.EscapeSQL()}'");
                                if (dRows.Length == 1 && english != null && french != null) // only 1 match per document
                                {
                                    english = english.ReplaceFirstCellMatches(dRows[0].GetExactColumnStringValue("english"));
                                    french = french.ReplaceFirstCellMatches(dRows[0].GetExactColumnStringValue("french"));
                                }

                                if (english != null && french != null)
                                {
                                    CreateEnglishRecord(fieldRow, rowIdentity, english);
                                    CreateFrenchRecord(fieldRow, rowIdentity, french);
                                }
                                //else
                                //{
                                //    Console.WriteLine($"{rowIdentity.DocumentCode} had no {fieldName}");
                                //}
                                dt.Dispose();
                            }
                            break;

                        case "FP24":
                        case "EP24":
                            scenario = _gen.searchClientScenarioText(rowIdentity, fieldName); //get the appropriate table configuration - need to add the template identifier
                            if (scenario != null)
                            {
                                // we have a valid scenario so grab the replacement columns from the staging table
                                english = scenario.Item2;
                                french = scenario.Item3;

                                string temp = documentRow.GetExactColumnStringValue("AverageTerm");
                                bool avgTermInDays = documentRow.GetPartialColumnBoolValue("AverageTermInDays");
                                //<AverageTerm> Can be in years or days - calculate the missing value 

                                english = english.ReplaceCI("<AverageTermDays>", temp.ToDays(avgTermInDays));
                                french = french.ReplaceCI("<AverageTermDays>", temp.ToDays(avgTermInDays, "fr-CA"));

                                english = english.ReplaceCI("<AverageTermYears>", temp.ToYears(avgTermInDays));
                                french = french.ReplaceCI("<AverageTermYears>", temp.ToYears(avgTermInDays, "fr-CA"));

                                //<CurrentYield> 
                                temp = documentRow.GetExactColumnStringValue("CurrentYield").ToPercent();
                                english = english.ReplaceCI("<CurrentYield>", temp);
                                french = french.ReplaceCI("<CurrentYield>", temp);

                                //<PortfolioYield> 
                                temp = documentRow.GetExactColumnStringValue("PortfolioYield").ToPercent();
                                english = english.ReplaceCI("<PortfolioYield>", temp);
                                french = french.ReplaceCI("<PortfolioYield>", temp);

                                //<AverageCoupon> 
                                temp = documentRow.GetExactColumnStringValue("AverageCoupon").ToPercent();
                                english = english.ReplaceCI("<AverageCoupon>", temp);
                                french = french.ReplaceCI("<AverageCoupon>", temp);

                                //<ModifiedDuration>
                                temp = documentRow.GetPartialColumnStringValue("ModifiedDuration");
                                english = english.ReplaceCI("<ModifiedDurationYears>", temp.ToDecimal());
                                french = french.ReplaceCI("<ModifiedDurationYears>", temp.ToDecimal("fr-CA"));

                                //<AverageCreditQuality>
                                temp = documentRow.GetExactColumnStringValue("AverageCreditQuality").NaOrBlankNull();
                                english = english.ReplaceCI("<AverageCreditQuality>", temp);
                                french = french.ReplaceCI("<AverageCreditQuality>", temp);

                                CreateEnglishRecord(fieldRow, rowIdentity, english);
                                CreateFrenchRecord(fieldRow, rowIdentity, french);

                            }
                            else
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, EmptyTable);
                                CreateFrenchRecord(fieldRow, rowIdentity, EmptyTable);
                            }

                            break;

                        case "FP30":
                        case "FP30h":
                        case "FP31":
                        case "FP31h":
                        case "FP32":
                        case "FP32h":
                        case "FP33":
                        case "FP33h":
                        case "FP34":
                        case "FP34h":
                        case "FP35":
                        case "FP35h":
                        case "FP36":
                        case "FP36h":
                        case "FP37":
                        case "FP37h":
                        case "FP38":
                        case "FP38h":
                        case "EP30":
                        case "EP30h":
                        case "EP31":
                        case "EP31h":
                        case "EP32":
                        case "EP32h":
                        case "EP33":
                        case "EP33h":
                        case "EP34":
                        case "EP34h":
                        case "EP35":
                        case "EP35h":
                        case "EP36":
                        case "EP36h":
                        case "EP37":
                        case "EP37h":
                        case "EP38":
                        case "EP38h":
                            //Regex.Match(fieldName, @"[EF]P3[0-8]h*", RegexOptions.IgnoreCase).Captures.Count > 0
                            // Allocation FP30-FP38h EP30-EP38h
                            fundCode = documentRow.GetExactColumnStringValue("FundCode"); // Allocations are a fundCode based table - make sure we have a fundCode match
                            if (fundCode != null && fundCode != string.Empty)
                            {
                                dataStaging.DefaultView.RowFilter = $"Code = '{fundCode}' AND Sheet_Name = 'Allocation Tables' AND Item_Name LIKE '{fieldName}_%'";
                                dataStaging.DefaultView.Sort = "Row_Number, Column_Number, Item_Name";
                                DataTable dt = dataStaging.DefaultView.ToTable();

                                foreach (DataRow dr in dt.Rows)
                                {
                                    if (dr.GetExactColumnStringValue("Item_Name").IndexOf("_FR", StringComparison.OrdinalIgnoreCase) >= 0)
                                        CreateFrenchRecord(fieldRow, rowIdentity, dr.GetExactColumnStringValue("Value"));
                                    else
                                        CreateEnglishRecord(fieldRow, rowIdentity, dr.GetExactColumnStringValue("Value"));
                                }
                                dt.Dispose();
                            }
                            break;


                        #endregion


                        #region MRFP
                        case "M15": //Prepare M15 English & French Records - this special case creates 3x fields each time the first column value changes

                            english = documentRow.GetExactColumnStringValue(fieldName + "_EN");

                            if (!english.IsNaOrBlank())
                            {
                                if (_mergeTables.GetMergeFieldNamePrefix.Any(fieldName.Contains) && !fieldRow.Field<bool>("isTextField")) // check if the current field needs to be merged
                                {
                                    english = _mergeTables.MergeTableData(english, fieldName, rowIdentity, _pubDbCon); // merge the field
                                    // TODO: enable a culture code on the merge so the historic French could be loaded and merged if the French was available - in this case we will just use the translation library
                                    if (english != null && english.Length > 0)
                                    {
                                        CreateEnglishRecord(fieldRow, rowIdentity, english);
                                        CreateFrenchRecord(fieldRow, rowIdentity, _gen.GenerateFrench(english, rowIdentity, _jobID, fieldName));
                                    }
                                }
                                else // continue with normal table assembly
                                {
                                    french = documentRow.GetExactColumnStringValue(fieldName + "_FR");
                                    DataTable dtEn = english.XMLtoDataTable();
                                    DataTable dtFr = french.XMLtoDataTable();

                                    char letterCode = 'a'; // f and h are skipped
                                    string curSeries = "||NotASeries||";
                                    string lastTableField = string.Empty;
                                    // Every time the first column value changes to something other than N/A we output the header and footer for the current sequence value
                                    // Do the same for the French table at the same time as we assume the English and French tables to be identical other than text

                                    DataTable chartEn = new DataTable("Current English Chart");
                                    for (int col = 1; col < dtEn.Columns.Count - 1; col++) // add all the columns between the first and the last to the temp table
                                        chartEn.Columns.Add(dtEn.Columns[col].ColumnName, dtEn.Columns[col].DataType);
                                    DataTable chartFr = chartEn.Clone();
                                    DataRow tempRow = fieldTable.NewRow();

                                    tempRow["Field_Name"] = fieldName + letterCode + 'h';
                                    tempRow["isTextField"] = false;
                                    tempRow["isTableField"] = false;
                                    tempRow["isChartField"] = false;

                                    for (int row = 0; row < dtEn.Rows.Count; row++)
                                    {
                                        if (!dtEn.Rows[row][0].ToString().IsNaOrBlank() && dtEn.Rows[row][0].ToString() != curSeries)
                                        {
                                            tempRow["Field_Name"] = fieldName + letterCode + 'h';
                                            tempRow["isTextField"] = true;
                                            tempRow["isChartField"] = false;
                                            CreateEnglishRecord(tempRow, rowIdentity, dtEn.Rows[row][0].ToString()); //English Record
                                            CreateFrenchRecord(tempRow, rowIdentity, dtFr.Rows[row][0].ToString()); //French Record

                                            curSeries = dtEn.Rows[row][0].ToString();

                                            if (!dtEn.Rows[row][dtEn.Columns.Count - 1].ToString().IsNaOrBlank()) // check if we need to create a footer
                                            {
                                                tempRow["Field_Name"] = fieldName + letterCode + 'f';
                                                CreateEnglishRecord(tempRow, rowIdentity, dtEn.Rows[row][dtEn.Columns.Count - 1].ToString()); //English Record
                                                CreateFrenchRecord(tempRow, rowIdentity, dtFr.Rows[row][dtFr.Columns.Count - 1].ToString()); //French Record
                                            }
                                            if (lastTableField.Length > 0 && chartEn.Rows.Count > 0)
                                            {
                                                tempRow["Field_Name"] = lastTableField;
                                                tempRow["isTextField"] = false;
                                                tempRow["isChartField"] = true;

                                                CreateEnglishRecord(tempRow, rowIdentity, chartEn.DataTabletoXML()); //English Chart Record
                                                CreateFrenchRecord(tempRow, rowIdentity, chartEn.DataTabletoXML()); //French Record

                                            }
                                            lastTableField = fieldName + letterCode;
                                            chartEn.Clear();
                                            chartFr.Clear();
                                            letterCode++;
                                            if (letterCode == 'f' || letterCode == 'h') // skip f and h letter codes
                                                letterCode++;


                                        }

                                        // add the current row between header and footer to the temp chart
                                        DataRow drEn = chartEn.NewRow();
                                        DataRow drFr = chartFr.NewRow();
                                        for (int col = 1; col < dtEn.Columns.Count - 1; col++)
                                        {
                                            drEn[dtEn.Columns[col].ColumnName] = dtEn.Rows[row][col];
                                            drFr[dtFr.Columns[col].ColumnName] = dtFr.Rows[row][col];
                                        }
                                        chartEn.Rows.Add(drEn);
                                        chartFr.Rows.Add(drFr);
                                    }
                                    if (lastTableField.Length > 0 && chartEn.Rows.Count > 0) // add the last chart extracted from the table
                                    {
                                        tempRow["Field_Name"] = lastTableField;
                                        tempRow["isTextField"] = false;
                                        tempRow["isChartField"] = true;

                                        CreateEnglishRecord(tempRow, rowIdentity, chartEn.DataTabletoXML()); //English Chart Record
                                        CreateFrenchRecord(tempRow, rowIdentity, chartEn.DataTabletoXML()); //French Record

                                    }
                                }
                            }

                            break;

                        case "M17": //Prepare M17 English & French Records - this special case creates 2x fields each time the first column value changes
                            english = documentRow.GetExactColumnStringValue(fieldName + "_EN");

                            if (!english.IsNaOrBlank())
                            {
                                french = documentRow.GetExactColumnStringValue(fieldName + "_FR");
                                DataTable dtEn = english.XMLtoDataTable();
                                DataTable dtFr = french.XMLtoDataTable();

                                char letterCode = 'a'; // f and h are skipped
                                string curSeries = "||NotASeries||";
                                string lastTableField = string.Empty;
                                // Every time the first column value changes to something other than N/A we output  footer for the current sequence value
                                // Do the same for the French table at the same time as we assume the English and French tables to be identical other than text

                                DataTable tableEn = new DataTable("Current English Chart");
                                for (int col = 1; col < dtEn.Columns.Count - 1; col++) // add all the columns between the first and the last to the temp table
                                    tableEn.Columns.Add(dtEn.Columns[col].ColumnName, dtEn.Columns[col].DataType);
                                DataTable tableFr = tableEn.Clone();
                                DataRow tempRow = fieldTable.NewRow();

                                tempRow["Field_Name"] = fieldName + letterCode + 'h';
                                tempRow["isTextField"] = false;
                                tempRow["isTableField"] = false;
                                tempRow["isChartField"] = false;

                                for (int row = 0; row < dtEn.Rows.Count; row++)
                                {
                                    if (!dtEn.Rows[row][0].ToString().IsNaOrBlank() && dtEn.Rows[row][0].ToString() != curSeries)
                                    {
                                        curSeries = dtEn.Rows[row][0].ToString();

                                        if (!dtEn.Rows[row][dtEn.Columns.Count - 1].ToString().IsNaOrBlank()) // check if we need to create a footer
                                        {
                                            tempRow["Field_Name"] = fieldName + letterCode + 'f';
                                            tempRow["isTextField"] = true;
                                            tempRow["isTableField"] = false;
                                            CreateEnglishRecord(tempRow, rowIdentity, dtEn.Rows[row][dtEn.Columns.Count - 1].ToString()); //English Record
                                            CreateFrenchRecord(tempRow, rowIdentity, dtFr.Rows[row][dtFr.Columns.Count - 1].ToString()); //French Record
                                        }
                                        if (lastTableField.Length > 0 && tableEn.Rows.Count > 0)
                                        {
                                            tempRow["Field_Name"] = lastTableField;
                                            tempRow["isTextField"] = false;
                                            tempRow["isTableField"] = true;

                                            CreateEnglishRecord(tempRow, rowIdentity, tableEn.DataTabletoXML()); //English Chart Record
                                            CreateFrenchRecord(tempRow, rowIdentity, tableEn.DataTabletoXML()); //French Record

                                        }
                                        lastTableField = fieldName + letterCode;
                                        tableEn.Clear();
                                        tableFr.Clear();
                                        letterCode++;
                                        if (letterCode == 'f' || letterCode == 'h') // skip f and h letter codes
                                            letterCode++;
                                    }

                                    // add the current row between header and footer to the temp chart
                                    DataRow drEn = tableEn.NewRow();
                                    DataRow drFr = tableFr.NewRow();

                                    if (tableEn.Rows.Count == 0) // insert the header row after a change or the as the first
                                    {
                                        drEn[0] = dtEn.Rows[row][0];
                                        drFr[0] = dtFr.Rows[row][0];
                                        for (int col = 2; col < dtEn.Columns.Count - 1; col++)
                                        {
                                            drEn[dtEn.Columns[col].ColumnName] = string.Empty;
                                            drFr[dtFr.Columns[col].ColumnName] = string.Empty;
                                        }
                                        tableEn.Rows.Add(drEn);
                                        tableFr.Rows.Add(drFr);
                                        drEn = tableEn.NewRow();
                                        drFr = tableFr.NewRow();
                                    }

                                    for (int col = 1; col < dtEn.Columns.Count - 1; col++)
                                    {
                                        drEn[dtEn.Columns[col].ColumnName] = dtEn.Rows[row][col];
                                        drFr[dtFr.Columns[col].ColumnName] = dtFr.Rows[row][col];
                                    }
                                    tableEn.Rows.Add(drEn);
                                    tableFr.Rows.Add(drFr);
                                }
                                if (lastTableField.Length > 0 && tableEn.Rows.Count > 0) // add the last chart extracted from the table
                                {
                                    tempRow["Field_Name"] = lastTableField;
                                    tempRow["isTextField"] = false;
                                    tempRow["isChartField"] = true;

                                    CreateEnglishRecord(tempRow, rowIdentity, tableEn.DataTabletoXML()); //English Chart Record
                                    CreateFrenchRecord(tempRow, rowIdentity, tableEn.DataTabletoXML()); //French Record

                                }

                            }
                            break;
                        #endregion

                        default:
                            //Logger.AddError(_log, $"Field name {fieldName} Has no transform.");
                            if (_fileName.GetDocumentType == DocumentTypeID.FS || _fileName.GetDocumentType == DocumentTypeID.MRFP || _fileName.GetDocumentType == DocumentTypeID.SFS || _fileName.GetDocumentType == DocumentTypeID.SFSBOOK || _fileName.GetDocumentType == DocumentTypeID.QPDBOOK)
                            {
                                english = documentRow.GetPartialColumnStringValue(fieldName + "_EN");

                                if (!english.IsNaOrBlank())
                                {
                                    if (_mergeTables.GetMergeFieldNamePrefix.Any(fieldName.Contains) && !fieldRow.Field<bool>("isTextField")) // check if the current field needs to be merged
                                    {
                                        english = _mergeTables.MergeTableData(english, fieldName, rowIdentity, _pubDbCon); // merge the field
                                                                                                                           // TODO: enable a culture code on the merge so the historic French could be loaded and merged if the French was available - in this case we will just use the translation library
                                        if (english != null && english.Length > 0)
                                        {
                                            CreateEnglishRecord(fieldRow, rowIdentity, english);
                                            CreateFrenchRecord(fieldRow, rowIdentity, _gen.GenerateFrench(english, rowIdentity, _jobID, fieldName));
                                        }
                                    }
                                    else
                                    {

                                        french = documentRow.GetPartialColumnStringValue(fieldName + "_FR");
                                        scenario = _gen.searchClientScenarioText(rowIdentity, fieldName, documentRow.GetDataRowDictionary(documentFields));
                                        if (scenario != null) // check if there is a header row to append
                                        {
                                            DataRowInsert dri = DataRowInsert.FirstRow;
                                            if (fieldName.Contains("SC120")) //SC120 Subsidiary Transaction Details - row is inserted after every description - which has only the first column populated 
                                                dri = DataRowInsert.AfterDescRepeat;
                                            else if (fieldName.Contains("SC91") || fieldName.Contains("SC93") || fieldName.Contains("SC94") || fieldName.Contains("SC95") || fieldName.Contains("SC123") || fieldName.Contains("SC147") || fieldName.Contains("SC121"))
                                                dri = DataRowInsert.AfterColumnChange;
                                            else if (fieldName.Contains("F41"))
                                                dri = DataRowInsert.ClearExtraColumns;

                                            english = english.InsertHeaderRow(scenario.Item2, dri);
                                            if (scenario.Item3.IsNaOrBlank())
                                                french = french.InsertHeaderRow(_gen.GenerateFrench(scenario.Item2, rowIdentity, _jobID, fieldName), dri);
                                            else
                                                french = french.InsertHeaderRow(scenario.Item3, dri);

                                        }
                                        english = english.ReplaceByDictionary(documentFields).ReplaceByDataRow(documentRow, _gen);
                                        french = french.ReplaceByDictionary(documentFields).ReplaceByDataRow(documentRow, _gen, true);

                                        if (english.CleanXML().IsValidXML())
                                            CreateEnglishRecord(fieldRow, rowIdentity, english); //English Record
                                        else
                                            Logger.AddError(_log, $"Field {fieldName} not valid xml for {rowIdentity.DocumentCode}");
                                        if (french.CleanXML().IsValidXML())
                                            CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record
                                    }
                                }
                                else //if (fieldRow.GetPartialColumnBoolValue("IsTextField"))
                                {
                                    scenario = _gen.searchClientScenarioText(rowIdentity, fieldName, rowFields);
                                    if (scenario != null && !scenario.Item2.StartsWith("<row"))
                                    {
                                        english = scenario.Item2;
                                        if (scenario.Item3.IsNaOrBlank())
                                            french = _gen.GenerateFrench(english, rowIdentity, _jobID, fieldName);
                                        else
                                            french = scenario.Item3;

                                        english = english.ReplaceByDictionary(documentFields).ReplaceByDataRow(documentRow, _gen);
                                        french = french.ReplaceByDictionary(documentFields).ReplaceByDataRow(documentRow, _gen, true);

                                        if (fieldRow.GetExactColumnBoolValue("IsTextField") || english.IsValidXML(false))
                                            CreateEnglishRecord(fieldRow, rowIdentity, english); //English Record
                                        else
                                            Logger.AddError(_log, $"Field {fieldName} not valid xml for {rowIdentity.DocumentCode}");
                                        if (fieldRow.GetExactColumnBoolValue("IsTextField") || french.IsValidXML(false))
                                            CreateFrenchRecord(fieldRow, rowIdentity, french); //French Record
                                    }
                                }


                            }
                            break;
                    }
                }

                if (_fileName.GetDocumentType == DocumentTypeID.SFSBOOK || _fileName.GetDocumentType == DocumentTypeID.FSBOOK || _fileName.GetDocumentType == DocumentTypeID.QPDBOOK) // This handles the Aggregation for Book types
                {
                    DataRow tempRow = fieldTable.NewRow();

                    tempRow["Field_Name"] = "none";
                    tempRow["isTextField"] = false;
                    tempRow["isTableField"] = true;
                    tempRow["isChartField"] = false;
                    LoadBookAggregates(tempRow, rowIdentity, dataStaging, code);
                }
            }

            if (!_dbCon.BulkCopy("dbo.pdi_Transformed_Data", _transTable))
            {
                Logger.AddError(_log, $"Transform Failed for Job_ID: {_jobID} - Error: {_dbCon.LastError}");
                return false;
            }

            return true;
        }

        private void ProxySeriesSubstitution(DataRow documentRow, ref string english, ref string french)
        {
            //<ProxySeries> 
            english = english.ReplaceCI("<ProxySeries>", documentRow.GetExactColumnStringValue("ProxySeries"));
            french = french.ReplaceCI("<ProxySeries>", documentRow.GetExactColumnStringValue("ProxySeries"));

            //<ProxyStartDate> 
            string year = documentRow.GetExactColumnStringValue("ProxyStartDate").ToDate(DateTime.MinValue).Year.ToString();
            english = english.ReplaceCI("<ProxyStartDate>", year);
            french = french.ReplaceCI("<ProxyStartDate>", year);

            //<ProxyEndDate> 
            year = documentRow.GetExactColumnStringValue("ProxyEndDate").ToDate(DateTime.MinValue).Year.ToString();
            english = english.ReplaceCI("<ProxyEndDate>", year);
            french = french.ReplaceCI("<ProxyEndDate>", year);
        }

        private void LoadBookAggregates(DataRow fieldRow, RowIdentity rowIdentity, DataTable dataStaging, string code)
        {
            DataRow dr = dataStaging.Select("Code = '" + code.EscapeSQL() + "' AND Sheet_Name = 'BookFundMap'").SingleOrDefault();
            if (dr != null)
            {
                DataTable DocsInFund = dr.GetExactColumnStringValue("Value").XMLtoDataTable();
                dr = dataStaging.Select("Code = 'All' AND Sheet_Name = 'FundtoBookTableMap'").SingleOrDefault();
                if (dr != null)
                {
                    DataTable fundToBook = dr.GetExactColumnStringValue("Value").XMLtoDataTable();


                    string inDynamicSQL = string.Empty;
                    for (int i = 0; i < DocsInFund.Rows.Count; i++)
                        inDynamicSQL += $"'{DocsInFund.Rows[i][0].ToString().EscapeSQL()}', ";
                    if (inDynamicSQL.Length > 2)
                        inDynamicSQL = inDynamicSQL.Substring(0, inDynamicSQL.Length - 2);



                    foreach (DataRow curMerge in fundToBook.Rows)
                    {
                        fieldRow["Field_Name"] = curMerge.Field<string>(0); // the column names have been lost since the table was converted to XML in the extract
                        string sourceField = curMerge.Field<string>(1);
                        int headerRows = 1;
                        if (!curMerge.Field<string>(2).IsNaOrBlank() && int.TryParse(curMerge.Field<string>(2), out int tempParse))
                            headerRows = tempParse;

                        string rule = curMerge.Field<string>(3);
                        DateTime timeStamp = curMerge.Field<string>(4).ToDate(System.Data.SqlTypes.SqlDateTime.MinValue.Value);

                        string xmlTableEn = string.Empty;
                        string xmlTableFr = string.Empty;
                        DataTable dtResults = new DataTable("Results");

                        string sql = $"SELECT pTD.Document_Number, CONTENT, Culture_Code, pTD.[Timestamp] FROM pdi_Transformed_Data pTD INNER JOIN (SELECT MAX(Job_ID) AS Job_ID, Document_Number FROM pdi_Transformed_Data WHERE Field_Name = @fieldName AND [Timestamp] >= @timeStamp AND Document_Number IN ({inDynamicSQL}) GROUP BY Document_Number) pTDSub ON pTD.Job_ID = pTDSub.Job_ID AND pTD.Document_Number = pTDSub.Document_Number WHERE pTD.Field_Name = @fieldName; ";

                        if (!_dbCon.LoadDataTable(sql, new Dictionary<string, object>(2) { { "@fieldName", sourceField }, { "@timeStamp", timeStamp } }, dtResults))
                            Logger.AddError(_log, $"Unable to lookup aggregation for {sourceField} with error details: {_dbCon.LastError}");
                        else
                        {
                            Flags ruleList = null;
                            if (rule.Length > 0)
                                ruleList = new Flags(rule);  // make use of the scenario Flags class to load the rules


                            foreach (DataRow curRow in DocsInFund.Rows)
                            {
                                string fundCode = curRow.Field<string>(0);

                                DataRow[] results = dtResults.Select($"Document_Number = '{fundCode.EscapeSQL()}'");
                                if (results.Length == 0 && rule.IndexOf("Required", StringComparison.OrdinalIgnoreCase) >= 0)
                                    Logger.AddError(_log, $"AGEC1: Fund code {fundCode} does not have a {sourceField} entry when aggregating {fieldRow["Field_Name"]} for {code}");
                                foreach (DataRow drResult in results)
                                {
                                    string content = Regex.Replace(drResult.GetExactColumnStringValue("CONTENT"), "<row([^>/]*)?>", "<row$1 sourceDocument=\"" + fundCode + "\" timeStamp=\"" + drResult.GetExactColumnDateValue("Timestamp").ToString("yyyy-MM-dd") + "\">", RegexOptions.IgnoreCase); // add additional attributes to the row to indicate the source document and the timestamp of the document for debugging purposes

                                    if (drResult.GetExactColumnStringValue("Culture_Code").Contains("en"))
                                        xmlTableEn = AppendTable(xmlTableEn, content, headerRows, ruleList, fundCode, fieldRow["Field_Name"].ToString());
                                    else
                                        xmlTableFr = AppendTable(xmlTableFr, content, headerRows, ruleList, fundCode, fieldRow["Field_Name"].ToString());
                                }
                            }

                            if (ruleList != null && ruleList.Find("ReplaceInCell") != null)
                            {
                                foreach (Flag f in ruleList)
                                    if (f.FieldName == "ReplaceInCell" && f.Values.Count % 2 == 0)
                                        for (int i = 0; i < f.Values.Count; i += 2)
                                            xmlTableEn = xmlTableEn.ReplaceCI($"<cell>{f.Values[i]}</cell>", $"<cell>{f.Values[i + 1]}</cell>");
                            }

                            if (xmlTableEn.Length > 0 && xmlTableEn.IsValidXML(false) && xmlTableFr.IsValidXML(false))
                            {
                                CreateEnglishRecord(fieldRow, rowIdentity, xmlTableEn);
                                CreateFrenchRecord(fieldRow, rowIdentity, xmlTableFr);
                            }
                            else if (xmlTableEn.Length > 0)
                                Logger.AddError(_log, $"AGEC2: Invalid XML generated for Aggregate field {sourceField}");
                        }
                    }
                }

            }
        }

        /// <summary>
        /// If the existing table is not empty remove the closing table tag and append the append data without the x row
        /// </summary>
        /// <param name="table"></param>
        /// <param name="append"></param>
        /// <returns></returns>
        public string AppendTable(string table, string append, int removeRows, Flags rules, string fundCode, string fieldName)
        {
            if (table is null || table.Length == 0 && rules is null)
                return append;

            if (append is null || append.Length == 0 || !append.Contains("</row>"))
                return table;

            if (rules is null)
            {
                if (table.Contains("</table>")) // if we are appending to an existing table 
                {
                    table = table.Replace("</table>", string.Empty);
                    table += append.RemoveTableRows(removeRows);
                }
            }
            else // determine the rule and build the table appropriately
            {
                if (rules.DistinctFieldNames().Contains("PosNegSort"))
                { // this is currently the only rule with special handling
                    string checkTable = table.Length > 0 ? table : append;
                    string[] rows = checkTable.ReplaceCI("<table>", string.Empty).ReplaceCI("</table>", string.Empty).Split(new[] { "</row>", "<row />" }, StringSplitOptions.None);

                    if (rules.Find("PosNegSort") != null && rules.Find("PosNegSort").Values != null)
                    {
                        Flag posNeg = rules.Find("PosNegSort");
                        if (posNeg.Values.Contains("Unrealized") && !posNeg.Values.Contains("latente")) //US-21107 - Value Hardcoded - Check story for details
                            posNeg.Values.Add("latente");

                        int checkColumn = -1;
                        if (!int.TryParse(posNeg.Values[0], out checkColumn))
                        {
                            for (int i = removeRows - 1; i >= 0; i--) // check header rows in reverse order
                            {
                                if (rows.Count() >= i) // check that the row exists
                                    checkColumn = rows[i].FindXMLColumnByText(posNeg.Values);

                                if (checkColumn > 0) // once found exit for (FindXMLColumn is 1 based
                                    break;
                            }
                        }

                        if (checkColumn > 0) // 1 based index on column to match Excel input
                        {
                            // new sorting method - grab the header and the positive and negative rows then combine in order adding the <row /> seperator only if required
                            string header = table.ExtractXMLRows(removeRows);
                            if (header.Length <= 0)
                                header = append.ExtractXMLRows(removeRows);

                            string tablePos = table.ExtractXMLRows(checkColumn, removeRows);
                            string tableNeg = table.ExtractXMLRows(checkColumn, removeRows, -1).Replace("<row />", string.Empty); // remove the blank row if it already exists
                            string appendPos = append.ExtractXMLRows(checkColumn, removeRows);
                            string appendNeg = append.ExtractXMLRows(checkColumn, removeRows, -1).Replace("<row />", string.Empty); // remove the blank row if it already exists

                            table = "<table>" + header + tablePos + appendPos;
                            if ((tablePos.Length > 0 || appendPos.Length > 0) && (tableNeg.Length > 0 || appendNeg.Length > 0)) // we have a positive and negative section so add the divider row
                                table += "<row />";

                            table += tableNeg + appendNeg + "</table>";
                            //// we have a column so now we need to find where the last positive row is in the table and insert all the positive rows from append and then insert the remaining rows at the end
                            //int tablePosEnd = table.FindLastPositiveXMLRowIndex(checkColumn, removeRows);
                            ////append = append.RemoveTableRows(removeRows);
                            ////int appendPosEnd = append.FindLastPositiveXMLRowIndex(1, checkColumn, 0); // removeRows is 0 as the header has already been removed

                            //if (tablePosEnd >= 0)
                            //    table = table.Substring(0, tablePosEnd) + append.ExtractXMLRows(checkColumn, removeRows) + table.Substring(tablePosEnd).Replace("</table>", string.Empty) + append.ExtractXMLRows(checkColumn, removeRows, -1) + "</table>";
                            //else if (table.Length == 0)
                            //{
                            //    tablePosEnd = append.FindLastPositiveXMLRowIndex(checkColumn, removeRows);
                            //    if (tablePosEnd >= 0)
                            //        table = append.Substring(0, tablePosEnd) + "<row />" + append.ExtractXMLRows(checkColumn, removeRows, -1) + "</table>";
                            //    else // the existing table is blank and the incoming table does not have a pos/neg split - don't add the blank row and just return the append table
                            //        //Logger.AddError(_log, $"AGEC5: Unable to determine table positive/negative split location of {fieldName} for {fundCode}");
                            //        table = append;
                            //}
                            //else // the table does not have a positive or negative split
                            //{
                            //    tablePosEnd = append.FindLastPositiveXMLRowIndex(checkColumn, removeRows);
                            //    if (tablePosEnd >= 0) // the append table has positive and negatives
                            //    {
                            //        if (table.IsPositiveXMLColumnValueByIndex(checkColumn)) // existing table is positive
                            //            table = table.Replace("</table>", string.Empty) + append.ExtractXMLRows(checkColumn, removeRows) + "<row />" + append.ExtractXMLRows(checkColumn, removeRows, -1) + "</table>";
                            //        else // existing table is negative
                            //            table = append.Replace("</table>", string.Empty) + "<row />" + table.ExtractXMLRows(checkColumn, removeRows, -1) + append.ExtractXMLRows(checkColumn, removeRows, -1) + "</table>";
                            //    }
                            //    else // we can't determine a split point on either table 
                            //    {
                            //        if (table.IsPositiveXMLColumnValueByIndex(checkColumn))
                            //        {
                            //            if (append.IsPositiveXMLColumnValueByIndex(checkColumn)) // both positive only
                            //                table = table.Replace("</table>", string.Empty) + append.ExtractXMLRows(checkColumn, removeRows) + "</table>";
                            //            else // append is negative only
                            //                table = table.Replace("</table>", string.Empty) + "<row />" + append.ExtractXMLRows(checkColumn, removeRows, -1) + "</table>";
                            //        }

                            //        else
                            //        {

                            //        }
                            //    }

                            //        Logger.AddError(_log, $"AGEC4: Unable to determine table positive/negative split location of {fieldName} for {fundCode}");

                            //}

                            if (!table.IsValidXML(false))
                                Logger.AddError(_log, $"AGEC3: Invalid XML Encountered During aggregation of {fieldName} for {fundCode}");
                        }
                        else
                            Logger.AddError(_log, $"AGEC4: Unable to find designated check column ({string.Join(", ", posNeg.Values)}) for PosNegSort of {fieldName} for {fundCode}");
                    }
                }
                else
                {
                    if (table.Contains("</table>")) // if we are appending to an existing table 
                    {
                        table = table.Replace("</table>", string.Empty);
                        table += append.RemoveTableRows(removeRows);
                    }
                    else
                        return append;
                }
            }
            return table;
        }

        public bool TransformSTATIC()
        {
            UpdateTranslationLanguage();
            UpdateContentScenario();

            //SqlDataAdapter da = TransformedData();
            //da.Fill(_transTable);

            _transTable = TransformedData();
            List<string> fieldListEN = new List<string>();
            List<string> fieldListFR = new List<string>();
            List<string> fieldListEXTRA = new List<string>();

            Dictionary<string, string> documentFieldsEN = new Dictionary<string, string>();
            Dictionary<string, string> documentFieldsFR = new Dictionary<string, string>();

            // Since the query is time consuming and the data returned is tiny return all required data for the transforms in one call
            fieldListEN.Clear();
            fieldListEN.Add("SeriesLetter");
            fieldListEN.Add("InceptionDate");
            fieldListEN.Add("SeriesDesignationEN");
            fieldListEN.Add("DisplaySeriesNameEN");
            fieldListEN.Add("FundFamilyNameEN");
            fieldListEN.Add("SwitchToSeries");
            //fieldListEN.Add("RiskRating");

            fieldListFR.Clear();
            fieldListFR.Add("SeriesLetter");
            fieldListFR.Add("InceptionDate");
            fieldListFR.Add("SeriesDesignationFR");
            fieldListFR.Add("DisplaySeriesNameFR");
            fieldListFR.Add("FundFamilyNameFR");
            fieldListFR.Add("SwitchToSeries");
            fieldListFR.Add("SeriesLetterFR");
            //fieldListFR.Add("RiskRating");

            fieldListEXTRA.Clear();
            fieldListEXTRA.Add("DataAsAtDate");
            fieldListEXTRA.Add("FilingDateYear");            
            string sql = "SELECT T.*, PC.Company_ID, DT.Document_Type, DT.Feed_Type_Name, pPQL.Document_Type_ID, pPQL.Client_ID, pPQL.LOB_ID FROM (SELECT DS.Job_ID, DS.Code, Fields.Field_Name, DS.Item_Name AS Full_Name, DS.[Value], isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging] DS INNER JOIN (SELECT SDFU.Job_ID, SDFU.Field_Name, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging_STATIC_Field_Update] SDFU INNER JOIN [pdi_Processing_Queue_Log] PQL ON PQL.Job_ID = SDFU.Job_ID INNER JOIN [pdi_Publisher_Document_Field_Attribute] PDFA ON PQL.Document_Type_ID = PDFA.Document_Type_ID AND SDFU.Field_Name = PDFA.Field_Name WHERE Load_Type IN ('STATIC1', 'STATIC3')) Fields ON DS.Job_ID = Fields.Job_ID AND (DS.Item_Name like '%[_]' + Fields.Field_Name OR DS.Item_Name like '%[_]' + Fields.Field_Name + '[_]%') UNION SELECT DS.Job_ID, DS.Code, Fields.Field_Name, null AS Full_Name, null, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM (SELECT DISTINCT Job_ID, Code FROM[pdi_Data_Staging] WHERE Sheet_Name Like 'Document%') DS INNER JOIN (SELECT SDFU.Job_ID, SDFU.Field_Name, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging_STATIC_Field_Update] SDFU INNER JOIN [pdi_Processing_Queue_Log] PQL ON PQL.Job_ID = SDFU.Job_ID INNER JOIN [pdi_Publisher_Document_Field_Attribute] PDFA ON PQL.Document_Type_ID = PDFA.Document_Type_ID AND SDFU.Field_Name = PDFA.Field_Name WHERE Load_Type = 'STATIC2') Fields ON DS.Job_ID = Fields.Job_ID) T INNER JOIN [pdi_Processing_Queue_Log] pPQL ON T.Job_ID = pPQL.Job_ID INNER JOIN [pdi_Publisher_Client] PC ON pPQL.Client_ID = PC.Client_ID INNER JOIN [pdi_Document_Type] DT ON pPQL.Document_Type_ID = DT.Document_Type_ID WHERE T.JOB_ID = @jobID ORDER BY Code, Field_Name, Full_Name;";


            //This changes is to fix US-22195 issue. Now Fund Facts keeps the old sql validation. Other Document Types use new sql validation 
            //string sql = "";
            //if (_fileName.DocumentType == "FF")
            //{
            //    sql = "SELECT T.*, PC.Company_ID, DT.Document_Type, DT.Feed_Type_Name, pPQL.Document_Type_ID, pPQL.Client_ID, pPQL.LOB_ID FROM (SELECT DS.Job_ID, DS.Code, Fields.Field_Name, DS.Item_Name AS Full_Name, DS.[Value], isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging] DS INNER JOIN (SELECT SDFU.Job_ID, SDFU.Field_Name, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging_STATIC_Field_Update] SDFU INNER JOIN [pdi_Processing_Queue_Log] PQL ON PQL.Job_ID = SDFU.Job_ID INNER JOIN [pdi_Publisher_Document_Field_Attribute] PDFA ON PQL.Document_Type_ID = PDFA.Document_Type_ID AND SDFU.Field_Name = PDFA.Field_Name WHERE Load_Type IN ('STATIC1', 'STATIC3')) Fields ON DS.Job_ID = Fields.Job_ID AND (DS.Item_Name like '%[_]' + Fields.Field_Name OR DS.Item_Name like '%[_]' + Fields.Field_Name + '[_]%') UNION SELECT DS.Job_ID, DS.Code, Fields.Field_Name, null AS Full_Name, null, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM (SELECT DISTINCT Job_ID, Code FROM[pdi_Data_Staging] WHERE Sheet_Name Like 'Document%') DS INNER JOIN (SELECT SDFU.Job_ID, SDFU.Field_Name, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging_STATIC_Field_Update] SDFU INNER JOIN [pdi_Processing_Queue_Log] PQL ON PQL.Job_ID = SDFU.Job_ID INNER JOIN [pdi_Publisher_Document_Field_Attribute] PDFA ON PQL.Document_Type_ID = PDFA.Document_Type_ID AND SDFU.Field_Name = PDFA.Field_Name WHERE Load_Type = 'STATIC2') Fields ON DS.Job_ID = Fields.Job_ID) T INNER JOIN [pdi_Processing_Queue_Log] pPQL ON T.Job_ID = pPQL.Job_ID INNER JOIN [pdi_Publisher_Client] PC ON pPQL.Client_ID = PC.Client_ID INNER JOIN [pdi_Document_Type] DT ON pPQL.Document_Type_ID = DT.Document_Type_ID WHERE T.JOB_ID = @jobID ORDER BY Code, Field_Name, Full_Name;";
            //}
            //else {
            //    sql = "SELECT T.*, PC.Company_ID, DT.Document_Type, DT.Feed_Type_Name, pPQL.Document_Type_ID, pPQL.Client_ID, pPQL.LOB_ID FROM (SELECT DS.Job_ID, DS.Code, Fields.Field_Name, DS.Item_Name AS Full_Name, DS.[Value], isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging] DS INNER JOIN (SELECT SDFU.Job_ID, SDFU.Field_Name, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging_STATIC_Field_Update] SDFU INNER JOIN [pdi_Processing_Queue_Log] PQL ON PQL.Job_ID = SDFU.Job_ID INNER JOIN [pdi_Publisher_Document_Field_Attribute] PDFA ON PQL.Document_Type_ID = PDFA.Document_Type_ID AND SDFU.Field_Name = PDFA.Field_Name WHERE Load_Type IN ('STATIC1', 'STATIC3')) Fields ON DS.Job_ID = Fields.Job_ID UNION SELECT DS.Job_ID, DS.Code, Fields.Field_Name, null AS Full_Name, null, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM (SELECT DISTINCT Job_ID, Code FROM[pdi_Data_Staging] WHERE Sheet_Name Like 'Document%') DS INNER JOIN (SELECT SDFU.Job_ID, SDFU.Field_Name, isTextField, isTableField, isChartField, Cycle_Type, Load_Type FROM [pdi_Data_Staging_STATIC_Field_Update] SDFU INNER JOIN [pdi_Processing_Queue_Log] PQL ON PQL.Job_ID = SDFU.Job_ID INNER JOIN [pdi_Publisher_Document_Field_Attribute] PDFA ON PQL.Document_Type_ID = PDFA.Document_Type_ID AND SDFU.Field_Name = PDFA.Field_Name WHERE Load_Type IN ('STATIC2','BAU')) Fields ON DS.Job_ID = Fields.Job_ID) T INNER JOIN [pdi_Processing_Queue_Log] pPQL ON T.Job_ID = pPQL.Job_ID INNER JOIN [pdi_Publisher_Client] PC ON pPQL.Client_ID = PC.Client_ID INNER JOIN [pdi_Document_Type] DT ON pPQL.Document_Type_ID = DT.Document_Type_ID WHERE T.JOB_ID = @jobID ORDER BY Code, Field_Name, Full_Name;";
            //}

            DataTable dt = new DataTable("Fields");
            _dbCon.LoadDataTable(sql, new Dictionary<string, object>() { { "@jobID", (int)_jobID } }, dt);

            Dictionary<string, Static3Collection> rowDataCollection = new Dictionary<string, Static3Collection>();
            string fieldName = string.Empty;
            string fullName = string.Empty;
            string loadType = string.Empty;

            string[] fieldData = new string[2];
            RowIdentity prevRowIdentity = null;
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                prevRowIdentity = rowIdentity.Clone();
                rowIdentity.Update(dt.Rows[i].GetExactColumnIntValue("Document_Type_ID"), dt.Rows[i].GetExactColumnIntValue("Client_ID"), dt.Rows[i].GetExactColumnIntValue("LOB_ID"), dt.Rows[i].GetExactColumnStringValue("Code"));
                if (rowIdentity.IsChanged)
                {
                    if (rowDataCollection.Count > 0)
                    {
                        foreach (KeyValuePair<string, Static3Collection> keyValue in rowDataCollection)
                        {
                            fieldData = _gen.PrepareStaticTypeFields(prevRowIdentity, keyValue.Key, documentFieldsEN, documentFieldsFR, keyValue.Value.Collection);
                            if (fieldData != null && fieldData.Length > 0 && fieldData[0].Length > 0)
                            {
                                CreateEnglishRecord(dt.Rows[keyValue.Value.Row], prevRowIdentity, fieldData[0]);
                                CreateFrenchRecord(dt.Rows[keyValue.Value.Row], prevRowIdentity, fieldData[1]);
                            }
                        }
                        rowDataCollection.Clear();
                    }
                    // if there have been changes update the documentFields from the database
                    documentFieldsEN = _gen.getPublisherDocumentFields(rowIdentity, fieldListEN);
                    documentFieldsFR = _gen.getPublisherDocumentFields(rowIdentity, fieldListFR);
                    _gen.AddStaticFields(documentFieldsEN, documentFieldsFR, fieldListEXTRA);
                    rowIdentity.AcceptChanges();
                }
                fieldName = dt.Rows[i].GetExactColumnStringValue("Field_Name");              
                fullName = dt.Rows[i].GetExactColumnStringValue("Full_Name");
                loadType = dt.Rows[i].GetExactColumnStringValue("Load_Type");

                switch (loadType)
                {
                    case "STATIC1":
                        string val = dt.Rows[i].GetExactColumnStringValue("Value");
                        if (!val.IsNaOrBlank())
                        {
                            val = val.Trim('"').Trim('\''); // remove outer quotes - "N/A" becomes N/A after passing IsNaOrBlank
                            if (fullName.IndexOf("_EN", StringComparison.OrdinalIgnoreCase) >= 0)
                                CreateEnglishRecord(dt.Rows[i], rowIdentity, val);
                            else if (fullName.IndexOf("_FR", StringComparison.OrdinalIgnoreCase) >= 0)
                                CreateFrenchRecord(dt.Rows[i], rowIdentity, val);
                        }
                        break;
                    case "STATIC2":
                        fieldData = _gen.PrepareStaticTypeFields(rowIdentity, fieldName, documentFieldsEN, documentFieldsFR, null, _jobID);
                        if (fieldData != null && fieldData.Length > 0 && fieldData[0].Length > 0)
                        {
                            CreateEnglishRecord(dt.Rows[i], rowIdentity, fieldData[0]);
                            CreateFrenchRecord(dt.Rows[i], rowIdentity, fieldData[1]);
                        }
                        break;

                    case "STATIC3":
                        Static3Collection curCollection;
                        if (!rowDataCollection.ContainsKey(fieldName))
                        {
                            curCollection = new Static3Collection();
                            rowDataCollection.Add(fieldName, curCollection);
                        }
                        else
                            curCollection = rowDataCollection[fieldName];

                        curCollection.Collection.Add(fieldName + Generic.TABLEDELIMITER + System.Text.RegularExpressions.Regex.Match(fullName, @"\d+(?=_)").Value, dt.Rows[i].GetExactColumnStringValue("Value"));
                        curCollection.Row = i;
                        rowDataCollection[fieldName] = curCollection;
                        break;

                    default:
                        Logger.AddError(_log, "Unrecognized Load_Type - " + loadType);
                        break;
                }
            }
            foreach (KeyValuePair<string, Static3Collection> keyValue in rowDataCollection)
            {
                fieldData = _gen.PrepareStaticTypeFields(rowIdentity, keyValue.Key, documentFieldsEN, documentFieldsFR, keyValue.Value.Collection);
                if (fieldData != null && fieldData.Length > 0 && fieldData[0].Length > 0)
                {
                    CreateEnglishRecord(dt.Rows[keyValue.Value.Row], rowIdentity, fieldData[0]);
                    CreateFrenchRecord(dt.Rows[keyValue.Value.Row], rowIdentity, fieldData[1]);
                }
            }

            if (!_dbCon.BulkCopy("dbo.pdi_Transformed_Data", _transTable))
            {
                Logger.AddError(_log, $"Transform Failed for Job_ID: {_jobID} - Error: {_dbCon.LastError}");
                return false;
            }
            return true;

        }

        private DataTable LoadFieldAttributes(string loadType = "BAU")
        {
            DataTable dt = new DataTable("FieldAttributes");

            _dbCon.LoadDataTable("SELECT * FROM [pdi_Publisher_Document_Field_Attribute] pPDFA WHERE pPDFA.Load_Type = @loadType AND pPDFA.Document_Type_ID = @docTypeID;", new Dictionary<string, object>(2) {
                 { "@loadType", loadType },
                { "@docTypeID", _fileName.DocumentTypeID }
            }, dt);

            return dt;
        }

        private DataTable LoadDataStaging(string docCode = "", string sheetName = "")
        {
            DataTable dt = new DataTable("DataStaging");
            string sql = "SELECT * FROM [pdi_Data_Staging] WHERE Job_ID = @jobID";
            if (docCode != null && docCode != string.Empty)
                sql += " AND Code = @docCode";
            if (sheetName != null && sheetName != string.Empty)
                sql += " AND Sheet_Name = @sheetName";

            _dbCon.LoadDataTable(sql, new Dictionary<string, object>(3)
            {
                { "@jobID", _fileName.JobID },
                { "@docCode", docCode },
                { "@sheetName", sheetName }
            }, dt);

            return dt;
        }

        private DataTable LoadNumberOfInvestments()
        {
            DataTable dt = new DataTable("NumberOfInvestments");
            _dbCon.LoadDataTable("SELECT DISTINCT [DOCUMENT_NUMBER], [FIELD_NAME], [english], [french] FROM [dbo].[view_pdi_Fund_Profile_Data] WHERE Field_Name LIKE '%P23' AND Client_ID = @clientID AND DOCUMENT_TYPE_ID = @docTypeID;", new Dictionary<string, object>(2)
                {
                    { "@clientID", _fileName.ClientID },
                    { "@docTypeID", _fileName.DocumentTypeID }
                }, dt, false);

            return dt;
        }

        public DataTable TransformedData()
        {
            DataTable dt = new DataTable("Transformed");

            _dbCon.LoadDataTable("SELECT * FROM [dbo].[pdi_Transformed_Data] WHERE Job_ID = @jobID", new Dictionary<string, object>(1)
            {
                { "@jobID", _fileName.JobID },
            }, dt);

            return dt;
        }

        private void UpdateTranslationLanguage()
        {
            DataTable dt = new DataTable("TranslationLanguage");
            _dbCon.LoadDataTable("SELECT DSSTL.*, PQL.Client_ID, PQL.LOB_ID, PQL.Document_Type_ID, CTL.ID AS ExistingID FROM [pdi_Data_Staging_STATIC_Translation_Language] DSSTL INNER JOIN [pdi_Processing_Queue_Log] PQL ON DSSTL.Job_ID = PQL.Job_ID LEFT OUTER JOIN [pdi_Client_Translation_Language] CTL ON PQL.Client_ID = CTL.Client_ID AND PQL.LOB_ID = CTL.LOB_ID AND PQL.Document_Type_ID = CTL.Document_Type_ID AND DSSTL.[en-CA] = CAST(CTL.[en-CA] AS nvarchar(max)) WHERE DSSTL.JOB_ID = @jobID;", new Dictionary<string, object>(1)
            {
                { "@jobID", (int)_fileName.JobID }
            }, dt);

            // TODO: Need to validate the entries in case they go in XML fields - VALID XML!

            string sql = string.Empty;
            foreach (DataRow row in dt.Rows)
            {
                int existingID = row.GetExactColumnIntValue("ExistingID");
                if (existingID >= 0)
                    sql += $"UPDATE [pdi_Client_Translation_Language] SET [fr-CA] = N'{row.Field<string>("fr-CA").EscapeSQL()}', Last_Updated = GETUTCDATE() WHERE ID = {existingID}; ";
                else
                    sql += $"INSERT INTO [pdi_Client_Translation_Language] (Client_ID, LOB_ID, Document_Type_ID, [en-CA], [fr-CA], Last_Updated) VALUES ({row.Field<int>("Client_ID")},{row.Field<int>("LOB_ID")},{row.Field<int>("Document_Type_ID")},'{row.Field<string>("en-CA").EscapeSQL()}', N'{row.Field<string>("fr-CA").EscapeSQL()}', GETUTCDATE()); ";
            }
            dt.Dispose();
            if (sql != string.Empty)
            {
                if (!_dbCon.ExecuteNonQuery(sql, out int rows))
                    Logger.AddError(_log, "Update Translation Language Error: " + _dbCon.LastError);
            }
        }

        private void UpdateContentScenario()
        {
            // https://dev.azure.com/investorpos/ICOM%20DevOps/_workitems/edit/11189/
            if (!_dbCon.ExecuteNonQuery("DELETE FROM [dbo].[pdi_Client_Field_Content_Scenario_Language] WHERE Field_Name IN (SELECT DISTINCT DSSCS.Field_Name FROM [dbo].[pdi_Data_Staging_STATIC_Content_Scenario] DSSCS WHERE DSSCS.JOB_ID = @jobID) AND Document_Type_ID = @docTypeID AND Client_ID = @clientID AND LOB_ID = @lobID; INSERT INTO [dbo].[pdi_Client_Field_Content_Scenario_Language] SELECT PQL.Document_Type_ID, PQL.Client_ID, PQL.LOB_ID, DSSCS.Field_Name, DSSCS.Field_Description, DSSCS.Scenario_ID, DSSCS.Scenario, DSSCS.Scenario_Description, DSSCS.[en-CA], DSSCS.[fr-CA], GETUTCDATE() AS Last_Updated FROM [dbo].[pdi_Data_Staging_STATIC_Content_Scenario] DSSCS INNER JOIN [dbo].[pdi_Processing_Queue_Log] PQL ON DSSCS.Job_ID = PQL.Job_ID WHERE DSSCS.Job_ID = @jobID;", out int rows, new Dictionary<string, object>(4) {
                { "@jobID", (int)_fileName.JobID },
                { "@docTypeID", (int)_fileName.DocumentTypeID },
                { "@clientID", (int)_fileName.ClientID },
                { "@lobID", (int)_fileName.LOBID }
            }))
            {
                Logger.AddError(_log, "Update Scenario_Language Error: " + _dbCon.LastError);
            }
        }

        //Generic Record creation
        public void CreateRecord(DataRow Row, RowIdentity rowIdentity, string value, string cultureCode = "en-CA", string overrideFieldName = null)
        {

            if (Row.Field<bool>("isTableField") || Row.Field<bool>("isChartField"))
            {
                value = Generic.MakeTableString(value);
                value = value.CleanCellContents();
            }

            _transTable.Rows.Add(_fileName.JobID, rowIdentity.ClientID, _fileName.CompanyID, rowIdentity.LOBID, rowIdentity.DocumentTypeID, _fileName.DocumentType, rowIdentity.DocumentCode, (overrideFieldName is null) ? Row.Field<string>("Field_Name") : overrideFieldName, cultureCode, value, Row.Field<bool>("isTextField"), Row.Field<bool>("isTableField"), Row.Field<bool>("isChartField"), DateTime.Now);

            //_transTable.Rows.Add(_fileName.JobID, rowIdentity.DocumentCode, (overrideFieldName is null) ? Row.Field<string>("Field_Name") : overrideFieldName, cultureCode, value, DateTime.Now);
        }

        //English Record Wrapper
        public void CreateEnglishRecord(DataRow Row, RowIdentity rowIdentity, string value, string overrideFieldName = null)
        {
            CreateRecord(Row, rowIdentity, value, "en-CA", overrideFieldName);
        }

        //French Record Wrapper
        public void CreateFrenchRecord(DataRow Row, RowIdentity rowIdentity, string value, string overrideFieldName = null)
        {
            CreateRecord(Row, rowIdentity, value, "fr-CA", overrideFieldName);
        }


    }
}
