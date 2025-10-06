using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Publisher_Data_Operations.Extensions;
using System.Data;

namespace Publisher_Data_Operations.Helper
{
    [Serializable]
    public class MergeTablesSettings
    {
        public Tuple<int, int> CheckField { get; set; } // negative values indicate to use the last available row or column
        public int DesiredRows { get; set; }
        public int DesiredColumns { get; set; }
        public string InterimIndicator { get; set; }
        public int InterimOffset { get; set; }


        public MergeTablesSettings(int checkRow, int checkCol, int desiredRows, int desiredColumns, string interimIndicator, int interimOffset)
        {
            CheckField = new Tuple<int, int>(checkRow, checkCol);
            DesiredRows = desiredRows;
            DesiredColumns = desiredColumns;
            InterimIndicator = interimIndicator;
            InterimOffset = interimOffset;
        }

        public MergeTablesSettings(DataRow dr)
        {
            CheckField = new Tuple<int, int>(dr.GetExactColumnIntValue("Check_Field_Row"), dr.GetExactColumnIntValue("Check_Field_Column"));
            DesiredRows = dr.GetExactColumnIntValue("Desired_Rows");
            DesiredColumns = dr.GetExactColumnIntValue("Desired_Columns");
            InterimIndicator = dr.GetExactColumnStringValue("Interim_Indicator");
            InterimOffset = dr.GetExactColumnIntValue("Interim_Offset");
        }
    }

    public class MergeTables
    {
        public MergeTablesSettings MergeSettings { get; private set; }
        public DataTable HistoricTableData = null;
        public DataTable MergeTableSettings = null;
        private Logger _log = null;
        private string DocumentNumber = string.Empty;
        private string FieldNamePrefix = string.Empty;
        private int ClientID = -1;
        private int DataTypeID = -1;
        private int DocumentTypeID = -1;

        private int CheckRow => (MergeSettings != null && MergeSettings.CheckField != null ? MergeSettings.CheckField.Item1 : int.MaxValue);
        private int CheckColumn => (MergeSettings != null && MergeSettings.CheckField != null ? MergeSettings.CheckField.Item2 : int.MaxValue);

        public MergeTables()
        {

        }

        public MergeTables(int checkRow, int checkCol, int desiredRows, int desiredColumns, string interimIndicator, int interimOffset)
        {
            MergeSettings = new MergeTablesSettings(checkRow, checkCol, desiredRows, desiredColumns, interimIndicator, interimOffset);
        }

        public MergeTables(MergeTablesSettings mergeSettings)
        {
            if (mergeSettings is null)
                throw new ArgumentNullException("Merge Settings is required");
            MergeSettings = mergeSettings;
        }

        public MergeTables(RowIdentity rowIdentity, int dataTypeID, DBConnection conPdi, Logger log = null)
        {

            if (rowIdentity is null)
                throw new ArgumentNullException("rowIdentity");
            if (conPdi is null)
                throw new ArgumentNullException("conObject");
            if (dataTypeID < 0)
                throw new ArgumentNullException("dataTypeID");

            _log = log;

            LoadMergeTableSettings(rowIdentity, dataTypeID, conPdi);

        }

        public List<string> GetMergeFieldNamePrefix => (MergeTableSettings != null && MergeTableSettings.Rows.Count > 0 ? MergeTableSettings.AsEnumerable().Select(x => x[0].ToString()).ToList() : new List<string>());

