using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Publisher_Data_Operations.Extensions;
using Publisher_Data_Operations.Helper;

namespace Publisher_Data_Operations
{
    public class DocumentProcessing
    {

        //Pub_Data_Integration_DEVEntities defaultContext { get; set; }
        private DBConnection _dbCon = null;
        private DataTypeID _dataType;
        private DocumentTypeID _docType;
        private DataTable _publisherDocs = null;
        private Logger _log = null;

        /// <summary>
        /// DBConnection will identify the type of connection string object and contains the required metadata to create the necessary EntityFramework connection string
        /// </summary>
        /// <param name="sqlConnection">Either a connection string or an SQLConnection object</param>
        public DocumentProcessing(object sqlConnection, Logger log)
        {
            if (sqlConnection.GetType() == typeof(DBConnection))
                _dbCon = (DBConnection)sqlConnection;
            else
                _dbCon = new DBConnection(sqlConnection);

            //defaultContext = new Pub_Data_Integration_DEVEntities(_dbCon.GetEntityConnectionString());
            _log = log;
        }

        /// <summary>
        /// Process the staging table based on provided Data Type name and Data File Type name
        /// </summary>
        /// <param name="jobID">The jobID to process</param>
        /// <param name="docType">The name of the Data Type</param>
        /// <param name="dataType">The name of the File Type</param>
        /// <returns></returns>
        public bool processStaging(int jobID, string dataType, string docType)
        {
            if (Enum.TryParse(docType, out DocumentTypeID dt) && Enum.TryParse(dataType, out DataTypeID datat))
            {
                _docType = dt;
                _dataType = datat;
                return processStaging(jobID);
            }
            return false;
        }

        /// <summary>
        /// Process the staging table based on provided Data Type ID and Data File Type ID
        /// </summary>
        /// <param name="jobID">The jobID to process</param>
        /// <param name="docType">The ID of the Data Type</param>
        /// <param name="dataType">The ID of the File Type</param>
        /// <returns></returns>
        public bool processStaging(int jobID, int dataType, int docType)
        {
            if (Enum.IsDefined(typeof(DocumentTypeID), docType) && Enum.IsDefined(typeof(DataTypeID), dataType))
            {
                _docType = (DocumentTypeID)docType;
                _dataType = (DataTypeID)dataType;
                return processStaging(jobID);
            }
            return false;
        }

        /// <summary>
        /// Process the staging table based on provided Data Type  and Data File Type
        /// </summary>
        /// <param name="jobID">The jobID to process</param>
        /// <param name="docType">The ID of the Data Type</param>
        /// <param name="dataType">The ID of the File Type</param>
        /// <returns></returns>
        public bool processStaging(int jobID, DataTypeID dataType, DocumentTypeID docType)
        {
            _docType = docType;
            _dataType = dataType;
            return processStaging(jobID);
        }

        /// <summary>
        /// Process the staging table based on provided filename
        /// </summary>
        /// <param name="jobID">The jobID to process</param>
        /// <param name="fileName">The string filename</param>
        /// <returns></returns>
        public bool processStaging(int jobID, string fileName)
        {
            PDIFile fn = new PDIFile(fileName);
            if (fn.GetDataType != null || fn.GetDocumentType != null)
            {
                _docType = (DocumentTypeID)fn.GetDocumentType;
                _dataType = (DataTypeID)fn.GetDataType;
                if (fn.FileID.HasValue && _log is null)
                    _log = new Logger(_dbCon, fn);
                return processStaging(jobID);
            }
            return false;

        }

        /// <summary>
        /// Process the staging table based on provided FileName object
        /// </summary>
        /// <param name="jobID">The jobID to process</param>
        /// <param name="fn">The FileName object</param>
        /// <returns></returns>
        public bool processStaging(PDIFile fn)
        {
            if (fn.GetDataType != null && fn.GetDocumentType != null && fn.JobID.HasValue)
            {
                _docType = (DocumentTypeID)fn.GetDocumentType;
                _dataType = (DataTypeID)fn.GetDataType;
                if (fn.FileID.HasValue && _log is null)
                    _log = new Logger(_dbCon, fn);
                return processStaging((int)fn.JobID);
            }
            return false;

        }

        /// <summary>
        /// Process the staging table - Data and File type are defined in private properties
        /// </summary>
        /// <param name="jobID">The jobID to process</param>
        /// <returns></returns>
        private bool processStaging(int jobID)
        {
            return processDataStaging(jobID); //, defaultContext
        }


