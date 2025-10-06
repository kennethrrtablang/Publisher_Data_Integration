using Publisher_Data_Operations.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;

namespace Publisher_Data_Operations.Extensions
{

    [Serializable]
    public class PDI_DataTable : DataTable
    {
        public PDI_DataTable()
            : base()
        {
        }

        public PDI_DataTable(string tableName)
            : base(tableName)
        {
        }

        public PDI_DataTable(string tableName, string tableNamespace)
            : base(tableName, tableNamespace)
        {
        }

        protected override Type GetRowType()
        {
            return typeof(PDI_DataRow);
        }

        protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
        {
            return new PDI_DataRow(builder);
        }

        public PDI_DataTable(DataTable dt)
        {
            if (dt != null)
            {
                this.Clear();

                foreach (object key in dt.ExtendedProperties.Keys)
                    this.ExtendedProperties.Add(key, dt.ExtendedProperties[key]);

                
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    this.Columns.Add(dt.Columns[col].ColumnName, dt.Columns[col].DataType, dt.Columns[col].Expression);
                    foreach (var key in dt.Columns[col].ExtendedProperties.Keys)
                        this.Columns[col].ExtendedProperties.Add(key, dt.Columns[col].ExtendedProperties[key]);
                } 
                
                if (dt.Columns.Contains(Generic.FSMRFP_ROWTYPE_COLUMN))
                {
                    for (int row = 0; row < dt.Rows.Count; row++)
                    {
                        PDI_DataRow newRow = (PDI_DataRow)this.NewRow();
                        newRow.ExtendedProperties.Add(Generic.FSMRFP_ROWTYPE_COLUMN.ToLower(), dt.Rows[row][Generic.FSMRFP_ROWTYPE_COLUMN].ToString());
                        for (int col = 0; col < dt.Columns.Count; col++)
                            newRow[col] = dt.Rows[row][col];
                        this.Rows.Add(newRow);
                    }
                    this.Columns.Remove(Generic.FSMRFP_ROWTYPE_COLUMN);
                }    
                else
                    for (int row = 0; row < dt.Rows.Count; row++)
                        this.ImportRow(dt.Rows[row]);

                if (dt.TableName != null)
                    this.TableName = dt.TableName;
            }

        }
    }

    [Serializable]
    public class PDI_DataRow : DataRow
    {
        public Dictionary<string, object> _extendedProperties = new Dictionary<string, object>();

        public Dictionary<string, object> ExtendedProperties
        {
            get { return _extendedProperties; }
        }

        public void SetAttribute(string name, object value)
        {
            ExtendedProperties.Add(name, value);
        }

        public PDI_DataRow()
            : base(null)
        {
        }

        public PDI_DataRow(DataRowBuilder builder)
            : base(builder)
        {
        }
    }

    public static class Data
    {
        /// <summary>
        /// Retrieve the column name from a DataRow based on a partial column name
        /// </summary>
        /// <param name="dr">The DataRow</param>
        /// <param name="column">The partial column name</param>
        /// <returns>the matching column string value</returns>
        public static string GetPartialColumnStringValue(this DataRow dr, string column)
        {
            return dr.GetStringValue(dr.FindDataRowColumn(column));
        }

        /// <summary>
        /// Return the string value of an exact column name - when using it on extract check in <>
        /// </summary>
        /// <param name="dr">The Datarow</param>
        /// <param name="column">The exact column name</param>
        /// <returns>the column string value</returns>
        public static string GetExactColumnStringValue(this DataRow dr, string column)
        {
            if (dr.Table.Columns.Contains(column))
            {
                if (dr.Table.Columns[column].DataType.Name.IndexOf("String", StringComparison.OrdinalIgnoreCase) >= 0)
                    return dr.Field<string>(column);
                else
                    return dr[column].ToString();
            }
            int col = dr.FindDataRowColumn($"<{column}>");
            if (col >= 0)
                return dr.GetStringValue(col);
            else if (dr.FindDataRowColumn(column) >= 0)
                Logger.AzureWarning(null, $"Unable to find column '{column}' did you mean '{dr.Table.Columns[dr.FindDataRowColumn(column)].ColumnName}'?");
            
            return string.Empty;
            
        }

        public static string GetStringValue(this DataRow dr, int col)
        {
            if (col >= 0)
                return dr[col].ToString().Trim(); // dr.Field<string>(col);

            return string.Empty;
        }

        public static int GetPartialColumnIntValue(this DataRow dr, string column)
        {
            return dr.GetIntValue(dr.FindDataRowColumn(column));
        }

        public static int GetExactColumnIntValue(this DataRow dr, string column)
        {
            if (dr.Table.Columns.Contains(column))
            {
                if (dr.IsNull(column))
                    return -1;
                else
                    return dr.GetIntValue(dr.Table.Columns[column].Ordinal);
            }
            int col = dr.FindDataRowColumn($"<{column}>");
            if (col >= 0)
                return dr.GetIntValue(col);
            else if (dr.FindDataRowColumn(column) >= 0)
                Logger.AzureWarning(null, $"Unable to find column '{column}' did you mean '{dr.Table.Columns[dr.FindDataRowColumn(column)].ColumnName}'?");
            
            return -1;

        }

        public static DateTime GetExactColumnDateValue(this DataRow dr, string column)
        {
            if (dr.Table.Columns.Contains(column) && !dr.IsNull(column))
                return dr.Field<DateTime>(column);
            else
                return DateTime.MinValue;
        }

        /// <summary>
        /// extract the positive int value from the column index - handles null and parsing
        /// </summary>
        /// <param name="dr">The DataRow</param>
        /// <param name="col">The integer column index</param>
        /// <returns>integer value extracted or -1 on failure</returns>
        public static int GetIntValue(this DataRow dr, int col)
        {
            if (col >= 0)
            {
                if (dr.IsNull(col))
                    return -1;
                else if (dr.Table.Columns[col].DataType.Name.IndexOf("Int") >= 0)
                    return dr.Field<int>(col);
                else
                {
                    if (int.TryParse(dr[col].ToString(), out int outVal))
                        return outVal;
                }
            }
            return -1;
        }

        /// <summary>
        /// Given a column name extract the FFDocAge as a nullable type
        /// </summary>
        /// <param name="dr">The Datarow</param>
        /// <param name="column">The exact column name</param>
        /// <returns>FFDocAge or null</returns>
        public static FFDocAge? GetExactColumnFFDocAge(this DataRow dr, string column)
        {
            if (dr.IsNull(column))
                return null;
            return dr.Field<FFDocAge>(column);
        }

        public static bool GetPartialColumnBoolValue(this DataRow dr, string column)
        {
            return dr.GetBoolValue(dr.FindDataRowColumn(column));
        }

        
        public static bool GetExactColumnBoolValue(this DataRow dr, string column)
        {
            return dr.GetExactColumnStringValue(column).ToBool();
        }

        public static bool GetBoolValue(this DataRow dr, int col)
        {
            if (col >= 0)
                return dr[col].ToString().Trim().ToBool();

            return false;
        }

        public static int FindDataRowColumn(this DataRow row, string column)
        {
            return row.Table.FindDataTableColumn(column);
        }

        /// <summary>
        /// For a given DataTable find the column number for a partial column name
        /// </summary>
        /// <param name="tbl">The DataTable to find the column in</param>
        /// <param name="column">The partial column name</param>
        /// <returns>The column position as an int</returns>
        public static int FindDataTableColumn(this DataTable tbl, string column)
        {
            foreach (DataColumn dc in tbl.Columns)
            {
                if (dc.ColumnName.IndexOf(column, StringComparison.OrdinalIgnoreCase) >= 0)
                    return dc.Ordinal;
            }
            return -1;
        }

        //https://stackoverflow.com/questions/17130902/datarow-getchanges-or-equivalent
        private static bool HasCellChanged(DataRow row, DataColumn col)
        {
            if (!row.HasVersion(DataRowVersion.Original))
            {
                // Row has been added. All columns have changed. 
                return true;
            }
            if (!row.HasVersion(DataRowVersion.Current))
            {
                // Row has been removed. No columns have changed.
                return false;
            }
            var originalVersion = row[col, DataRowVersion.Original];
            var currentVersion = row[col, DataRowVersion.Current];
            if (originalVersion == DBNull.Value && currentVersion == DBNull.Value)
            {
                return false;
            }
            else if (originalVersion != DBNull.Value && currentVersion != DBNull.Value)
            {
                return !originalVersion.Equals(currentVersion);
            }
            return true;
        }

        public static IEnumerable<DataColumn> GetChangedColumns(this DataRow row)
        {
            return row.Table.Columns.Cast<DataColumn>()
                .Where(col => HasCellChanged(row, col));
        }

        public static IEnumerable<DataColumn> GetChangedColumns(this IEnumerable<DataRow> rows)
        {
            return rows.SelectMany(row => row.GetChangedColumns())
                .Distinct();
        }

        public static IEnumerable<DataColumn> GetChangedColumns(this DataTable table)
        {
            return table.GetChanges().Rows
                .Cast<DataRow>()
                .GetChangedColumns();
        }

        public static bool RowHasChanged(this DataRow row)
        {
            return GetChangedColumns(row).Count() > 0;
        }

        public static bool RemoveBlankColumns(this DataTable dt, int minColumns = 0)
        {
            bool removed = false;
            foreach (DataColumn column in dt.Columns.Cast<DataColumn>().ToArray())
                if (dt.AsEnumerable().All(dRow => dRow.IsNull(column) || dRow[column].ToString() == string.Empty)) // remove completely empty columns
                {
                    if (minColumns <= 0 || dt.Columns.Count > minColumns && column.ColumnName != Generic.FSMRFP_ROWTYPE_COLUMN) //bug in FSMRFP extraction - don't remove a blank rowtype column
                    {
                        dt.Columns.Remove(column);
                        removed = true;
                    }    
                }

            return removed;
        }

        public static bool RemoveBlankColumnsAtEnd(this DataTable dt, bool sourceHasRowType, string formatColumnName)
        {
            bool removed = false;
            List<DataColumn> remColumns = new List<DataColumn>(1);
            for (int i = dt.Columns.Count - 1; i >= 0; i--)
                if (dt.AsEnumerable().All(dRow => dRow.IsNull(i) || dRow[i].ToString() == string.Empty)) // remove completely empty columns
                {
                    remColumns.Add(dt.Columns[i]);
                    removed = true;
                }
                else
                    break; // stop removing once we hit data

            if (removed)
            {
                foreach (DataColumn col in remColumns)
                    dt.Columns.Remove(col);
                if (sourceHasRowType)
                    dt.Columns[dt.Columns.Count - 1].ColumnName = formatColumnName;
            }
            return removed;
        }

        public static bool RemoveBlankRows(this DataTable dt)
        {
            bool removed = false;
            foreach (DataRow row in dt.Rows.Cast<DataRow>().ToArray())
                if (row.ItemArray.All(dCol => dCol is null || dCol.ToString().Trim() == string.Empty))
                {
                    dt.Rows.Remove(row);
                    removed = true;
                }

            return removed;
        }

        public static bool IsBlankRow(this PDI_DataRow dr)
        {
            return dr.ItemArray.All(dCol => dCol is null || dCol.ToString() == string.Empty);
        }

        /// <summary>
        /// Extra confirmation that the generated XML is error free - used to log errors
        /// </summary>
        /// <param name="dt">The Transformed DataTable</param>
        /// <param name="log">The Logger to write errors to</param>
        public static bool ValidateXML(this DataTable dt, Helper.Logger log = null)
        {
            ParameterValidator pm = new ParameterValidator(new Dictionary<string, string>());
            bool retVal = true;
            foreach (DataRow dr in dt.Rows)
            {
                if (!dr.Field<bool>("isTextField"))
                {
                    string content = dr.Field<string>("Content");
                    if (content.IndexOf("<") == 0)
                    {
                        content = content.CleanXML();
                        if (!pm.IsValidXML(content, false))
                        {
                            retVal = false;
                            Helper.Logger.AddError(log, $"Invalid {dr.Field<string>("Field_Name")} {dr.Field<string>("Culture_Code")} - Last Error: {pm.LastError}");
                        }
                    }
                }
            }
            return retVal;
        }
        //https://www.c-sharpcorner.com/UploadFile/deveshomar/export-datatable-to-csv-using-extension-method/
        public static string ToCSV(this DataTable dtTable)
        {
            StringBuilder sb = new StringBuilder();
            //Headers  
            for (int i = 0; i < dtTable.Columns.Count; i++)
            {
                sb.Append(dtTable.Columns[i]);
                if (i < dtTable.Columns.Count - 1)
                    sb.Append(",");
            }
            sb.Append(Environment.NewLine);
            //Content
            foreach (DataRow dr in dtTable.Rows)
            {
                for (int i = 0; i < dtTable.Columns.Count; i++)
                {
                    if (!Convert.IsDBNull(dr[i]))
                    {
                        string value = dr[i].ToString();
                        if (value.Contains(',') || value.Contains(Environment.NewLine))
                        {
                            value = $"\"{value}\"";
                            sb.Append(value);
                        }
                        else
                        {
                            sb.Append(dr[i].ToString());
                        }
                    }
                    if (i < dtTable.Columns.Count - 1)
                        sb.Append(",");
                }
                sb.Append(Environment.NewLine);
            }
            return sb.ToString();
        }


        public static Dictionary<string, string> GetDataRowDictionary(this DataRow dr, Dictionary<string, string> docFields = null)
        {
            
            if (dr != null && dr.Table.Columns.Count > 0)
            {
                Dictionary<string, string> rowDictionary = new Dictionary<string, string>(dr.Table.Columns.Count);
                foreach (DataColumn dc in dr.Table.Columns)
                {
                    if ((docFields != null && docFields.Keys.Contains(dc.ColumnName)) || dc.ColumnName.Contains(PDIFile.FILE_DELIMITER))
                        continue; // don't add fields that are already in the Publisher document
                    rowDictionary.Add(dc.ColumnName, dr[dc].ToString());
                }
                return rowDictionary;
            }
            return null;
        }

        public static Dictionary<string, string> GetDataRowDictionaryLocal(this DataRow dr)
        {
            if (dr != null && dr.Table.Columns.Count > 0)
            {
                Dictionary<string, string> rowDictionary = new Dictionary<string, string>(dr.Table.Columns.Count);
                foreach (DataColumn dc in dr.Table.Columns)
                {
                    if (rowDictionary.ContainsKey(dc.ColumnName))
                        continue; // don't add fields that already exist

                    switch (dc.DataType.ToString())
                    {
                        //case "System.Int32":
                        //    if (int.TryParse(row[field].ToString(), out int resInt))
                                
                        //    break;
                        //case "System.Boolean":
                        //    UpdateIfChanged(matchedDoc, field, row[field].ToString().ToBool()); // matchedDoc[field] = row[field].ToString().ToBool();
                        //    break;
                        case "System.DateTime":
                            if (dr[dc] is DBNull)
                                rowDictionary.Add(dc.ColumnName, string.Empty);
                            else
                                rowDictionary.Add(dc.ColumnName, ((System.DateTime)dr[dc]).ToLocalTime().ToString("yyyy-MM-dd h:mm:ss tt"));
                             
                         
                            break;
                        //case "System.String":
                        //    UpdateIfChanged(matchedDoc, field, row[field].ToString().NaOrBlankNull()); // matchedDoc[field] = AsNullString(row[field].ToString(), matchedDoc.Field<string>(field));
                        //    break;
                        //case "Publisher_Data_Operations.Entities.FFDocAge":
                        //    if (int.TryParse(row[field].ToString(), out int resFF))
                        //        UpdateIfChanged(matchedDoc, field, (FFDocAge)resFF); // matchedDoc[field] = (FFDocAge)resFF;
                        //    break;
                        default:
                            rowDictionary.Add(dc.ColumnName, dr[dc].ToString());
                            break;
                    }
                }
                return rowDictionary;
            }
            return null;
        }

        public static string ReplaceByDataRow(this string input, DataRow dr, Generic gen, bool isFrench = false, string naValue = "-")
        {
            if (input.ContainsXML())
            {
                Dictionary<string, string> allTokens = gen.loadValidParameters();
                if (allTokens != null && allTokens.Count > 0)
                {
                    for (int i = 0; i<allTokens.Count; i++)
                    {
                        string key = allTokens.Keys.ElementAt(i);
                        if (dr.Table.Columns.Contains(key))
                        {
                            //switch (dr.Table.Columns[key].DataType.ToString())
                            //{
                            //    case "System.Int32":
                            //    case "System.Boolean":
                            //        allTokens[key] = dr[key].ToString();
                            //        break;
                            //    case "System.Boolean":
                            //        allTokens[key] = dr[key].ToString(); // matchedDoc[field] = row[field].ToString().ToBool();
                            //        break;
                            //    case "System.DateTime":
                            //        if (System.DateTime.TryParse(row[field].ToString(), out System.DateTime resDate))
                            //            UpdateIfChanged(matchedDoc, field, resDate); // matchedDoc[field] = resDate;
                            //        else if (row[field].ToString().Length > 0)
                            //            UpdateIfChanged(matchedDoc, field, row[field].ToString().ToDate(System.DateTime.MinValue)); // matchedDoc[field] = row[field].ToString().ToDate(System.DateTime.MinValue);
                            //        break;
                            //    case "System.String":
                            //        UpdateIfChanged(matchedDoc, field, row[field].ToString().NaOrBlankNull()); // matchedDoc[field] = AsNullString(row[field].ToString(), matchedDoc.Field<string>(field));
                            //        break;
                            //    case "Publisher_Data_Operations.Entities.FFDocAge":
                            //        if (int.TryParse(row[field].ToString(), out int resFF))
                            //            UpdateIfChanged(matchedDoc, field, (FFDocAge)resFF); // matchedDoc[field] = (FFDocAge)resFF;
                            //        break;
                            //    default:

                            //        UpdateIfChanged(matchedDoc, field, row[field].ToString());// matchedDoc[field] = row[field].ToString();
                            //        break;
                            //}
                            string val = dr[key].ToString();
                            if (val.IsDate())
                                allTokens[key] = gen.longFormDate(val)[isFrench ? 1 : 0];
                            else if (val.IsNaOrBlank())
                                allTokens[key] = naValue;
                            else if (val.GetScale() > 0)
                                allTokens[key] = val;
                            else if (int.TryParse(val, out int valInt))
                                allTokens[key] = val.ToDecimal(-1, 0, isFrench ? "fr-CA" : "en-CA");
                            else
                                allTokens[key] = val;
                        }
                           
                    }
                }
                Dictionary<string, string> matchedTokens = allTokens.Where(f => f.Value.Length > 0).ToDictionary(x => x.Key, x => x.Value); // Clear any tokens that were not found (will leave them in input)

                input = input.ReplaceByDictionary(matchedTokens);
            }
            return input;
        }

        public static PDI_DataTable XMLtoDataTable(this string xmlString, string tableName = null)
        {
            if (xmlString is null || xmlString.Trim() == string.Empty)
                return null; // throw new ArgumentNullException("xmlString");

            PDI_DataTable dt = new PDI_DataTable(tableName);
            string[] rows = xmlString.ReplaceCI("<table>", string.Empty).ReplaceCI("</table>", string.Empty).ReplaceCI("<table/>", string.Empty).ReplaceCI("<table />", string.Empty).Split(new[] { "<row" }, StringSplitOptions.RemoveEmptyEntries);

            if (rows.Length > 0 && rows[0].IndexOf("<table ", StringComparison.OrdinalIgnoreCase) >= 0) // table contains extended properties
            {
                MatchCollection mc = Regex.Matches(rows[0], @"(\S+)=[""']?((?:.(?![""']?\s+(?:\S+)=|\s*\/?[>""']))*.)[""']?"); //Changed + to * to match single character attributes
                foreach (Match m in mc)
                    if (m.Groups.Count == 3 && m.Groups[2].Value != "\"" && m.Groups[2].Value != "'")
                        dt.ExtendedProperties.Add(m.Groups[1].Value, m.Groups[2].Value);

                List<string> templist = rows.ToList();
                templist.RemoveAt(0);
                rows = templist.ToArray();
            }
            foreach (string row in rows)
            {
                // extract any attributes from the row
                PDI_DataRow newRow = (PDI_DataRow)dt.NewRow();
                try
                {
                    MatchCollection mc = Regex.Matches(row.Substring(0, (row.IndexOf(">") >= 0 ? row.IndexOf(">") : 0)), @"(\S+)=[""']?((?:.(?![""']?\s + (?:\S +)=|[> ""']))*.)[""']?"); //can grab " or ' on empty attributes
                    foreach (Match m in mc)
                        if (m.Groups.Count == 3)
                            if (m.Groups[2].Value == "\"" || m.Groups[2].Value == "'")
                                newRow.ExtendedProperties.Add(m.Groups[1].Value, string.Empty);
                            else
                                newRow.ExtendedProperties.Add(m.Groups[1].Value, m.Groups[2].Value);
                }
                catch (Exception e)
                {
                    Logger.AzureError(null, "Unexpected Error in XMLtoDataTable: " + e.Message);
                }
                

                if (row.IndexOf("</row>", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    string[] columns = row.Split(new[] { "<cell" }, StringSplitOptions.None);
                    for (int c = 1; c < columns.Count(); c++)
                    {
                        if (c > dt.Columns.Count) // previous method relied on the number of columns in the first row - if the first row was blank it would fail
                            dt.Columns.Add(new DataColumn($"Column{c}", typeof(string)));

                        MatchCollection mc = Regex.Matches(columns[c].Substring(0, (columns[c].IndexOf(">") >= 0 ? columns[c].IndexOf(">") : 0)), @"(\S+)=[""']?((?:.(?![""']?\s + (?:\S +)=|[> ""']))*.)[""']?"); //
                        foreach (Match m in mc)
                            if (m.Groups.Count == 3 && dt.Columns[c-1].ColumnName != m.Groups[2].Value && !dt.Columns[c-1].ExtendedProperties.Contains(m.Groups[1].Value))
                            {
                                if (m.Groups[1].Value == "ColumnName")
                                    dt.Columns[c - 1].ColumnName = m.Groups[2].Value;
                                else
                                {
                                    if (m.Groups[2].Value == "\"" || m.Groups[2].Value == "'")
                                        dt.Columns[c - 1].ExtendedProperties.Add(m.Groups[1].Value, string.Empty);
                                    else
                                        dt.Columns[c - 1].ExtendedProperties.Add(m.Groups[1].Value, m.Groups[2].Value);
                                }
                            }
                                

                        if (columns[c].IndexOf(">") >= 0)
                            if (columns[c].IndexOf("</c") >= 0)
                                newRow[c - 1] = columns[c].Substring(columns[c].IndexOf(">") + 1, columns[c].LastIndexOf("</c") - columns[c].IndexOf(">") - 1); // last index so we skip any HTML and go to the closing </cell> tag
                            else
                                newRow[c - 1] = columns[c].Substring(columns[c].IndexOf(">") + 1).Replace("</row>", string.Empty);
                        else
                            newRow[c - 1] = columns[c].Replace("</row>", string.Empty);

                        //newRow[c - 1] = columns[c].Replace("</cell>", string.Empty).Replace("</row>", string.Empty).Replace("<cell />", string.Empty).Replace("<cell/>", string.Empty);
                    }


                    dt.Rows.Add(newRow);
                }
                else if (row.IndexOf("/>") >= 0)
                    dt.Rows.Add(newRow); // add a blank row

            }    
            return dt;
        }

        public static string DataTabletoXML(this DataTable dt, bool removeNA = false)
        { 
            if (dt is null)
                return null;

            PDI_DataTable pdi_dt = new PDI_DataTable(dt);
            return pdi_dt.DataTabletoXML(removeNA);
        }

        public static string DataTabletoXML(this PDI_DataTable dt, bool removeNA = false, bool preserveColumnNames = false)
        {
            if (dt is null)
                return null;

            if (dt.Rows.Count == 0)
                return ("<table/>");
           
            StringBuilder tableString = new StringBuilder("<table");
            foreach (object key in dt.ExtendedProperties.Keys)
                tableString.Append($" {key}=\"{dt.ExtendedProperties[key]}\"");
            tableString.Append(">");

            foreach (PDI_DataRow dr in dt.Rows)
            {
                //if (dt.Columns.Contains(Generic.FSMRFP_ROWTYPE_COLUMN) && dr[Generic.FSMRFP_ROWTYPE_COLUMN].ToString().Trim().Length > 0)
                tableString.Append("<row");
                if (dr.ExtendedProperties.Count > 0)
                    foreach (string key in dr.ExtendedProperties.Keys)
                        tableString.Append($" {key}=\"{dr.ExtendedProperties[key]}\"");

                if (dt.Columns.Count < 1 || dr.IsBlankRow())
                    tableString.Append(" />");
                else
                {
                    tableString.Append(">");

                    for (int i = 0; i < dr.Table.Columns.Count; i++)
                    {
                        string val = dr[i].ToString();
                        if (removeNA && val.IsNaOrBlank())
                            val = string.Empty;

                        tableString.Append("<cell");
                        foreach (string key in dr.Table.Columns[i].ExtendedProperties.Keys)
                            tableString.Append($" {key}=\"{dr.Table.Columns[i].ExtendedProperties[key]}\"");
                        if (preserveColumnNames && !dr.Table.Columns[i].ExtendedProperties.Contains("ColumnName") && dr.Table.Columns[i].ColumnName.IndexOf("Column") != 0)
                            tableString.Append($" ColumnName=\"{dr.Table.Columns[i].ColumnName}\"");

                        if (val.Length > 0)
                            tableString.Append($">{val}</cell>");
                        else
                            tableString.Append(" />");
                    }
                    tableString.Append("</row>");
                }
            }
            tableString.Append("</table>");

            return tableString.ToString();
        }

        public static string DataTabletoHTML(this DataTable dt, bool addHeader, bool processColors = false)
        {
            StringBuilder tableString = new StringBuilder("<table style='border-spacing: 5px;border: none;'>");
            if (addHeader)
            {
                tableString.Append("<tr>");
                foreach (DataColumn col in dt.Columns)
                    tableString.Append($"<th>{col.ColumnName}</th>");
                tableString.Append("</tr>");
            }
               
            foreach (DataRow dr in dt.Rows)
            {
                tableString.Append("<tr>");
                for (int i = 0; i < dr.Table.Columns.Count; i++)
                {
                    if (processColors && Processing.ProcessingText.ContainsValue(dr[i].ToString()))
                    {
                        if (dr[i].Equals(Processing.ProcessingText[ProcessingStage.Complete]))
                            tableString.Append($"<td style='color:#00FF00'>{dr[i]}</td>");
                        else
                            tableString.Append($"<td style='color:#FF0000'>{dr[i]}</td>");
                    }
                    else
                        tableString.Append($"<td>{dr[i]}</td>");
                } 
                tableString.Append("</tr>");
            }
            tableString.Append("</table>");

            return tableString.ToString();
        }

        public static DataRow XMLtoDataRow(this DataTable dt, string rowString)
        {
            string[] columns = rowString.ReplaceCI("<row>", string.Empty).ReplaceCI("</row>", string.Empty).Split(new[] { "<cell>" }, StringSplitOptions.RemoveEmptyEntries);
            DataRow dr = dt.NewRow();
            if (columns.Count() != dt.Columns.Count)
                dr[0] = "Columns in table and new row don't match";
            else
            {
                for (int c = 0; c < columns.Count(); c++)
                    dr[c] = columns[c].RemoveHTML().Trim();
            }
            return dr;
        }

        public static int FindHeaderColumn(this DataTable dt, string headerText, int headerRow = 0)
        {
            if (dt != null && dt.Rows.Count > 0 && dt.Columns.Count > 0 && dt.Rows.Count > headerRow)
            {
                for (int c = 0; c < dt.Columns.Count; c++)
                {
                    if (dt.Rows[headerRow][c].ToString().IndexOf(headerText, StringComparison.OrdinalIgnoreCase) == 0)
                        return c;
                }
            }
            return -2;
        }

        // For testing
        public static void ConsolePrintTable(this DataTable tbl)
        {
            if (tbl.TableName.Length > 0)
                Console.WriteLine(tbl.TableName);
            foreach (DataColumn col in tbl.Columns)
                Console.Write(col.ColumnName + " | ");
            Console.WriteLine();

            foreach (DataRow dataRow in tbl.Rows)
            {
                foreach (DataColumn col in dataRow.Table.Columns)
                    Console.Write($"'{(dataRow[col] != null && dataRow[col].ToString().Length > 60 ? dataRow[col].ToString().Substring(0, 60) : dataRow[col].ToString())}' ");
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}
