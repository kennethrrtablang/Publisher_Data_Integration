using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Publisher_Data_Operations.Extensions;
using Publisher_Data_Operations.Helper;

namespace Publisher_Data_Operations
{

    public class Generic
    {

        //Constants for Missing Text
        public const string MISSING_EN_TEXT = "MISSING ENGLISH: ";
        public const string MISSING_FR_TEXT = "MISSING FRENCH: ";
        public const double DAYSPERYEAR = 365.2425;
        public const string FLAGPRE = "{{";
        public const string FLAGPOST = "}}";
        public const char TABLEDELIMITER = '╣';
        public const string FSMRFP_ROWTYPE_COLUMN = "RowType";
        internal DBConnection dbCon = null;

        public DataTable GlobalTextLanguage = null;
        private DataTable ClientScenario = null;
        private DataTable PublisherDocuments = null;
        public DataTable ClientTranslation = null;
        public DataTable MissingFrench = null;
        public DataTable MissingFrenchDetails = null;
        private DataTable DocumentTemplates = null;

        public Dictionary<string, string> ValidParameters = null;

        public int ClientID = -1;
        public int LobID = -1;
        public int DocTypeID = -1;

        private Logger _log = null;

        //public Generic(object connectionObject)
        //{
        //    if (connectionObject.GetType() == typeof(DBConnection))
        //        dbCon = (DBConnection)connectionObject;
        //    else
        //        dbCon = new DBConnection(connectionObject);
        //}

        public Generic(object connectionObject, Logger log)
        {
            if (connectionObject.GetType() == typeof(DBConnection))
                dbCon = (DBConnection)connectionObject;
            else
                dbCon = new DBConnection(connectionObject);

            _log = log;
        }

        public string[] searchGlobalScenarioText(string scenario)
        {
            string[] output = new string[2];

            if (GlobalTextLanguage is null || GlobalTextLanguage.Rows.Count < 1)
            {
                GlobalTextLanguage = new DataTable("GlobalTextLanguage");
                dbCon.LoadDataTable("Select [Scenario], [en-CA],[fr-CA] from pdi_Global_Text_Language", null, GlobalTextLanguage);
            }
            DataRow dr = null;
            if (GlobalTextLanguage != null && GlobalTextLanguage.Rows.Count > 0)
                dr = GlobalTextLanguage.Select($"Scenario = '{scenario.EscapeSQL()}'").SingleOrDefault();

            if (dr != null)
            {
                output[0] = dr.Field<string>("en-CA");
                output[1] = dr.Field<string>("fr-CA");
            }
            return output;
        }

        //Allow use of RowIdentity class 
        public Tuple<string, string, string> searchClientScenarioText(RowIdentity rowIdentity, string fieldName, Dictionary<string, string> stageFields = null)
        {
            return searchClientScenarioText(rowIdentity.DocumentTypeID, rowIdentity.ClientID, rowIdentity.LOBID, fieldName, rowIdentity.DocumentCode, stageFields);
        }

        /// <summary>
        /// Return the scenario that matches the most flags or default if no other scenario's match - return null if there is no match or default
        /// Accepts an optional dictionary of field information from the staging (or other) table
        /// </summary>
        /// <param name="documentTypeID"></param>
        /// <param name="clientID"></param>
        /// <param name="lobID"></param>
        /// <param name="fieldName"></param>
        /// <param name="documentNumber"></param>
        /// <param name="stageFields"></param>
        /// <returns></returns>
        public Tuple<string, string, string> searchClientScenarioText(int documentTypeID, int clientID, int lobID, string fieldName, string documentNumber, Dictionary<string, string> stageFields = null)
        {
            Scenarios scenarioList = new Scenarios();
            Tuple<string, string, string> scenario = null;

            // instead of querying the DB each time the DB will only be checked once for the current clientID, LOBID and DocumentTypeID then the DataTable will be filtered and used to populate the current scenario.
            if (ClientScenario is null || ClientScenario.Rows.Count < 1 || ClientID != clientID || LobID != lobID || DocTypeID != documentTypeID)
            {
                ClientScenario = new DataTable("ClientScenarioText");
                ClientID = clientID;
                LobID = lobID;
                DocTypeID = documentTypeID;
                dbCon.LoadDataTable("SELECT [Field_Name], [Scenario], [en-CA], [fr-CA], [Last_Updated] FROM [pdi_Client_Field_Content_Scenario_Language] WHERE Document_Type_ID = @documentTypeID AND Client_ID = @clientID AND LOB_ID = @lobID;",
                    new Dictionary<string, object>(3)
                    {
                        { "@documentTypeID",  documentTypeID },
                        { "@clientID",  clientID },
                        { "@lobID",  lobID }
                    }, ClientScenario);
            }

            DataRow[] dRows = ClientScenario.Select($"Field_Name = '{fieldName.EscapeSQL()}'");

            foreach (DataRow dr in dRows)
                scenarioList.Add(new Scenario(dr.Field<string>("Scenario"), dr.Field<string>("en-CA"), dr.Field<string>("fr-CA"), dr.Field<DateTime>("Last_Updated")));

            if (scenarioList.Count > 0) // && (uniqueFieldNames.Count > 0 || (stageFields != null && stageFields.Count > 0))
            {
                scenarioList.RankOrder();
                Dictionary<string, string> docFields = getPublisherDocumentFields(documentTypeID, clientID, lobID, documentNumber, scenarioList.AllDistinctFieldNames(stageFields));

                if (stageFields != null && stageFields.Count > 0)
                    stageFields.ToList().ForEach(x => docFields[x.Key] = x.Value); // add any stageFields to the docFields

                foreach (Scenario sc in scenarioList)
                {
                    if (sc.MatchFields(docFields))
                        return new Tuple<string, string, string>(sc.FlagList.ToString(), sc.EnglishText, sc.FrenchText);
                }
            }
            return scenario;
        }


