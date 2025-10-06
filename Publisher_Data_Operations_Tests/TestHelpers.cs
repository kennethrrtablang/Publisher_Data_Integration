using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Publisher_Data_Operations.Extensions;

namespace Publisher_Data_Operations_Tests
{
    public class TestHelpers
    {
        public static DataTable LoadXMLTable(string xml, DataTable dt)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xml);


            XmlReader xmlReader = new XmlNodeReader(xmlDoc);
            DataSet ds = new DataSet();
            ds.ReadXml(xmlReader);


            for (int r = 0; r < ds.Tables[0].Rows.Count; r++)
            {
                DataRow newRow = dt.NewRow();

                foreach (DataTable dtCur in ds.Tables) // readXML breaks the incoming data into multiple tables 
                {
                    foreach (DataColumn dc in dtCur.Columns)
                    {
                        if (!dt.Columns.Contains(dc.ColumnName) || r >= dtCur.Rows.Count) // make sure the column we are looking for exists in the target table and that the row exists in the current table
                            continue;

                        switch (dt.Columns[dc.ColumnName].DataType.ToString())
                        {
                            case "System.Int32":
                                if (int.TryParse(dtCur.Rows[r][dc].ToString(), out int resInt))
                                    newRow[dc.ColumnName] = resInt;
                                break;
                            case "System.Boolean":
                                newRow[dc.ColumnName] = dtCur.Rows[r][dc].ToString().ToBool();
                                break;
                            case "System.DateTime":
                                if (DateTime.TryParse(dtCur.Rows[r][dc].ToString(), out DateTime resDate))
                                    newRow[dc.ColumnName] = resDate;
                                else if (dtCur.Rows[r][dc].ToString().Length > 0)
                                    newRow[dc.ColumnName] = dtCur.Rows[r][dc].ToString().ToDate(DateTime.MinValue);
                                break;
                            case "System.String":
                                newRow[dc.ColumnName] = dtCur.Rows[r][dc].ToString();
                                break;
                            case "Publisher_Data_Operations.Extensions.FFDocAge":
                                if (int.TryParse(dtCur.Rows[r][dc].ToString(), out int resFF))
                                    newRow[dc.ColumnName] = (FFDocAge)resFF;
                                break;
                            default:

                                newRow[dc.ColumnName] = dtCur.Rows[r][dc].ToString();
                                break;
                        }
                    }
                }

                dt.Rows.Add(newRow);
            }
            dt.AcceptChanges();
            return dt;
        }

        /// <summary>
        /// Load data table from provided XML format string - used to load data from resource file
        /// </summary>
        /// <param name="xml">The XML formatted data string</param>
        /// <param name="tableName">The table source name</param>
        /// <returns>DataTable</returns>
        public static DataTable LoadTestData(string xml, string tableName)
        {
            try
            {
                DataSet ds = new DataSet();
                using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
                    ds.ReadXml(reader);

                if (tableName.IndexOf("Translation", StringComparison.OrdinalIgnoreCase) >= 0)
                    ds.Tables[0].CaseSensitive = true;
                return ds.Tables[0];
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private static Random rand = new Random();

        public static DataTable RandomTable(bool includeRowType = false)
        {

            int cols = rand.Next(2, 20);
            int rows = rand.Next(1, 35);

            DataTable dt = new DataTable($"Random_{rows}_{cols}");
            for (int c = 0; c < cols; c++)
                dt.Columns.Add($"Column_{c + 1}");

            if (includeRowType)
                dt.Columns.Add(Publisher_Data_Operations.Generic.FSMRFP_ROWTYPE_COLUMN);

            for (int r = 0; r < rows; r++)
            {
                DataRow dr = dt.NewRow();
                for (int c = 0; c < cols; c++)
                    dr[c] = RandomString(rand.Next(0, 50));

                if (dt.Columns.Contains(Publisher_Data_Operations.Generic.FSMRFP_ROWTYPE_COLUMN))
                    dr[Publisher_Data_Operations.Generic.FSMRFP_ROWTYPE_COLUMN] = "Level1." + RandomString(10);

                dt.Rows.Add(dr);
            }

            return dt;

        }

        public static string RandomString(int length)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        public static bool TablesEqual(DataTable dt, PDI_DataTable pdi)
        {
            bool isRowType = dt.Columns.Contains(Publisher_Data_Operations.Generic.FSMRFP_ROWTYPE_COLUMN);
            if (dt.Rows.Count != pdi.Rows.Count)
                return false;
            if (dt.Columns.Count != pdi.Columns.Count + (isRowType ? 1 : 0))
                return false;
            if (dt.TableName != pdi.TableName)
                return false;

            for (int row = 0; row < dt.Rows.Count; row++)
            {
                for (int col = 0; col < dt.Columns.Count; col++)
                {
                    if (isRowType && dt.Columns[col].ColumnName == Publisher_Data_Operations.Generic.FSMRFP_ROWTYPE_COLUMN)
                    {
                        if (!((PDI_DataRow)pdi.Rows[row]).ExtendedProperties.ContainsValue(dt.Rows[row][col]))
                            return false;
                    } 
                    else if (dt.Rows[row][col] != pdi.Rows[row][col])
                        return false;
                        
                }
            }

            return true;
        }
    }
    }