        /// <summary>
        /// Update the Publisher_Document table with pdi_Data_Staging for all data types
        /// </summary>
        /// <param name="jobID">The Job_ID to process</param>
        /// <param name="ctx">The Entity Framework Context</param>
        /// <returns>true</returns>
        internal bool processDataStaging(int jobID) //, Pub_Data_Integration_DEVEntities ctx
        {
            //filter for the provided job number
            DataTable dt = GetStagingPivotTable(jobID, _dbCon);
            //_publisherDocs = GetPublisherDocumentsTable(jobID);

            if (dt is null || dt.Rows.Count == 0)
                if (_dataType != DataTypeID.STATIC)
                    return false;
                else
                    return true;
            // Check that we have results for only a single Client, Doc Type and LOB
            DataView view = new DataView(dt);
            DataTable distinctValues = view.ToTable(true, "Client_ID", "Document_Type_ID", "LOB_ID");
            if (distinctValues.Rows.Count > 1)
                return false; // Error if more than one row is distinct

            // Store the distinct values we are processing
            int clientID = distinctValues.Rows[0].GetExactColumnIntValue("Client_ID");
            int docTypeID = distinctValues.Rows[0].GetExactColumnIntValue("Document_Type_ID");
            int lobID = distinctValues.Rows[0].GetExactColumnIntValue("LOB_ID");
            view.Dispose();
            distinctValues.Dispose();

            // Create a dataset and populate the datatable with any existing records for this client - DataAdapter will be used to write the added or updated records back to the database
            //DataSet ds = new DataSet();
            string sql = "SELECT * FROM pdi_Publisher_Documents WHERE Client_ID = @ClientID;";
            Dictionary<string, object> parameters = new Dictionary<string, object>(1) { { "@ClientID", clientID } };
            _publisherDocs = new DataTable("Documents");
            if (!_dbCon.LoadDataTable(sql, parameters, _publisherDocs))
            {
                Logger.AddError(_log, $"Unable to load Publisher Documents error was: {_dbCon.LastError}");
                return false;
            }
                

            string filingReferenceID = string.Empty;
            foreach (DataRow row in dt.Rows)
            {
                //for each of the matching job_id's records check for an existing document
                //int clientID = row.GetExactColumnIntValue("Client_ID");
                //int docTypeID = row.GetExactColumnIntValue("Document_Type_ID");
                //int lobID = row.GetExactColumnIntValue("LOB_ID");
                string docCode = row.GetExactColumnStringValue("Code");
                string fundCode = row.GetExactColumnStringValue("FundCode");

                if ((docCode) == "All") // don't process aggregation instructions as a document
                    continue;

                if (filingReferenceID is null || filingReferenceID.Length < 1) // only update the filing reference ID once
                {
                    filingReferenceID = row.GetExactColumnStringValue("FilingReferenceID");
                    if (filingReferenceID != null && filingReferenceID.Length > 0)
                        UpdateJobFilingReferenceID(jobID, filingReferenceID);

                }
                DataRow matchedDoc = null;
                try
                {
                    matchedDoc = _publisherDocs.Select($"Client_ID = {clientID} AND Document_Type_ID = {docTypeID} AND LOB_ID = {lobID} AND Document_Number = '{docCode}'").SingleOrDefault(); //ctx.pdi_Publisher_Documents.Where(p => p.Client_ID == clientID && p.Document_Type_ID == docTypeID && p.LOB_ID == lobID && p.Document_Number == docCode).SingleOrDefault<pdi_Publisher_Documents>(); //p.Client_ID == stage.Client_ID && p.Document_Type_ID == stage.Document_Type_ID && && p.SeriesLetter == stage.SeriesLetter 
                }
                catch (Exception err)
                {
                    Logger.AddError(_log, $"Error filtering to find matching document {docCode}: {err.Message}");
                }


                if (matchedDoc is null)
                {
                    //there is no matching document add it as new
                    matchedDoc = _publisherDocs.NewRow(); //pdi_Publisher_Documents - row is not attached yet

                    matchedDoc["Client_ID"] = clientID;
                    matchedDoc["Document_Type_ID"] = docTypeID;
                    matchedDoc["LOB_ID"] = lobID;
                    matchedDoc["Document_Number"] = docCode;
                    matchedDoc["FundCode"] = fundCode ?? docCode;
                    matchedDoc["Date_Created"] = DateTime.Now;

                    _publisherDocs.Rows.Add(matchedDoc); // add the new row to the DataTable
                }
                //https://dev.azure.com/investorpos/ICOM%20DevOps/_workitems/edit/11177 - only update IsActiveStatus (and last updated) if the document is not active and the file type is not STATIC. 
                
                if (_dataType != DataTypeID.STATIC && (_docType == DocumentTypeID.FS || _docType == DocumentTypeID.MRFP) && !row.GetExactColumnBoolValue("IsActiveStatus")) // set active for FS MRFP documents that 
                    UpdateIfChanged(matchedDoc, "IsActiveStatus", true);
                else if (_dataType != DataTypeID.STATIC && !row.GetExactColumnBoolValue("IsActiveStatus") )
                {
                    UpdateIfChanged(matchedDoc, "IsActiveStatus", false); //matchedDoc.IsActiveStatus = false;
                    UpdateIfChanged(matchedDoc, "FFDocAgeStatusID", DBNull.Value); // us13302
                }
                   
                else // Update fields in all other conditions
                {
                    if (_dataType == DataTypeID.BAU && (_docType == DocumentTypeID.FF || _docType == DocumentTypeID.ETF)) // 1 and 2 on the FFDocAgeStatusID logic restrictions us11177
                        UpdateIfChanged(matchedDoc, "FFDocAgeStatusID", SetFFDocAge(row, matchedDoc));
                    

                    List<string> fieldList = new List<string> { "IsPool", "IsUSD", "IsCorporateClass", "FundFamilyNameEN", "FundFamilyNameFR", "Document_FileName_EN", "Document_FileName_FR", "Sort_Order", "SeriesDesignationEN", "SeriesDesignationFR", "DisplaySeriesNameEN", "DisplaySeriesNameFR", "AgeCalendarYears", "NegativeReturnCalendarYears", "IsProforma", "MerFeeWaiver", "FilingReferenceID", "IsUnderlying", "NoSecuritiesIssued", "ProxySeriesLink", "PerformanceReset", "FundCode", "Switching", "SwitchToSeries", "SeriesLetter", "SeriesLetterFR", "TickerSymbol", "Cusip", "UnderlyingIndexNameEN", "UnderlyingIndexNameFR", "PortfolioCharacteristicsTemplate" }; // added UnderlingIndexNameEN and FR for Wajeb - 20011008 // added PortfolioCharacteristicsTemplate for FP24 //us13300 - SeriesLetterFR



                    foreach (string field in fieldList)
                    {
                        if (row.Table.Columns.Contains(field)) // don't try to update fields that don't exist in the source
                        {
                            switch (_publisherDocs.Columns[field].DataType.ToString())
                            {
                                case "System.Int32":
                                    if (int.TryParse(row[field].ToString(), out int resInt))
                                        UpdateIfChanged(matchedDoc, field, resInt); //matchedDoc[field] = resInt;
                                    break;
                                case "System.Boolean":
                                    UpdateIfChanged(matchedDoc, field, row[field].ToString().ToBool()); // matchedDoc[field] = row[field].ToString().ToBool();
                                    break;
                                case "System.DateTime":
                                    if (System.DateTime.TryParse(row[field].ToString(), out System.DateTime resDate))
                                        UpdateIfChanged(matchedDoc, field, resDate); // matchedDoc[field] = resDate;
                                    else if (row[field].ToString().Length > 0)
                                        UpdateIfChanged(matchedDoc, field, row[field].ToString().ToDate(System.DateTime.MinValue)); // matchedDoc[field] = row[field].ToString().ToDate(System.DateTime.MinValue);
                                    break;
                                case "System.String":
                                    UpdateIfChanged(matchedDoc, field, row[field].ToString().NaOrBlankNull()); // matchedDoc[field] = AsNullString(row[field].ToString(), matchedDoc.Field<string>(field));
                                    break;
                                case "Publisher_Data_Operations.Entities.FFDocAge":
                                    if (int.TryParse(row[field].ToString(), out int resFF))
                                        UpdateIfChanged(matchedDoc, field, (FFDocAge)resFF); // matchedDoc[field] = (FFDocAge)resFF;
                                    break;
                                default:

                                    UpdateIfChanged(matchedDoc, field, row[field].ToString());// matchedDoc[field] = row[field].ToString();
                                    break;
                            }
                        }
                    }
                    // last filing date excel and table headers don't match so update separately
                    UpdateIfChanged(matchedDoc, "Last_Filing_Date", row.GetExactColumnStringValue("FilingDate"));

                    // Update other parameters that require conditions or calculations
                    if (row.Table.Columns.Contains("InceptionDate"))
                    {
                        UpdateIfChanged(matchedDoc, "InceptionDate", row.GetExactColumnStringValue("InceptionDate").IsNaOrBlank() ? row.GetExactColumnStringValue("FirstOfferingDate") : row.GetExactColumnStringValue("InceptionDate"));
                        //https://dev.azure.com/investorpos/ICOM%20DevOps/_workitems/edit/8201/
                        UpdateIfChanged(matchedDoc, "Is120ConsecutiveMonths", matchedDoc.Field<string>("InceptionDate").ConsecutiveYears(row.GetExactColumnStringValue("DataAsAtDate"), 10));
                    }
                    
                    if (_dataType == DataTypeID.BAU && (_docType == DocumentTypeID.FF || _docType == DocumentTypeID.ETF))
                        UpdateIfChanged(matchedDoc, "IsMERAvailable", !row.GetExactColumnStringValue("MerDate").IsNaOrBlank() && !row.GetExactColumnStringValue("MerPercent").IsNaOrBlank());
                  
                    if (row.Table.Columns.Contains("IsActiveStatus"))
                        UpdateIfChanged(matchedDoc, "IsActiveStatus", row.GetExactColumnStringValue("IsActiveStatus").ToBool());

                    if (!row.GetExactColumnStringValue("ProxySeriesLink").IsNaOrBlank())
                        UpdateIfChanged(matchedDoc, "ProxyStartEndYearEqual", row.GetExactColumnStringValue("ProxyStartDate").YearEqual(row.GetExactColumnStringValue("ProxyEndDate"))); 

                    if (matchedDoc.Field<bool?>("PerformanceReset").HasValue && matchedDoc.Field<bool>("PerformanceReset") && row.Table.Columns.Contains("PerformanceResetDate"))
                    {
                        int perfResetDate = row.GetExactColumnStringValue("PerformanceResetDate").ToDate(DateTime.MinValue).Year;
                        if ((perfResetDate <= DateTime.Now.Year - 1 && perfResetDate >= DateTime.Now.Year - 10) || _docType == DocumentTypeID.EP || _docType == DocumentTypeID.FP)
                        {
                            UpdateIfChanged(matchedDoc, "PerformanceReset", true); 
                            UpdateIfChanged(matchedDoc, "PerformanceResetDate", row.GetExactColumnStringValue("PerformanceResetDate"));
                            //UpdateIfChanged(matchedDoc, "InceptionDate", row.GetExactColumnStringValue("PerformanceResetDate")); // Replace the inception date with the performance reset date when the performance has been reset.
                            UpdateIfChanged(matchedDoc, "Is120ConsecutiveMonths", matchedDoc.Field<string>("PerformanceResetDate").ConsecutiveYears(row.GetExactColumnStringValue("DataAsAtDate"), 10));
                        }
                        else
                        {
                            UpdateIfChanged(matchedDoc, "PerformanceReset", false); 
                            UpdateIfChanged(matchedDoc, "PerformanceResetDate", null);
                        }
                    }
                }
                if (matchedDoc.Field<string>("FundCode").IsNaOrBlank())
                    UpdateIfChanged(matchedDoc, "FundCode", docCode);

                if (matchedDoc.RowHasChanged())
                    matchedDoc["Last_Updated"] = DateTime.Now;
            }

            if (!_dbCon.UpdateDataTable(sql, parameters, _publisherDocs))
            {
                Logger.AddError(_log, $"Error in Document Processing: {_dbCon.LastError}");
                return false;
            }
            
            return true;
        }