        public int LoadMergeTableSettings(RowIdentity rowIdentity, int dataTypeID, DBConnection dbPdi)
        {
            if (dbPdi is null)
                throw new ArgumentNullException("dbPdi");
            if (rowIdentity is null)
                throw new ArgumentNullException("rowIdentity");

            // instead of querying the DB each time the DB will only be checked once for the current Client, Data Type and Document Type
            if (MergeTableSettings is null || rowIdentity.ClientID != ClientID || dataTypeID != DataTypeID  || rowIdentity.DocumentTypeID != DocumentTypeID)
            {
                MergeTableSettings = new DataTable("MergeSettings");
                ClientID = rowIdentity.ClientID;
                DataTypeID = dataTypeID;
                DocumentTypeID = rowIdentity.DocumentTypeID;

                if (!dbPdi.LoadDataTable("SELECT [Field_Name_Prefix] ,[Check_Field_Row] ,[Check_Field_Column] ,[Desired_Rows] ,[Desired_Columns], [Interim_Indicator] ,[Interim_Offset] FROM [pdi_Data_Type_Merge_Table] WHERE Client_ID = @clientID AND Data_Type_ID=@dataTypeID AND Document_Type_ID=@docTypeID", new Dictionary<string, object>(3)
                {
                    { "@clientID",  ClientID },
                    { "@dataTypeID", DataTypeID },
                    { "@docTypeID",  DocumentTypeID }
                }, MergeTableSettings))
                    Logger.AddError(_log, $"Error loading Historic Table Merge Settings:  {dbPdi.LastError}");
            }
            

            return (MergeTableSettings != null ? MergeTableSettings.Rows.Count : -1);
        }

        public bool GetMergeSettings(string fieldName, RowIdentity rowIdentity, int dataTypeID, DBConnection dbPdi)
        {
            if (fieldName.IsNaOrBlank())
                throw new ArgumentNullException("Field Name is required");

            
            if (LoadMergeTableSettings(rowIdentity, dataTypeID, dbPdi) >= 0)
            {
                string fieldNamePrefix = fieldName.GetFieldNamePrefix();
                DataRow[] dRows = MergeTableSettings.Select($"Field_Name_Prefix = '{fieldName.EscapeSQL()}'");

                if (dRows.Length == 1)
                {
                    MergeSettings = new MergeTablesSettings(dRows[0]);
                    return true;
                }   
            }
            return false;
        }

        public bool GetMergeSettings(string fieldName)
        {
            if (fieldName.IsNaOrBlank())
                throw new ArgumentNullException("Field Name is required");

            if (MergeTableSettings == null || MergeTableSettings.Rows.Count < 1)
                return false;

            DataRow[] dRows = MergeTableSettings.Select($"Field_Name_Prefix = '{ fieldName.GetFieldNamePrefix().EscapeSQL()}'");

            if (dRows.Length == 1)
            {
                MergeSettings = new MergeTablesSettings(dRows[0]);
                return true;
            }

            return false;
        }