        public string[] assembleDateText(string rawData, string[] monthNames)
        {
            return assembleDateText(rawData.ToDate(DateTime.MaxValue), monthNames);
        }

        public string[] assembleDateText(DateTime theDate, string[] monthNames)
        {
            string[] output = new string[2];

            if (theDate != DateTime.MaxValue)
            {
                output[0] = $"{monthNames[0]} {theDate.Day}, {theDate.Year}"; //English

                //French
                if (theDate.Day == 1)
                    output[1] = $"1<sup>er</sup> {monthNames[1]} {theDate.Year}";
                else
                    output[1] = $"{theDate.Day} {monthNames[1]} {theDate.Year}";
            }
            else
            {
                output[0] = "Invalid Date Format";
                output[1] = "Format de date non valide";
            }
            return output;
        }

        /// <summary>
        /// Create the longFormat date by parsing the date string - if a dictionary of date translations has already been loaded use that instead of querying the DB
        /// </summary>
        /// <param name="rawDate">The date in string format - expected dd/MM/yyyy</param>
        /// <param name="dateNames">The optional Dictionary of date strings</param>
        /// <returns>A formatted date string</returns>
        public string[] longFormDate(string rawDate, Dictionary<string, string[]> dateNames = null, string prefix = "")
        {
            string[] month = new string[2];
            DateTime formatDate = rawDate.ToDate(DateTime.MaxValue);
            if (!(rawDate is null) && formatDate != DateTime.MaxValue)
            {
                if (dateNames is null)
                    month = searchGlobalScenarioText(prefix + formatDate.Month.ToString().PadLeft(2, '0')); // in case we start supporting single digit months pad the month portion with zeros to match the DB //rawDate.Split('/')[1]
                else
                    month = dateNames[prefix + formatDate.Month.ToString().PadLeft(2, '0')];
            }

            return assembleDateText(rawDate, month);

        }

        /// <summary>
        /// Create the longFormat date by parsing the date string - if a dictionary of date translations has already been loaded use that instead of querying the DB
        /// </summary>
        /// <param name="rawDate">The date in string format - expected dd/MM/yyyy</param>
        /// <param name="dateNames">The optional Dictionary of date strings</param>
        /// <returns>A formatted date string</returns>
        public string[] shortFormDate(string rawDate, Dictionary<string, string[]> dateNames = null)
        {
            return longFormDate(rawDate, dateNames, "SF");
        }

        /// <summary>
        /// The BNY data format specific US source short date format for M17
        /// </summary>
        /// <param name="rawDate"></param>
        /// <param name="prefix"></param>
        /// <param name="dateNames"></param>
        /// <returns></returns>
        public string[] shortFormDateUS(string rawDate, string prefix = "SF", Dictionary<string, string[]> dateNames = null)
        {
            string[] month = new string[2];
            DateTime formatDate = rawDate.ToDateUS(DateTime.MaxValue);
            if (dateNames is null)
                month = searchGlobalScenarioText(prefix + formatDate.Month.ToString().PadLeft(2, '0')); // in case we start supporting single digit months pad the month portion with zeros to match the DB //rawDate.Split('/')[1]
            else
                month = dateNames[prefix + formatDate.Month.ToString().PadLeft(2, '0')];
            
            month[0] += formatDate.ToString("d/yy");
            month[1] += formatDate.ToString("d/yy");
            
            return month;
        }

        /// <summary>
        /// Create a dictionary of English and French short format months by using the generic month loader set for short form
        /// </summary>
        /// <returns>Dictionary with a string key and a 2 item string array of English[0] and French[1]</returns>
        public Dictionary<string, string[]> loadShortMonths()
        {
            return loadMonths(true);
        }

        /// <summary>
        /// Load either the English and French long month names or optionally the short form names - dictionary is based on two digit months
        /// </summary>
        /// <param name="shortForm">True for short form names</param>
        /// <returns>Dictionary of English and French names</returns>
        public Dictionary<string, string[]> loadMonths(bool shortForm = false)
        {
            Dictionary<string, string[]> months = new Dictionary<string, string[]>(12);

            for (int i = 1; i <= 12; i++)
            {
                string code = (shortForm ? "SF" : "") + i.ToString("0#");
                months.Add(code, searchGlobalScenarioText(code)); // this would be more efficient to query all months at the same time but would then need a custom query
            }

            return months;
        }