        private void UpdateJobFilingReferenceID(int jobID, string filingReferenceID)
        {
            if (_dbCon != null)
            {
                Processing.UpdateFilingReferenceID(_dbCon, jobID, filingReferenceID, out string errorMessage);
                if (errorMessage.Length > 0)
                    Logger.AddError(_log, errorMessage);
            }
        }

        /// <summary>
        /// Return the row staging data as a pivot datatable - uses dynamic column names
        /// </summary>
        /// <param name="jobID">The Job_ID to query</param>
        /// <returns>The queried datatable</returns>
        public static DataTable GetStagingPivotTable(int jobID, DBConnection dbCon)
        {
            DataTable dt = new DataTable("DataStaging");
            dbCon.LoadDataTable("sp_DataStagingPivot", new Dictionary<string, object>(1) { { "@Job_ID", jobID } }, dt, true);
           
            return dt;

        }

        /// <summary>
        /// Updates the specified column only if the provided value is not equal to the current value 
        /// </summary>
        /// <param name="row">The DataRow</param>
        /// <param name="column">The Column name</param>
        /// <param name="value">The new value</param>
        private void UpdateIfChanged(DataRow row, string column, object value)
        {
            //if (value.GetType().FullName == "System.String" && value.ToString().IsNaOrBlank())
            //    return; // do nothing if the value is a string and is N/A

            if (!row[column].Equals(value))
                row[column] = value;

        }
  