        public int LoadHistoricalData(string fieldName, RowIdentity rowIdentity, DBConnection dbPub)
        {
            if (dbPub is null)
                throw new ArgumentNullException("conObject");
            if (rowIdentity is null)
                throw new ArgumentNullException("rowIdentity");
            if (fieldName.IsNaOrBlank())
                throw new ArgumentNullException("fieldName");

            string fieldNamePrefix = fieldName.GetFieldNamePrefix();
            
            // instead of querying the DB each time the DB will only be checked once for the current field name prefix and Document Number
            if (HistoricTableData is null || fieldNamePrefix != FieldNamePrefix || rowIdentity.DocumentCode != DocumentNumber || rowIdentity.ClientID != ClientID)
            {
 
                HistoricTableData = new DataTable("HistoricData_" + fieldNamePrefix);
                FieldNamePrefix = fieldNamePrefix;
                DocumentNumber = rowIdentity.DocumentCode;
                ClientID = rowIdentity.ClientID;

                if (dbPub != null && dbPub.GetSqlConnection() != null )
                    if (!dbPub.LoadDataTable(@"SELECT DISTINCT a.[FIELD_NAME]
                    ,CASE WHEN e.[CONTENT] IS NULL THEN CAST(e.[XMLCONTENT] AS nvarchar(MAX)) ELSE e.[CONTENT] END As [Content_EN]
					,CASE WHEN f.[CONTENT] IS NULL THEN CAST(f.[XMLCONTENT] AS nvarchar(MAX)) ELSE f.[CONTENT] END As [Content_FR]
                    , head.FIELD_NAME AS Header_Field_Name, head.CONTENT AS Header_Content_EN, fh.CONTENT AS Header_Content_FR
                    FROM DOCUMENT_FIELD_VALUE AS e 
					LEFT OUTER JOIN DOCUMENT_FIELD_VALUE f ON e.DOCUMENT_ID = f.DOCUMENT_ID AND e.DOCUMENT_FIELD_ID = f.DOCUMENT_FIELD_ID AND f.LANGUAGE_ID = 2
                    INNER JOIN DOCUMENT_FIELD_ATTRIBUTE AS a ON a.DOCUMENT_FIELD_ID = e.DOCUMENT_FIELD_ID 
                    INNER JOIN DOCUMENT AS d ON d.DOCUMENT_ID = e.DOCUMENT_ID 
                    INNER JOIN LINE_OF_BUSINESS AS l ON l.BUSINESS_ID = d.BUSINESS_ID 
                    INNER JOIN COMPANY AS c ON c.COMPANY_ID = l.COMPANY_ID 
                    LEFT OUTER JOIN (SELECT eh.DOCUMENT_ID, eh.DOCUMENT_FIELD_ID, eh.LANGUAGE_ID, ah.FIELD_NAME, eh.CONTENT FROM DOCUMENT_FIELD_VALUE AS eh
                    INNER JOIN DOCUMENT_FIELD_ATTRIBUTE ah ON eh.DOCUMENT_FIELD_ID = ah.DOCUMENT_FIELD_ID 
                    WHERE eh.DATA_TYPE = 'TEXT'
                    ) AS head ON head.DOCUMENT_ID = e.DOCUMENT_ID AND head.LANGUAGE_ID = e.LANGUAGE_ID AND head.FIELD_NAME = CONCAT(a.FIELD_NAME, 'h')
					LEFT OUTER JOIN DOCUMENT_FIELD_VALUE fh ON head.DOCUMENT_ID = fh.DOCUMENT_ID AND head.DOCUMENT_FIELD_ID = fh.DOCUMENT_FIELD_ID AND fh.LANGUAGE_ID = 2 AND fh.DATA_TYPE = 'TEXT'
                    WHERE c.FEED_COMPANY_ID = @clientID 
                    AND a.FIELD_NAME LIKE @fieldName
                    AND e.DATA_TYPE IN ('TABLE', 'CHART')
                    AND e.LANGUAGE_ID = 1
                    AND d.DOCUMENT_NUMBER = @documentNumber
                    AND a.IS_ACTIVE = 1 
                    AND d.IS_ACTIVE = 1 
                    AND l.IS_ACTIVE = 1;",
                    new Dictionary<string, object>(4)
                    {
                        { "@clientID",  ClientID },
                        { "@documentNumber", DocumentNumber },
                        { "@fieldName",  FieldNamePrefix + "%" }
                    }, HistoricTableData))
                        Logger.AddError(_log, $"Error loading Historic Table Merge Data:  {dbPub.LastError}");
            }

            return (HistoricTableData != null ? HistoricTableData.Rows.Count : -1);
        }

        public PDI_DataTable GetHistoricFieldTable(string fieldName, RowIdentity rowIdentity, DBConnection dbPub)
        {
            if (fieldName.IsNaOrBlank())
                throw new ArgumentNullException("Field Name is required");

            if (LoadHistoricalData(fieldName, rowIdentity, dbPub) >= 0)
            {
                DataRow[] dRows = HistoricTableData.Select($"FIELD_NAME = '{fieldName.EscapeSQL()}'");

                if (dRows.Length == 1)
                    return dRows[0][1].ToString().XMLtoDataTable();
            }
            return null;
        }

        public string GetHistoricFieldString(string fieldName, bool getFrench = false)
        {
            if (fieldName.IsNaOrBlank())
                throw new ArgumentNullException("Field Name is required");

            if (HistoricTableData is null || !HistoricTableData.Columns.Contains("FIELD_NAME"))
                return null;

            DataRow[] dRows = HistoricTableData.Select($"FIELD_NAME = '{fieldName.EscapeSQL()}'");

            if (dRows.Length == 1)
                if (getFrench && dRows[0].Table.Columns.Contains("Content_FR"))
                    return dRows[0].GetExactColumnStringValue("Content_FR");
                else if (getFrench)
                    return dRows[0][2].ToString();
                else if (dRows[0].Table.Columns.Contains("Content_EN"))
                    return dRows[0].GetExactColumnStringValue("Content_EN");
                else
                    return dRows[0][1].ToString();
            return null;

        }