        /// <summary>
        /// Determine if the prelim date or filing date should be used for FF4 date calculations
        /// </summary>
        /// <param name="documentTypeID">The document type ID</param>
        /// <param name="clientID">The client ID</param>
        /// <param name="lobID">The line of business ID</param>
        /// <param name="documentNumber">The document number/code</param>
        /// <returns>true if the business conditions for using PrelimData are met</returns>
        public bool usePrelimDateFF4(int documentTypeID, int clientID, int lobID, string documentNumber)
        {
            //refactored to use getPublisherDocumentFields instead of separate query
            Dictionary<string, string> publisherDocs = getPublisherDocumentFields(documentTypeID, clientID, lobID, documentNumber, new List<string> { "FFDocAgeStatusID", "IsProforma" });

            if (int.TryParse(publisherDocs["FFDocAgeStatusID"], out int ffDocAgeStatusID))
            {
                if (ffDocAgeStatusID < 2 && publisherDocs["IsProforma"].ToBool())
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Use RowIdentity and non FF4 specific name to retrieve if prelim or not should be used
        /// </summary>
        /// <param name="rowIdentity">The RowIdentity value</param>
        /// <param name="documentNumber">The document number</param>
        /// <returns>Boolean indicating if the prelim date should be used</returns>
        public bool usePrelimDate(RowIdentity rowIdentity)
        {
            return usePrelimDateFF4(rowIdentity.DocumentTypeID, rowIdentity.ClientID, rowIdentity.LOBID, rowIdentity.DocumentCode);
        }

        //Simplify use of RowIdentity
        public string getPublisherDocumentField(RowIdentity rowIdentity, string fieldName)
        {
            return getPublisherDocumentField(rowIdentity.DocumentTypeID, rowIdentity.ClientID, rowIdentity.LOBID, rowIdentity.DocumentCode, fieldName);
        }

        /// <summary>
        /// Return the requested field from the pdi_Publisher_Documents table given the key values
        /// </summary>
        //// <param name="documentTypeID">The document type ID</param>
        /// <param name="clientID">The client ID</param>
        /// <param name="lobID">The line of business ID</param>
        /// <param name="documentNumber">The document number/code</param>
        /// <param name="fieldName">The database field (column) to request</param>
        /// <returns>requested field as a string or string.empty if a matching document is not found or InceptionDate is null</returns>
        public string getPublisherDocumentField(int documentTypeID, int clientID, int lobID, string documentNumber, string fieldName)
        {
            List<string> list = new List<string>() { fieldName };
            return getPublisherDocumentFields(documentTypeID, clientID, lobID, documentNumber, list).FirstOrDefault().Value;
        }

        //Simplify use of RowIdentity
        public Dictionary<string, string> getPublisherDocumentFields(RowIdentity rowIdentity, List<string> fieldNames, bool convertDates = false, bool french = false)
        {
            return getPublisherDocumentFields(rowIdentity.DocumentTypeID, rowIdentity.ClientID, rowIdentity.LOBID, rowIdentity.DocumentCode, fieldNames, convertDates, french);
        }


        /// <summary>
        /// Return the requested fields from the pdi_Publisher_Documents table given the key values - bit fields are returned as "1" or "0" not null.
        /// </summary>
        //// <param name="documentTypeID">The document type ID</param>
        /// <param name="clientID">The client ID</param>
        /// <param name="lobID">The line of business ID</param>
        /// <param name="documentNumber">The document number/code</param>
        /// <param name="fieldNames">A List of field names to query</param>
        /// <returns>requested field as a string or string.empty if a matching document is not found or InceptionDate is null</returns>
        public Dictionary<string, string> getPublisherDocumentFields(int documentTypeID, int clientID, int lobID, string documentNumber, List<string> fieldNames, bool convertDates = false, bool french = false)
        {

            Dictionary<string, string> returnVals = new Dictionary<string, string>(fieldNames.Count + 1);

            string fieldNamesSQL = "[" + string.Join("], [", fieldNames) + "]";

            // instead of querying the DB each time the DB will only be checked once for the current clientID, LOBID and DocumentTypeID then the DataTable will be filtered and used to populate the current scenario.
            if (PublisherDocuments is null || PublisherDocuments.Rows.Count < 1 || ClientID != clientID || LobID != lobID || DocTypeID != documentTypeID)
            {
                PublisherDocuments = new DataTable("PublisherDocuments");
                ClientID = clientID;
                LobID = lobID;
                DocTypeID = documentTypeID;
                dbCon.LoadDataTable("SELECT * FROM pdi_Publisher_Documents WHERE Client_ID = @clientID AND Document_Type_ID = @documentTypeID AND LOB_ID = @lobID;",
                    new Dictionary<string, object>(3)
                    {
                        { "@documentTypeID",  documentTypeID },
                        { "@clientID",  clientID },
                        { "@lobID",  lobID }
                    }, PublisherDocuments);
            }

            DataRow dr = PublisherDocuments.Select($"Document_Number = '{documentNumber.EscapeSQL()}'").SingleOrDefault();

            if (dr != null)
            {
                foreach (string col in fieldNames)
                {
                    string colName = col;
                    if (col.Equals("DocumentCode", StringComparison.OrdinalIgnoreCase))
                        colName = "Document_Number";
                    if (!dr.Table.Columns.Contains(colName))
                    {
                        Logger.AddError(_log, $"Tried to lookup {colName} in Publisher Documents but field doesn't exist");
                        continue;
                    }

                    var type = dr.Table.Columns[colName].DataType.ToString();
                    switch (type)
                    {
                        case "bit":
                        case "System.Boolean":
                            if (dr[colName] != null)
                                returnVals.Add(col, dr[colName].ToString().ToBool() ? "1" : "0");
                            else
                                returnVals.Add(col, "0");
                            break;
                        case "System.String":
                            returnVals.Add(col, dr[colName].ToString());
                            break;
                        default:
                            if (dr[colName] != null)
                                returnVals.Add(col, dr[colName].ToString());
                            else
                                returnVals.Add(col, string.Empty);
                            break;
                    }
                }
            }

            return returnVals;
        }

        /// <summary>
        /// Retrieve French value from pdi_Client_Translation_language
        /// </summary>
        /// <param name="english">English Text</param>
        /// <param name="clientID">Client ID</param>
        /// <param name="LOB">Line of Business</param>
        /// <param name="documentTypeID"> Document Type ID</param>
        /// <returns>French Value</returns>
        public string searchClientFrenchText(string english, RowIdentity rowIdentity) //, int clientID, int lobID, int documentTypeID
        {
            if (ClientTranslation is null || ClientTranslation.Rows.Count < 1 || ClientID != rowIdentity.ClientID || LobID != rowIdentity.LOBID || DocTypeID != rowIdentity.DocumentTypeID)
            {
                ClientTranslation = new DataTable("ClientTranslation"); // table converted to nvarchar(max) so don't need CAST([en-CA] AS NVARCHAR(MAX)) as 
                dbCon.LoadDataTable("Select [en-CA], [fr-CA] From [dbo].[pdi_Client_Translation_language] WHERE Client_ID = @clientID AND LOB_ID = @LOB AND Document_Type_ID = @documentTypeID",
                    new Dictionary<string, object>(3) {
                        { "@clientID", rowIdentity.ClientID },
                        { "@LOB", rowIdentity.LOBID },
                        { "@documentTypeID", rowIdentity.DocumentTypeID }
                    }, ClientTranslation);
                ClientTranslation.CaseSensitive = true; // Make the searches case sensitive

                ClientID = rowIdentity.ClientID;
                LobID = rowIdentity.LOBID;
                DocTypeID = rowIdentity.DocumentTypeID;
            }

            DataRow dr = null;
            if (ClientTranslation != null && ClientTranslation.Rows.Count > 0)
                dr = ClientTranslation.Select($"[en-CA] = '{english.EscapeSQL()}'").FirstOrDefault(); //SingleOrDefault - there shouldn't be multiple results with the same text but if there are just take the first one

            if (dr != null)
                return dr["fr-CA"].ToString();

            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="french"></param>
        /// <param name="english"></param>
        /// <param name="rowIdentity"></param>
        /// <param name="jobID"></param>
        /// <returns></returns>
        public string verifyFrenchTableText(string french, string english, RowIdentity rowIdentity, int jobID, string fieldName)
        {
            if (french.IsNaOrBlank())
            {
                french = searchClientFrenchText(english.Trim(), rowIdentity);
                if (french.Equals(string.Empty) && !english.Equals(string.Empty))
                {
                    AddMissingFrench(english, rowIdentity, jobID, fieldName);
                    return MISSING_FR_TEXT + english;
                }
            }
            return french;
        }

        /// <summary>
        /// Look up French value using English value if the French value passed in is blank - return English text with prepended MISSING text
        /// </summary>
        /// <param name="french">The French text available</param>
        /// <param name="english">The English text available</param>
        /// <param name="clientID">The Client ID for lookup</param>
        /// <param name="LOB">The Line of Business ID for lookup</param>
        /// <param name="documentTypeID">The Document Type ID for lookup</param>
        /// <returns>The French value that was passed in, found in lookup or added to the English as MISSING</returns>
        //public string verifyFrenchTableText(string french, string english, int clientID, int LOB, int documentTypeID, int jobID)
        //{
        //    return verifyFrenchTableText(french, english, new RowIdentity(documentTypeID, clientID, LOB, ""), jobID);
        //    //if (french.IsNaOrBlank())
        //    //{
        //    //    french = searchClientFrenchText(english.Trim(), clientID, LOB, documentTypeID);
        //    //    if (french.Equals(string.Empty) && !english.Equals(string.Empty))
        //    //    {
        //    //        //AddMissingFrench()
        //    //        AddMissingFrench(clientID, LOB, documentTypeID, jobID, english);
        //    //        return MISSING_FR_TEXT + english;
        //    //    }      
        //    //}
        //    //return french;
        //}

        private void LoadMissingFrench(RowIdentity rowIdentity)
        {
            if (MissingFrench is null || MissingFrenchDetails is null || ClientID != rowIdentity.ClientID || LobID != rowIdentity.LOBID || DocTypeID != rowIdentity.DocumentTypeID)
            {
                Dictionary<string, object> parameters = new Dictionary<string, object>(3)
                    {
                        { "@documentTypeID",  rowIdentity.DocumentTypeID },
                        { "@clientID",  rowIdentity.ClientID },
                        { "@lobID",  rowIdentity.LOBID }
                    };

                MissingFrench = new DataTable("MissingFrench");
                MissingFrench.CaseSensitive = true; // Make the DataTable case sensitive to match the client translation case sensitivity
                dbCon.LoadDataTable("SELECT MIN([Missing_ID]) [Missing_ID], [Client_ID], [LOB_ID], [Document_Type_ID], [en-CA] FROM [pdi_Client_Translation_Language_Missing_Log] WHERE Document_Type_ID = @documentTypeID AND Client_ID = @clientID AND LOB_ID = @lobID GROUP BY [Client_ID], [LOB_ID], [Document_Type_ID], [en-CA];", parameters, MissingFrench);
                MissingFrench.Constraints.Add(new UniqueConstraint(new DataColumn[] { MissingFrench.Columns["Client_ID"], MissingFrench.Columns["LOB_ID"], MissingFrench.Columns["Document_Type_ID"], MissingFrench.Columns["en-CA"] })); // prevent adding in the same English value more than once. 20220503 - added missing case sensitivity to table

                MissingFrenchDetails = new DataTable("MissingFrenchDetails");
                dbCon.LoadDataTable("SELECT MLD.[Missing_ID], [Job_ID], [Document_Number], [Field_Name] FROM [pdi_Client_Translation_Language_Missing_Log_Details] MLD INNER JOIN [pdi_Client_Translation_Language_Missing_Log] ML ON ML.[Missing_ID] = MLD.[Missing_ID] WHERE ML.[Client_ID] = @clientID AND ML.[LOB_ID] = @lobID AND ML.[Document_Type_ID] = @documentTypeID", parameters, MissingFrenchDetails);
            }

            ClientID = rowIdentity.ClientID;
            LobID = rowIdentity.LOBID;
            DocTypeID = rowIdentity.DocumentTypeID;
        }

        /// <summary>
        /// Given English text (that may or may not contain a table) find text separated by <cell> or <br/> and lookup the French value
        /// </summary>
        /// <param name="english"></param>
        /// <param name="rowIdentity"></param>
        /// <returns></returns>
        public string GenerateFrench(string english, RowIdentity rowIdentity, int jobID, string fieldName)
        {
            StringBuilder french = new StringBuilder(Math.Max(english.Length, 1));
            Tuple<string, string> replacement;

            // Need to step through the table otherwise when the replacement is "A" it will replace all the A's (possibly with MISSING FRENCH: A - And you end up with MISSING FRENCH: CAMISSING FRENCH: A (From CAD)
            if (english.IndexOf("<cell>", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                int start = 0;
                int cellStart = 0;
                int cellEnd = 0;
                string frenchVal = string.Empty;
                while (start < english.Length)
                {
                    cellStart = english.IndexOf("<cell>", start, StringComparison.OrdinalIgnoreCase);
                    cellEnd = english.IndexOf("</cell>", start, StringComparison.OrdinalIgnoreCase);
                    if (cellStart > 0 && cellEnd > 0)
                    {
                        french.Append(english.Substring(start, cellStart + 6 - start)); // add up to the end of <cell>
                        string inCellText = english.Substring(cellStart + 6, cellEnd - 6 - cellStart);
                        if (inCellText.IsNumeric())
                            french.Append(inCellText);
                        else if (inCellText.Length > 0)
                        {
                            replacement = SearchFrench(rowIdentity, inCellText, jobID, fieldName); //, true
                            french.Append(inCellText.Replace(replacement.Item1, replacement.Item2));
                        }
                        french.Append(english.Substring(cellEnd, 7));
                        start = cellEnd + 7;
                    }
                    else
                    {
                        french.Append(english.Substring(start));
                        start = english.Length;
                    }
                }
            }
            else
            {
                replacement = SearchFrench(rowIdentity, english, jobID, fieldName);
                if (replacement.Item1 != null && replacement.Item1.Length > 0)
                    french.Append(english.Replace(replacement.Item1, replacement.Item2));
                else
                    french.Append(english);
            }
            return french.ToString();
        }


        public void AddMissingFrench(string english, RowIdentity rowIdentity, int jobID, string fieldName)
        {
            LoadMissingFrench(rowIdentity);
            try // catch the duplicates and continue
            {
                DataRow[] res = MissingFrench.Select($"[en-CA] = '{english.EscapeSQL()}'");
                if (res.Length < 1)
                {
                    string uniqueID = Guid.NewGuid().ToString();
                    MissingFrench.Rows.Add(uniqueID, rowIdentity.ClientID, rowIdentity.LOBID, rowIdentity.DocumentTypeID, english);
                    MissingFrenchDetails.Rows.Add(uniqueID, jobID, rowIdentity.DocumentCode, fieldName);
                }
                else if (res.Length == 1)
                    MissingFrenchDetails.Rows.Add(res[0]["Missing_ID"].ToString(), jobID, rowIdentity.DocumentCode, fieldName);
                else
                    Logger.AddError(_log, $"There were {res.Length} rows returned when adding {english} to the Missing French DataTable found when one or less was expected");
            }
            catch (ConstraintException e)
            {
                // this shouldn't happen anymore since we are searching the DataTable for the English value
                Logger.AddError(_log, $"There was a constraint exception adding the value {english} to the Missing French DataTable: {e.Message}");
            }
            catch (Exception ex) // a real error has occurred
            {
                Logger.AddError(_log, $"There was an error adding the value {english} to the Missing French DataTable: {ex.Message}");
            }
        }

        public bool SaveFrench()
        {
            //AsposeLoader.ConsolePrintTable(MissingFrench);
            if (MissingFrench != null && MissingFrench.Rows.Count > 0)
                if (!dbCon.BulkCopy("dbo.pdi_Client_Translation_Language_Missing_Log", MissingFrench, true))
                {
                    Logger.AddError(_log, $"Failed to save Missing Client Language Translation Log: {dbCon.LastError}");
                    return false;
                }

            if (MissingFrenchDetails != null && MissingFrenchDetails.Rows.Count > 0)
                if (!dbCon.BulkCopy("dbo.pdi_Client_Translation_Language_Missing_Log_Details", MissingFrenchDetails, true))
                {
                    Logger.AddError(_log, $"Failed to save Missing Client Translation Language Log Details: {dbCon.LastError}");
                    return false;
                }

            return true;
        }

        public Tuple<string, string> SearchFrench(RowIdentity rowIdentity, string english, int jobID, string fieldName)
        {
            // at this point we could add handling for numbers and dates either alone or in strings - pull out the number/date - look for the matching French - add back the formatted number/date
            // likely could only do this for numbers/dates that are by themselves or at the beginning or end of strings as the placement in the middle of a string could change in French

            english = english.Trim().RemoveBoundingMarkup(); // remove the surrounding markup tags from the comparison (only removes matching tags at start and end).
            if (english.Equals(string.Empty) || english == Transform.EmptyTable || english == Transform.EmptyText) // if the English we are looking for is the blank value return nothing to use the English string
                return new Tuple<string, string>(string.Empty, string.Empty);
            string frenchLookup = string.Empty;
            System.Globalization.CultureInfo enCA = new System.Globalization.CultureInfo("en-CA");
            if (DateTime.TryParseExact(english, new string[] { "MMMM d, yyyy" }, enCA, System.Globalization.DateTimeStyles.None, out DateTime theDate))
            {
                string[] tempDate = longFormDate(theDate.ToString("dd/MM/yyyy"));
                if (tempDate[0] == english)
                    frenchLookup = tempDate[1];
            }
            else if (english.Length == 5 && english.Contains("/") && DateTime.TryParseExact(english, new string[] { "MM/dd" }, enCA, System.Globalization.DateTimeStyles.None, out DateTime theShortDate))
                frenchLookup = english;
            else if (english.Length <= 10 && english.Contains("/") && DateTime.TryParseExact(english, new string[] { "MM/dd/yyyy" }, enCA, System.Globalization.DateTimeStyles.None, out DateTime theAltDate))
                frenchLookup = theAltDate.ToString("dd/MM/yyyy");
            else if ((english.Contains("(") || english.Contains("–")) && (english.Contains("%") || english.Contains("$")))
            {
                Match m = Regex.Match(english, @"^([\w -.]+)\s+[\(–]+\s*([\d-$]{1}[\d.%,]+)\)?"); //([\w -.]+)\s+\(([\d-]{1}[\d.$%]+)\)
                if (m.Success && m.Groups.Count == 3)
                {
                    string t = m.Groups[2].Value.Replace("%", string.Empty).Replace("$", string.Empty).Trim();
                    // convert the number
                    string ft = "ERROR";
                    if (m.Groups[2].Value.Contains("%"))
                        ft = t.ToPercent("fr-CA");
                    else if (m.Groups[2].Value.Contains("$"))
                    {
                        if (m.Groups[2].Value.Contains("."))
                            ft = t.ToCurrencyDecimal("fr-CA");
                        else
                            ft = t.ToCurrency("fr-CA");
                    }
                    // if String ft contains error, check if the entire english string has a corresponding translation available.
                    if (ft.Equals("ERROR"))
                        frenchLookup = verifyFrenchTableText("", english, rowIdentity, jobID, fieldName);

                    // if the return value did not find the translation, check DB for partial string
                    if (frenchLookup == MISSING_FR_TEXT + english || frenchLookup == String.Empty)
                    {
                        frenchLookup = verifyFrenchTableText("", m.Groups[1].Value, rowIdentity, jobID, fieldName);
                        frenchLookup = english.Replace(m.Groups[1].Value, frenchLookup).Replace(m.Groups[2].Value, ft);
                    }
                }
            }

            if (frenchLookup == string.Empty) 
                frenchLookup = verifyFrenchTableText("", english, rowIdentity, jobID, fieldName);

            return new Tuple<string, string>(english, frenchLookup);
        }

        /// <summary>
        /// We need to add some extra calculated fields to the dictionary
        /// </summary>
        /// <param name="tokensEN">Dictionary of English fields and values</param>
        /// <param name="tokensFR">Dictionary of French fields and values</param>
        /// <param name="extras">A list of fields to add</param>
        public void AddStaticFields(Dictionary<string, string> tokensEN, Dictionary<string, string> tokensFR, List<string> extras)
        {
            foreach (string field in extras)
            {
                if (field.IndexOf("DataAsAtDate", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string[] dates = null;
                    if (tokensEN.Keys.Contains("InceptionDate") && tokensEN["InceptionDate"].IsDate())
                    {
                        dates = longFormDate(tokensEN["InceptionDate"]);
                        tokensEN.Add(field, dates[0]);
                        tokensFR.Add(field, dates[1]);
                    }
                }
                else if (field.IndexOf("FilingDateYear", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (tokensEN.Keys.Contains("Last_Filing_Date") && tokensEN["Last_Filing_Date"].IsDate())
                    {
                        string year = tokensEN["Last_Filing_Date"].ToDate(DateTime.MinValue).Year.ToString();
                        tokensEN.Add(field, year);
                        tokensFR.Add(field, year);
                    }
                }
            }
        }

        /// <summary>
        /// Handle STATIC2 and STATIC3 load types - rowData determines if the type is 2 or 3
        /// </summary>
        /// <param name="rowIdentity">The RowIdentity information</param>
        /// <param name="fieldName">The Field_Name being processed</param>
        /// <param name="tokenListEN">List of English tokens to lookup</param>
        /// <param name="tokenListFR">List of French tokens to lookup</param>
        /// <param name="rowData">The optional list of data related to the current row information</param>
        /// <returns>The English and French string data as an array</returns>
        public string[] PrepareStaticTypeFields(RowIdentity rowIdentity, string fieldName, Dictionary<string, string> tokenListEN, Dictionary<string, string> tokenListFR, Dictionary<string, string> rowData = null, int jobID = -1)
        {
            Tuple<string, string, string> scenario = null;

            if (rowData is null) //Static2
            {
                // replace tokens
                scenario = searchClientScenarioText(rowIdentity, fieldName);
                if (scenario != null)
                {
                    string french = scenario.Item3;
                    if (scenario.Item3.IsNaOrBlank())
                        french = verifyFrenchTableText(french, scenario.Item2, rowIdentity, jobID, fieldName); // add looking up French text when N/A or blank for FS and MRFP documents
                    return new string[] { scenario.Item2.ReplaceByDictionary(tokenListEN), french.ReplaceByDictionary(tokenListFR) };
                }


            }
            else
            {
                // build table
                StringBuilder tableEN = new StringBuilder("<table>");
                StringBuilder tableFR = new StringBuilder("<table>");
                Dictionary<string, string> stageFields = new Dictionary<string, string>(1);
                Dictionary<int, string> curList = new Dictionary<int, string>();
                foreach (KeyValuePair<string, string> row in rowData)
                {
                    string[] rowItems = row.Key.Split(TABLEDELIMITER);
                    if (rowItems.Length == 2 && !row.Value.IsNaOrBlank())
                        curList.Add(int.Parse(rowItems[1]), rowItems[0] + TABLEDELIMITER + row.Value);
                }

                var sortList = curList.ToList();
                sortList.Sort((pair1, pair2) => pair1.Key.CompareTo(pair2.Key));

                foreach (KeyValuePair<int, string> row in sortList)
                {
                    string[] rowVals = row.Value.Split(TABLEDELIMITER);

                    if (!stageFields.ContainsKey(rowVals[0]))
                        stageFields.Add(rowVals[0], rowVals[1]);
                    else
                        stageFields[rowVals[0]] = rowVals[1];

                    scenario = searchClientScenarioText(rowIdentity, fieldName, stageFields);
                    if (scenario != null)
                    {
                        tableEN.Append(scenario.Item2.ReplaceByDictionary(tokenListEN).Trim());
                        tableFR.Append(scenario.Item3.ReplaceByDictionary(tokenListFR).Trim());
                    }

                }

                if (tableEN.Length > 10)
                    return new string[] { tableEN.Append("</table>").ToString(), tableFR.Append("</table>").ToString() };
            }
            return null;
        }

        public bool CheckDocumentTemplates(string clientCode, string documentType, out bool active)
        {
            active = true;
            if (DocumentTemplates is null || DocumentTemplates.Rows.Count == 0)
            {
                DocumentTemplates = new DataTable("DocumentTemplates");
                dbCon.LoadDataTable("SELECT PDT.Client_ID, PC.Client_Code, PDT.Document_Type_ID, DT.Document_Type, Document_Temp_Name, IS_Active FROM [dbo].[pdi_Publisher_Document_Templates] PDT INNER JOIN [dbo].[pdi_Publisher_Client] PC on PC.Client_ID = PDT.Client_ID INNER JOIN [dbo].[pdi_Document_Type] DT ON DT.Document_Type_ID = PDT.Document_Type_ID;", null, DocumentTemplates);
            }

            DataRow dr = DocumentTemplates.Select($"Client_Code = '{clientCode.EscapeSQL()}' AND Document_Type = '{documentType.EscapeSQL()}'").SingleOrDefault();

            if (dr != null)
            {
                active = dr.IsNull("IS_Active") ? false : dr.Field<bool>("IS_Active");
                return true;
            }
            return false;

        }

        /// <summary>
        /// Check if fields marked as IsTable have the <table> tag
        /// </summary>
        /// <param name="value">The string value to check</param>
        /// <returns>The original or modified string</returns>
        public static string MakeTableString(string value)
        {
            if (value.IndexOf("<table", StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (value.Length > 0)
                    return $"<table><row><cell>{value}</cell></row></table>";
                else
                    return Transform.EmptyTable;
            }

            else
                return value;
        }

        /// <summary>
        /// Use MakeTableString with data that is already converted to Unicode Byte[] array
        /// </summary>
        /// <param name="value">Unicode Byte[] array</param>
        /// <returns>the Byte[] result of MakeTableString</returns>
        public static Byte[] MakeTableString(Byte[] value)
        {
            return Encoding.Unicode.GetBytes(MakeTableString(Encoding.Unicode.GetString(value)));
        }

        public static string GetInceptionDate(Dictionary<string, string> documentFields, string altDateField = null)
        {
            if (documentFields.ContainsKey("PerformanceReset") && documentFields["PerformanceReset"].ToBool() && documentFields.ContainsKey("PerformanceResetDate") && !documentFields["PerformanceResetDate"].IsNaOrBlank())
                return documentFields["PerformanceResetDate"];
            else if (documentFields.ContainsKey("InceptionDate") && !documentFields["InceptionDate"].IsNaOrBlank())
                return documentFields["InceptionDate"];
            else if (!altDateField.IsNaOrBlank() && altDateField.IsDate())
                return altDateField;
            else if (documentFields.ContainsKey("FilingDate") && !documentFields["FilingDate"].IsNaOrBlank())
                return documentFields["FilingDate"];
            else if (documentFields.ContainsKey("Last_Filing_Date") && !documentFields["Last_Filing_Date"].IsNaOrBlank())
                return documentFields["Last_Filing_Date"];

            return null;
        }


        public Dictionary<string, string> loadValidParameters() //, int clientID, int lobID, int documentTypeID
        {
            if (ValidParameters is null || ValidParameters.Count < 1)
                ValidParameters = ExcelHelper.loadValidParameters(dbCon);

            return ValidParameters;
        }

        public DataTable GetStatusTable(int offSet, int pageSize = 10)
        {
            using (DataTable dt = new DataTable("TableStatus"))
            {
                if (dbCon.LoadDataTable("SELECT FRL.File_Name, ISNULL(PQL.Job_Start, FRL.File_Receipt_Timestamp) AS Start_Time,  FRL.IsValidFileName, FL.IsValidDataFile, FL.Number_of_Records, PQL.Job_Status, DATEDIFF_BIG(second, PQL.Job_Start, COALESCE(PQL.Import_End, PQL.Load_End, PQL.Transform_End, PQL.Extract_End, PQL.Validation_End, PQL.Job_Start)) AS Processing_Seconds, FVL.Validation_Message_Count, FVL.Max_Message, FRL.File_ID, PQL.Job_ID FROM pdi_File_Receipt_Log FRL LEFT OUTER JOIN pdi_File_Log FL ON FRL.File_ID = FL.File_ID LEFT OUTER JOIN pdi_Processing_Queue_Log PQL ON FL.Data_ID = PQL.Data_ID LEFT OUTER JOIN(SELECT FIle_ID, COUNT(*) as Validation_Message_Count, MAX(Validation_Message) as Max_Message FROM pdi_File_Validation_Log GROUP BY FIle_ID) FVL ON FVL.File_ID = FRL.File_ID ORDER BY ISNULL(PQL.Job_Start, FRL.File_Receipt_Timestamp) DESC OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY;", new Dictionary<string, object>(1) { { "@offset", offSet }, { "@pageSize", pageSize } }, dt))
                {
                    return dt;
                }
                else
                    Logger.AddError(_log, $"Error getting Status from database - {dbCon.LastError}");
            }
            return null;

        }

        public DataTable GetValidationMessages(string fileNameOrID)
        {
            DataTable dt = new DataTable("ValidationErrors");

            if (int.TryParse(fileNameOrID, out int fileID))
            {
                if (!dbCon.LoadDataTable("SELECT Validation_Message FROM [dbo].[pdi_File_Validation_Log] WHERE File_ID = @fileID;", new Dictionary<string, object>(1) { { "@fileID", fileNameOrID } }, dt))
                    Logger.AddError(_log, $"Error getting Validation Messages from database - {dbCon.LastError}");
            }
            else
            {
                if (!dbCon.LoadDataTable("SELECT Validation_Message FROM [dbo].[pdi_File_Validation_Log] WHERE File_ID = (SELECT MAX(File_ID) AS File_ID From [pdi_File_Receipt_Log] WHERE File_Name = @fileName)", new Dictionary<string, object>(1) { { "@fileName", fileNameOrID } }, dt))
                    Logger.AddError(_log, $"Error getting Validation Messages from database - {dbCon.LastError}");
            }
            return dt;
        }
    }
}