        private FFDocAge? SetFFDocAge(DataRow row, DataRow matchedDoc)
        {
            Dictionary<string, string> staging = new Dictionary<string, string>
            {
                { "InceptionDate", row.GetExactColumnStringValue("InceptionDate").IsNaOrBlank() ? row.GetExactColumnStringValue("FirstOfferingDate") : row.GetExactColumnStringValue("InceptionDate") },
                { "FilingReferenceID", row.GetExactColumnStringValue("FilingReferenceID") },
                { "NoSecuritiesIssued", row.GetExactColumnStringValue("NoSecuritiesIssued") },
                { "DataAsAtDate", row.GetExactColumnStringValue("DataAsAtDate") },
                { "FundCode", row.GetExactColumnStringValue("FundCode") },
                { "Client_ID", row.GetExactColumnStringValue("Client_ID") }
            };
            return SetFFDocAge(staging, matchedDoc, _publisherDocs);
        }

        public FFDocAge? SetFFDocAge(Dictionary<string, string> staging, DataRow matchedRow, DataTable pubDocs)
        {
            if (matchedRow != null && !matchedRow.IsNull("FFDocAgeStatusID")) // 1.
            {
                if (matchedRow.Field<string>("FilingReferenceID") != staging["FilingReferenceID"])   // 1.a.
                {
                    if (staging["InceptionDate"].ConsecutiveYears(staging["DataAsAtDate"], 1)) // 1.a.ii.
                        return FFDocAge.TwelveConsecutiveMonths;  // F013_ISFA
                    else if (matchedRow.Field<FFDocAge>("FFDocAgeStatusID") == FFDocAge.BrandNewFund) // 1.a.iii
                        return FFDocAge.NewFund;  // F013_ISFB
                    else if (matchedRow.Field<FFDocAge>("FFDocAgeStatusID") == FFDocAge.BrandNewSeries) // 1.a.iv
                        return FFDocAge.NewSeries; // F014_ISF8
                }
                return matchedRow.Field<FFDocAge>("FFDocAgeStatusID"); // T099_CISFLA //1.a.i. - and all cases where the status is not 12 consecutive or brand new - return current value
            }
            else // 1.b.
            {
                if (staging["InceptionDate"].ConsecutiveYears(staging["DataAsAtDate"], 1)) // 1.b.ii.
                    return FFDocAge.TwelveConsecutiveMonths; // T097_CISFL

                bool InceptionGreater = staging["InceptionDate"].ToDate(DateTime.MinValue) > staging["DataAsAtDate"].ToDate(DateTime.MaxValue);
                DataRow[] matches = pubDocs.Select($"Client_ID = {staging["Client_ID"]} AND FundCode = '{staging["FundCode"]}' {(matchedRow != null && matchedRow["Document_Number"] != null ? $"AND Document_Number <> '" + matchedRow["Document_Number"] + "'" : string.Empty)}", null, DataViewRowState.OriginalRows); // select filter uses "OriginalRows" in order to query only data that existed before this import - in this way we can ignore a newly added brand new fund causing other fund series to be brand new series
                // if a matched record is found it is excluded from the results as it is not a valid comparison target

                if (matches.Length > 0)
                {
                    if (InceptionGreater)
                    {
                        if (matches.Any(x => x.GetExactColumnFFDocAge("FFDocAgeStatusID") == FFDocAge.BrandNewFund && x.Field<string>("FilingReferenceID") == staging["FilingReferenceID"]))
                            return FFDocAge.BrandNewFund; // T099_CISFLC
                        else
                            return FFDocAge.BrandNewSeries; // F014_ISF6
                    }
                    else
                    {
                        if (matches.Any(x => x.GetExactColumnFFDocAge("FFDocAgeStatusID") == FFDocAge.NewFund && x.Field<string>("FilingReferenceID") == staging["FilingReferenceID"]))
                            return FFDocAge.NewFund; // T098_CISFLC
                        else
                            return FFDocAge.NewSeries; // F014_ISF6
                    }
                }
                return InceptionGreater ? FFDocAge.BrandNewFund : FFDocAge.NewFund; // F015_ISFA - T097_CISFL
            }
        }
    }
}