        public PDI_DataTable GetHistoricFieldTable(string fieldName, bool getFrench = false)
        {
            return GetHistoricFieldString(fieldName, getFrench).XMLtoDataTable("Historic_" + fieldName);
        }

        public string GetHistoricHeaderString(string fieldName, bool getFrench = false)
        {
            if (fieldName.IsNaOrBlank())
                throw new ArgumentNullException("Field Name is required");

            if (HistoricTableData is null || !HistoricTableData.Columns.Contains("FIELD_NAME"))
                return null;

            DataRow[] dRows = HistoricTableData.Select($"FIELD_NAME = '{fieldName.EscapeSQL()}'");

            if (dRows.Length == 1)
                if (getFrench && dRows[0].Table.Columns.Contains("Header_Content_FR"))
                    return dRows[0].GetExactColumnStringValue("Header_Content_FR");
                else if (getFrench)
                    return dRows[0][4].ToString();
                else if (dRows[0].Table.Columns.Contains("Header_Content_EN"))
                    return dRows[0].GetExactColumnStringValue("Header_Content_EN");
                else
                    return dRows[0][5].ToString();

            return null;
        }

        public string MergeTableData(string currentDataTable, string fieldName, RowIdentity rowIdentity = null, DBConnection dbPub = null, bool getFrench = false)
        {
            if (currentDataTable.IsNaOrBlank())
                throw new ArgumentNullException("currentTable");
            if (fieldName.IsNaOrBlank())
                throw new ArgumentNullException("fieldName");

            if (rowIdentity != null && dbPub != null)
                LoadHistoricalData(fieldName, rowIdentity, dbPub);

            if (!GetMergeSettings(fieldName))
            {
                Logger.AddError(_log, $"Unable to load table merge settings for {fieldName} - skipping import");
                return null;
            }

            if (!HistoricTableData.Columns.Contains("Field_Name"))
            {
                Logger.AddError(_log, $"Unable to load historic data from Publisher for Document {rowIdentity.DocumentCode} and Field {fieldName} - skipping import");
                return null;
            }

            PDI_DataTable current = currentDataTable.XMLtoDataTable("Current_" + fieldName);
            PDI_DataTable hist = GetHistoricFieldTable(fieldName, getFrench);

            if (hist is null || hist.Rows.Count < 1 || hist.Columns.Count < 1 || hist.Rows.Count <= CheckRow) // check the history table rows and columns in case we are pulling a blank <table /> or a <table><row /></table> that Publisher uses for deleting tables for some reason
            {
                Logger.AddError(_log, $"No historic data found during table merge for Document {(rowIdentity == null?"TEST": rowIdentity.DocumentCode)} and Field {fieldName} {(getFrench?"fr-CA":"")} - Loading current table");
                return currentDataTable.Contains("<table") ? currentDataTable : null;
            }

            current = CombineTables(current, hist);
            
            return current.DataTabletoXML();
        }

        //public void MergeTableData(PDI_DataTable currentDataTable, PDI_DataTable historicalDataTable)
        //{
        //    if (historicalDataTable != null && historicalDataTable.Rows.Count > 0 && currentDataTable != null && currentDataTable.Rows.Count > 0)
        //        UpdateHistoricalDataTableWithCurrentData(historicalDataTable, currentDataTable);
        //    else
        //        throw new ArgumentNullException("Current Data Table and Historic Data Table are required and must have rows");
        //    //RemoveUnwanted(historicalDataTable);
        //}

        public bool IsRerun(PDI_DataTable table1, PDI_DataTable table2)
        {
            if (table1 is null || table1.Rows.Count < 1)
                throw new ArgumentNullException("table1");

            if (table2 is null || table2.Rows.Count < 1)
                throw new ArgumentNullException("table2");

            if (CheckRow > Math.Min(table1.Rows.Count - 1, table2.Rows.Count - 1) || CheckColumn > Math.Min(table1.Columns.Count - 1, table2.Columns.Count - 1))
                throw new System.ArgumentOutOfRangeException("MergeSettings.CheckField");

            string histData = table1.Rows[(CheckRow >= 0 ? CheckRow : table1.Rows.Count - 1)][(CheckColumn >= 0 ? CheckColumn : table1.Columns.Count - 1)].ToString();
            string curData = table2.Rows[(CheckRow >= 0 ? CheckRow : table2.Rows.Count - 1)][(CheckColumn >= 0 ? CheckColumn : table2.Columns.Count - 1)].ToString();

            return (histData.RestoreAngleBrackets().RemoveHTML().RemoveExceptAlphaNumeric() == curData.RestoreAngleBrackets().RemoveHTML().RemoveExceptAlphaNumeric());
        }

        public bool IsInterim(PDI_DataTable checkTable)
        {
            if (checkTable is null || checkTable.Rows.Count < 1)
                throw new ArgumentNullException("table1");

            if (CheckRow > checkTable.Rows.Count - 1 || CheckColumn > checkTable.Columns.Count - 1)
                throw new System.ArgumentOutOfRangeException("MergeSettings.CheckField");

            string checkField = checkTable.Rows[(CheckRow >= 0 ? CheckRow : checkTable.Rows.Count - 1)][(CheckColumn >= 0 ? CheckColumn : checkTable.Columns.Count - 1)].ToString();
            
            return (checkField.RestoreAngleBrackets().RemoveHTML().RemoveExceptAlphaNumeric().Contains(MergeSettings.InterimIndicator.RestoreAngleBrackets().RemoveHTML().RemoveExceptAlphaNumeric()));
        }

        private PDI_DataTable CopyCells(PDI_DataTable target, PDI_DataTable source, int offset = 1)
        {
            for (int r = 0; r < source.Rows.Count; r++)
            {
                if (MergeSettings.DesiredRows > 0 && target.Rows.Count <= r + offset)
                    target.Rows.Add();

                CopyExtendedProperties((PDI_DataRow)target.Rows[r], (PDI_DataRow)source.Rows[r]);

                for (int c = 0; c < source.Columns.Count; c++)
                {
                    if (MergeSettings.DesiredColumns > 0 && target.Columns.Count <= c + offset)
                        target.Columns.Add("NewColumn" + (c + 1 + offset).ToString());

                    if (r > CheckRow && c + offset > CheckColumn) 
                        target.Rows[r][c + offset] = source.Rows[r][c];
                    else if (target.Rows[r][c].ToString().IsNaOrBlank() && !source.Rows[r][c].ToString().IsNaOrBlank())
                        target.Rows[r][c] = source.Rows[r][c];

                }
            }

            return target;
        }

        private void CopyExtendedProperties(PDI_DataRow target, PDI_DataRow source)
        {
            foreach (string key in source.ExtendedProperties.Keys)
            {
                if (target.ExtendedProperties.ContainsKey(key))
                    target.ExtendedProperties[key] = source.ExtendedProperties[key];
                else
                    target.ExtendedProperties.Add(key, source.ExtendedProperties[key]);
            }
        }
/*
 *      
        private void UpdateHistoricalDataTableWithCurrentData(PDI_DataTable historicalDataTable, PDI_DataTable currentDataTable, bool isSemiMerge = false)
        {

            if (IsRerun(historicalDataTable, currentDataTable)) // update the historic data as we've already run at least one merge
            {
                if (MergeSettings.DesiredRows > 0) // This is a row based merge - columns should be equal
                {
                    if (historicalDataTable.Columns.Count != currentDataTable.Columns.Count)
                        throw new Exception("Historic and Current columns don't match and can't be merged");

                    for (int c = 0; c < historicalDataTable.Columns.Count; c++)
                    {
                        if (c != CheckColumn) // don't update the check column
                            historicalDataTable.Rows[(CheckRow >= 0 ? CheckRow : historicalDataTable.Rows.Count - 1)][c] = currentDataTable.Rows[(CheckRow >= 0 ? CheckRow : currentDataTable.Rows.Count - 1)][c];
                    }
                }
                else
                {
                    if (historicalDataTable.Rows.Count != currentDataTable.Rows.Count)
                        throw new Exception("Historic and Current rows don't match and can't be merged");

                    for (int r = 0; r < historicalDataTable.Rows.Count; r++)
                    { 
                        if (r != CheckRow) // don't update the check row
                            historicalDataTable.Rows[r][(CheckColumn >= 0 ? CheckColumn : historicalDataTable.Columns.Count - 1)] = currentDataTable.Rows[r][(CheckColumn >= 0 ? CheckColumn : currentDataTable.Columns.Count - 1)];
                    }
                }   
            }
            else // this is not a rerun so use desiredRows/desiredColumns 
            {
                if (MergeSettings.DesiredRows > 0) // This is a row based merge - columns should be equal
                {
                    if (historicalDataTable.Columns.Count != currentDataTable.Columns.Count)
                        throw new Exception("Historic and Current columns don't match and can't be merged");

                    while (historicalDataTable.Rows.Count >= MergeSettings.DesiredRows) // we need to remove the least relevant row
                        historicalDataTable.Rows.RemoveAt(CheckRow >= 0 ? historicalDataTable.Rows.Count - 1 : 0); // assume the least relevant is the last if checkfield row is positive or first if checkfield is last

                    if (CheckRow >= 0) // insert the row at the checkfield row
                    {
                        PDI_DataTable histCopy = (PDI_DataTable)historicalDataTable.Clone();
                        for (int r = 0; r < historicalDataTable.Rows.Count; r++)
                        {
                            if (r == CheckRow)
                                histCopy.ImportRow(currentDataTable.Rows[r]);
                            histCopy.ImportRow(historicalDataTable.Rows[r]);
                        }            
                        historicalDataTable = histCopy;
                    }
                    else // use import to add the row to the end
                        historicalDataTable.ImportRow(currentDataTable.Rows[(CheckRow >= 0 ? CheckRow : currentDataTable.Rows.Count - 1)]);
                }
                else // column merge - rows should be equal
                {
                    if (historicalDataTable.Rows.Count != currentDataTable.Rows.Count)
                        throw new Exception("Historic and Current rows don't match and can't be merged");

                    while (historicalDataTable.Columns.Count >= MergeSettings.DesiredColumns) // we need to remove the least relevant column
                        historicalDataTable.Columns.RemoveAt(CheckColumn >= 0 ? historicalDataTable.Columns.Count - 1 : 0);

                    DataColumn dc = historicalDataTable.Columns.Add("NewColumn", currentDataTable.Columns[(CheckColumn >= 0 ? CheckColumn : currentDataTable.Columns.Count - 1)].DataType);

                    for (int r = 0; r < currentDataTable.Rows.Count; r++)
                        historicalDataTable.Rows[r][dc] = currentDataTable.Rows[r][(CheckColumn >= 0 ? CheckColumn : currentDataTable.Columns.Count - 1)];

                    if (CheckColumn >= 0)
                        dc.SetOrdinal(CheckColumn);
                }
            }
        }
*/
        private PDI_DataTable CombineTables(PDI_DataTable newTable, PDI_DataTable oldTable)
        {
            if (newTable is null || oldTable is null)
                return null;

            PDI_DataTable tempTable = null;
            bool isRerun = IsRerun(newTable, oldTable); // do this before removing interim values in case it's an interim rerun
            if (IsInterim(oldTable) && !isRerun)
            {
                if (MergeSettings.DesiredRows > 0) // we have a row based table so remove the interim value first
                    oldTable.Rows.RemoveAt((CheckRow >= 0 ? CheckRow : oldTable.Rows.Count - 1));

                if (MergeSettings.DesiredColumns > 0) // we have a column based table so remove the interim column first
                    oldTable.Columns.RemoveAt((CheckColumn >= 0 ? CheckColumn : oldTable.Columns.Count - 1));
            }
            

            if (isRerun) // update the historic data as we've already run at least one merge -- there is a bug if the historic table only has an interim value as it's been removed by IsInterim - check for rerun first?
            {
                if (MergeSettings.DesiredRows > 0) // This is a row based merge - columns should be equal
                {
                    if (newTable.Columns.Count != oldTable.Columns.Count)
                        throw new Exception("Historic and Current columns don't match and can't be merged");

                    for (int c = 0; c < oldTable.Columns.Count; c++)
                        oldTable.Rows[(CheckRow >= 0 ? CheckRow : oldTable.Rows.Count - 1)][c] = newTable.Rows[(CheckRow >= 0 ? CheckRow : newTable.Rows.Count - 1)][c]; // copy the new table data into the old table data at the check row
                        
                    CopyExtendedProperties((PDI_DataRow)oldTable.Rows[(CheckRow >= 0 ? CheckRow : oldTable.Rows.Count - 1)], (PDI_DataRow)newTable.Rows[(CheckRow >= 0 ? CheckRow : newTable.Rows.Count - 1)]);
                    tempTable = oldTable;
                }
                else
                {
                    if (newTable.Rows.Count != oldTable.Rows.Count)
                        throw new Exception("Historic and Current rows don't match and can't be merged");

                    tempTable = CopyCells(newTable, oldTable, 0); // no offset
                    //for (int r = 0; r < oldTable.Rows.Count; r++)
                    //{
                    //    if (CheckColumn >= 0)
                    //        for (int c = 0; c <= CheckColumn && c < oldTable.Columns.Count; c++)
                    //            oldTable.Rows[r][c] = newTable.Rows[r][c]; // copy the new table data into the historic table
                    //    else
                    //        oldTable.Rows[r][oldTable.Columns.Count - 1] = newTable.Rows[r][newTable.Columns.Count - 1];
                    //}
                           
                }
               
            }
            else // this is not a rerun so use desiredRows/desiredColumns 
            {
                if (MergeSettings.DesiredRows > 0) // This is a row based merge - columns should be equal
                {
                    if (newTable.Columns.Count != oldTable.Columns.Count)
                        throw new Exception("Historic and Current columns don't match and can't be merged");

                    //while (historicalDataTable.Rows.Count >= MergeSettings.DesiredRows) // we need to remove the least relevant row
                    //    historicalDataTable.Rows.RemoveAt(CheckRow >= 0 ? historicalDataTable.Rows.Count - 1 : 0); // assume the least relevant is the last if checkfield row is positive or first if checkfield is last

                    if (CheckRow >= 0) // insert the old data starting at the checkfield into the next row in the new table - add rows as needed
                    {
                        while (newTable.Rows.Count > CheckRow + 1) // remove any extra rows in the new table - since only current year is submitted there may be extra blank years
                            newTable.Rows.RemoveAt(CheckRow + 1);

                        for (int r = CheckRow; r < oldTable.Rows.Count; r++) // add all the historic rows
                            newTable.ImportRow(oldTable.Rows[r]);

                        tempTable = newTable;
                    }
                    else // new data is added to the end of old data
                    {
                        for (int r = 0; r < newTable.Rows.Count; r++)
                            oldTable.ImportRow(newTable.Rows[r]);

                        tempTable = oldTable;
                    }
                }
                else // column merge - rows should be equal
                {
                    if (newTable.Rows.Count != oldTable.Rows.Count)
                        throw new Exception("Historic and Current rows don't match and can't be merged");

                    //while (historicalDataTable.Columns.Count >= MergeSettings.DesiredColumns) // we need to remove the least relevant column
                    //    historicalDataTable.Columns.RemoveAt(CheckColumn >= 0 ? historicalDataTable.Columns.Count - 1 : 0);

                    tempTable =  CopyCells(newTable, oldTable);
                   
                }
            }
            return RemoveExtra(tempTable);

        }

        private PDI_DataTable RemoveExtra(PDI_DataTable checkTable)
        {
            bool isInterim = IsInterim(checkTable);

            if (MergeSettings.DesiredRows > 0)
                while (checkTable.Rows.Count > MergeSettings.DesiredRows + (isInterim ? MergeSettings.InterimOffset : 0))
                    checkTable.Rows.RemoveAt(CheckRow >= 0 ? checkTable.Rows.Count - 1 : 0) ;
            else if (MergeSettings.DesiredColumns > 0)
                while (checkTable.Columns.Count > MergeSettings.DesiredColumns + (isInterim ? MergeSettings.InterimOffset : 0))
                    checkTable.Columns.RemoveAt(CheckColumn >= 0 ? checkTable.Columns.Count - 1 : 0);

            return checkTable;
        }
    }
}
