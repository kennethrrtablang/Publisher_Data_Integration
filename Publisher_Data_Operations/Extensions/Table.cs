using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Publisher_Data_Operations.Extensions
{
    public enum TableTypes
    {
        Percent,
        Currency,
        Number,
        Distribution,
        Pivot,
        Date,
        MultiDecimal,
        MultiText,
        MultiPercent,
        MultiYear
    }

    /// <summary>
    /// Stores the details for a single table row item
    /// </summary>
    public class TableItem
    {
        public string EnglishText { get; set; }
        public string FrenchText { get; set; }
        public string ValueText
        {
            get => _value;
            set
            {   // get the parsed decimal value and scale when ValuText set
                _value = value;
                if (decimal.TryParse(value,  out decimal outDec))
                {
                    _valueParsed = outDec;
                    ValueScale = outDec.GetScale();
                }
                    
                else
                {
                    _valueParsed = null;
                    ValueScale = 0; // make 0 the minimum scale instead of -1
                }

            }
        }
        public List<string> EnglishValueList 
        {
            get => _valueListEN;
            set
            {
                if (value != null)
                    ValueScale = value.Max(v => v.GetScale());

                _valueListEN = value;
                _valueParsed = null;
            }     
        }
        public List<string> FrenchValueList
        {
            get => _valueListFR;
            set
            {
                if (value != null)
                    ValueScale = value.Max(v => v.GetScale());

                _valueListFR = value;
                _valueParsed = null;
            }
        }
        public string RowLevel { get; set; }
        public string Row { get; set; }
        public string AllocationType { get; set; }
        public DateTime DistributionDate { get; set; }
        public string MarkDate { get; set; }
        public int ValueScale { get; private set; }
        public decimal? ValueDecimal { get => _valueParsed; }
        public int RowInt
        {
            get
            {
                if (int.TryParse(Row, out int theRow))
                    return theRow;
                return -1;
            }
        }
        public int RowLevelInt
        {
            get
            {
                if (int.TryParse(RowLevel, out int theRowLevel))
                    return theRowLevel;
                return -1;
            }
        }
        private string _value;
        private List<string> _valueListEN;
        private List<string> _valueListFR;
        private decimal? _valueParsed;

        // Create a new TableItem - only value is required
        public TableItem(string value, string english = null, string french = null, string rownumber = null, string rowlevel = null, string allocationOrDate = null)
        {
            ValueText = (value != null) ? value.Trim() : value;
            EnglishText = (english != null) ? english.Trim() : english;
            FrenchText = (french != null) ? french.Trim() : french;
            RowLevel = (rowlevel != null) ? rowlevel.Trim() : rowlevel;
            if (allocationOrDate != null && DateTime.TryParseExact(allocationOrDate.Trim(), "MM/yyyy", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime theDate))
            {
                DistributionDate = theDate;
                MarkDate = string.Empty;
            }
            else
                AllocationType = (allocationOrDate != null) ? allocationOrDate.Trim() : allocationOrDate;

            Row = rownumber;
        }

        public TableItem(List<string> valueListEN, string english = null, string french = null, string rownumber = null)
        {
            EnglishValueList = valueListEN;
            EnglishText = (english != null) ? english.Trim() : english;
            FrenchText = (french != null) ? french.Trim() : french;
            Row = rownumber;
        }

        public TableItem(List<string> valueListEN, List<string> valueListFR, string english = null, string french = null, string rownumber = null)
        {
            EnglishValueList = valueListEN;
            FrenchValueList = valueListFR;
            EnglishText = (english != null) ? english.Trim() : english;
            FrenchText = (french != null) ? french.Trim() : french;
            Row = rownumber;
        }

        public void AddToList(string valueEN, string valueFR = null)
        {
            if (EnglishValueList != null)
                EnglishValueList.Add(valueEN);

            if (FrenchValueList != null && valueFR != null)
                FrenchValueList.Add(valueFR);

            ValueScale = Math.Max(ValueScale, valueEN.GetScale()); // The FP20/EP20 Distribution table was not properly handling the scale when there were no values in the first series

        }
        /// <summary>
        /// Check if the item has a RowLevel
        /// </summary>
        /// <returns>boolean indicated if RowLevel available</returns>
        public bool IsRowLevel()
        {
            if (RowLevelInt >= 0)
                return true;
            return false;
        }

        /// <summary>
        /// Check if the item has an Allocation Type
        /// </summary>
        /// <returns>boolean indicated if AllocationType available</returns>
        public bool IsAllocationType()
        {
            if (!AllocationType.IsNaOrBlank())
                return true;
            return false;
        }

        /// <summary>
        /// Return the provided value formated as a "cell" with markup - If the RowLevel is set then return 2 columns with the value in the first cell if RowLevel = 1 or in the second cell otherwise (this corresponds to the 2nd or 3rd column of the table respectively)
        /// </summary>
        /// <param name="value">The string value to return formated as a cell</param>
        /// <returns>The cell formatted string</returns>
        private string makeCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "<cell />";
            return $"<cell>{value}</cell>";
        }

        /// <summary>
        /// This version of makeCell only expects a scale and an optional append value and will use the contents of the ValueList to create the Distribution table cell contents for all contained series
        /// </summary>
        /// <param name="scale">The number of decimal places to format numbers as</param>
        /// <param name="append">Optional value to add to the end of numbers</param>
        /// <returns>The resulting string</returns>
        private string makeCell(int scale, string append = "", bool getFrench = false)
        {
            string cells = string.Empty;
            List<string> selected = EnglishValueList;
            if (getFrench && FrenchValueList != null && FrenchValueList.Count == EnglishValueList.Count)
                selected = FrenchValueList;
            foreach(string s in selected)
            {
                if (decimal.TryParse(s, out decimal parsed))
                    cells += makeCell(parsed.ToString("N" + scale) + append);
                else
                    cells += makeCell(s);
            }
            return cells;
        }

        private string makeMultiCell(int scale, bool getFrench = false, TableTypes tableType = TableTypes.MultiText)
        {
            string cells = string.Empty;
            List<string> selected = EnglishValueList;
            if (getFrench && FrenchValueList != null && FrenchValueList.Count == EnglishValueList.Count)
                selected = FrenchValueList;
            foreach (string s in selected)
            {
                if (s.ToDate(DateTime.MinValue) != DateTime.MinValue)
                {
                    if (tableType == TableTypes.MultiYear)
                        cells += makeCell(s.ToDate(DateTime.MinValue).Year.ToString());
                    else
                        cells += makeCell(s);
                }
                else if (s.IsNumeric() && s.ReplaceByDictionary(new Dictionary<string, string>(6)
                {
                    { "-", string.Empty},
                    { "–", string.Empty},
                    { "%", string.Empty},
                    { "$", string.Empty},
                    { "#", string.Empty},
                    { "+", string.Empty}
                }, false).Trim().Length != 0) // check date first to make sure we don't have a date - the special case for SOI FSMRFP format will accept ISO dates as numeric - added additional check to not count - as numeric
                {
                    if (tableType == TableTypes.MultiPercent)
                        cells += makeCell(s.ToPercent(getFrench ? "fr-CA" : "en-CA", scale));
                    else
                    {
                        if (s.GetScale() > 0)
                            cells += makeCell(s.ToDecimal(-1, scale));
                        else
                            cells += makeCell(s.ToDecimal(-1, 0)); //ToDecimal with fixedScale 0 = whole number
                    }

                }
                else
                    cells += makeCell(s.Trim('"').Trim('\'')); // remove outer quotes - "N/A" becomes N/A after passing IsNaOrBlank);
            }
            return cells;
        }

        /// <summary>
        /// Return the item value formatted as a cell markup
        /// </summary>
        /// <param name="scale">The number of decimal places</param>
        /// <param name="append">The string to add after the formatted number - % is default</param>
        /// <returns>The value formatted as a string in cell markup</returns>
        public string GetCellValue(int scale, string append = "%")
        {
            string cell = makeCell(ValueDecimal.HasValue ? ((decimal)ValueDecimal).ToString("N" + scale) + append : ValueText);
            if (IsRowLevel())
            {
                if (RowLevelInt == 1)
                    cell += makeCell(string.Empty);
                else
                    cell = makeCell(string.Empty) + cell;
            }
            return cell;
        }

        /// <summary>
        /// Uses enum to determine the type of formatting for the cell value
        /// </summary>
        /// <param name="scale">The number of digits to format the number too</param>
        /// <param name="tableType">The type of values stored in the table</param>
        /// <returns>A formatted cell string</returns>
        public string GetCellValue(int scale, TableTypes tableType, bool getFrench)
        {
            string cell = string.Empty;
            switch (tableType)
            {
                case TableTypes.Percent: // bug14286 - add French formatting for percent values so non-breaking space is included
                    cell = makeCell(ValueDecimal.HasValue ? ValueDecimal.ToPercent(getFrench ? "fr-CA" : "en-CA", scale) : ValueText);
                    break;
                case TableTypes.Pivot:
                    cell = makeCell(ValueDecimal.HasValue ? ((decimal)ValueDecimal).ToString("N" + scale) + "%" : ValueText); //
                    break;
                case TableTypes.Currency:
                    cell = makeCell(ValueText.ToCurrency());
                    break;
                case TableTypes.Number:
                    cell = makeCell(ValueDecimal.HasValue ? ((decimal)ValueDecimal).ToString("N" + scale) : ValueText);
                    break;
                case TableTypes.Distribution:
                case TableTypes.MultiDecimal:
                    cell = makeCell(scale, "", getFrench);
                    break;
                case TableTypes.MultiText:
                case TableTypes.MultiPercent:
                case TableTypes.MultiYear:
                    cell = makeMultiCell(scale, getFrench, tableType);
                    break;
                case TableTypes.Date:
                    cell = makeCell(ValueText);
                    break;
                default:
                    cell = string.Empty;
                    break;
            }

            if (IsRowLevel() && tableType != TableTypes.Number)
            {
                if (IsAllocationType())
                {
                    cell += makeCell(RowLevel);
                }
                else
                {
                    if (RowLevelInt == 1)
                        cell += makeCell(string.Empty);
                    else
                        cell = makeCell(string.Empty) + cell;
                }
                
            }
            
            return cell;

        }

        /// <summary>
        /// Return the MarkDate value as a cell or a blank cell if the cell has a value or an empty string if the MarkDate is null
        /// </summary>
        /// <returns>string containing an xml cell with "Y" or blank</returns>
        internal object GetCellMark()
        {
            return (MarkDate != null) ? makeCell(MarkDate) : string.Empty;
        }

        /// <summary>
        /// Return the item Text formatted as a cell markup
        /// </summary>
        /// <param name="getFrench">Bool indicating if French should be returned - default English</param>
        /// <returns>The English or French item text in cell markup</returns>
        public string GetCellText(TableTypes tableType, bool getFrench = false)
        {
            if (tableType == TableTypes.MultiText || tableType == TableTypes.MultiDecimal || tableType == TableTypes.MultiPercent || tableType == TableTypes.MultiYear) // multi tables don't use the English or French text as the first column
                return string.Empty;

            if (getFrench)
                return makeCell(FrenchText);
            else
                return makeCell(EnglishText);

        }

        /// <summary>
        /// Return the text value for a cell - if shortMonths are available use the Distribution date to create a text label, otherwise use the regular GetCellText
        /// </summary>
        /// <param name="shortMonths">The dictionary of short month text</param>
        /// <param name="getFrench">Boolean indicating if the French language results should be returned</param>
        /// <returns></returns>
        public string GetCellText(Dictionary<string, string[]> shortMonths, TableTypes tableType, bool getFrench = false)
        {
            if (shortMonths != null)
            {
                string code = "SF" + DistributionDate.Month.ToString("0#"); // 20210902 - John Gilhuly - New date format MM/YY instead of short months and 4 digit year - back to MMM yyyy
                if (getFrench)
                    return makeCell(shortMonths[code][1] + " " + DistributionDate.Year.ToString()); // DistributionDate.ToString("MM/yy") //DistributionDate.ToString("MMM yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("fr-CA"))
                else
                    return makeCell(shortMonths[code][0] + " " + DistributionDate.Year.ToString()); // DistributionDate.ToString("MM/yy") //DistributionDate.ToString("MMM yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("en-CA"))
            }
            else
                return GetCellText(tableType, getFrench);
        }
    }

    /// <summary>
    /// A list of TableItems
    /// </summary>
    public class TableList : List<TableItem>
    {
        //When FilingYear is set a table formatted for 22b will be returned - Years and percentages
        int FilingYear { get; set; }
        public TableTypes TableType { get; set; }
        public Dictionary<string, string[]> ShortMonths { get; set; }
        public string NAValue { get; set; }

        public TableList()
        {
            FilingYear = -1;
            TableType = TableTypes.Percent;
        }

        public TableList(int filingYear)
        {
            FilingYear = filingYear;
            TableType = TableTypes.Percent;
        }

        public TableList(int filingYear, string naValue)
        {
            FilingYear = filingYear;
            TableType = TableTypes.Pivot;
            NAValue = naValue;
        }

        public TableList(TableTypes valueType)
        {
            FilingYear = -1;
            TableType = valueType;
        }

        public TableList(string naValue)
        {
            TableType = TableTypes.Distribution;
            NAValue = naValue;
        }

        /// <summary>
        /// In order to prevent creating blank/default TableItems in the TableList this first checks if value is IsNaOrBlank before creating and adding a TableItem
        /// </summary>
        /// <param name="value"></param>
        /// <param name="english"></param>
        /// <param name="french"></param>
        /// <param name="rowlevel"></param>
        /// <param name="rownumber"></param>
        /// <returns>If a TableItem was added</returns>
        public bool AddValidation(string value, string english = null, string french = null, string rownumber = null, string rowlevel = null, string allocation = null)
        {
            if (TableType == TableTypes.Distribution)
            {
                string vConverted = value.IsNaOrBlank() ? NAValue : value;
                TableItem cur = this.Find(v => v.EnglishText == english && v.Row == rownumber);
                if (cur != null)
                    cur.AddToList(vConverted);
                else
                    Add(new TableItem(new List<string>() { vConverted }, english, french, rownumber));
            }
            else if (TableType == TableTypes.Pivot)
                Add(new TableItem(value.IsNaOrBlank() ? NAValue : value, english, french, rownumber, rowlevel, allocation));
            else
            {
                if (value.IsNaOrBlank())
                    return false;
                else
                    Add(new TableItem(value, english, french, rownumber, rowlevel, allocation));

            }
            return true;
        }

        public bool AddValidationDistrib(string valueEN, string valueFR, string english = null, string french = null, string rownumber = null)
        {
            if (TableType == TableTypes.Distribution)
            {
                string vEN = valueEN.IsNaOrBlank() ? NAValue : valueEN;
                string vFR = valueFR.IsNaOrBlank() ? NAValue : valueFR;
                TableItem cur = this.Find(v => v.EnglishText == english && v.Row == rownumber);
                if (cur != null)
                    cur.AddToList(vEN, vFR);
                else
                    Add(new TableItem(new List<string>() { vEN }, new List<string>() { vFR }, english, french, rownumber));
            }
            return true;
        }

        public void AddMultiCell(string rownumber, string valueEN, string valueFR, string english = null, string french = null)
        {
            string vEN = valueEN.IsNaOrBlank() ? NAValue : valueEN;
            string vFR = valueFR.IsNaOrBlank() ? NAValue : valueFR;
            TableItem cur = this.Find(v => v.EnglishText == english && v.Row == rownumber);
            if (cur != null)
                cur.AddToList(vEN, vFR);
            else
                Add(new TableItem(new List<string>() { vEN }, new List<string>() { vFR }, english, french, rownumber));
        }

        /// <summary>
        /// If there are TableItems check what the maximum scale value of all of them is
        /// </summary>
        /// <returns>The scale value found through check or 2 if the max scale is higher</returns>
        public int GetMaxScale()
        {
            if (this.Count > 0)
                return Math.Min(this.Max(v => v.ValueScale), (TableType == TableTypes.Distribution) ? 3 : (TableType == TableTypes.MultiText) ? 4 : 2); //Max scale is 0 to 2 - For Distribution it is 3 and MultiText is 4
            return 0;
        }

        /// <summary>
        /// Mark a valid axis label by finding the indicated date and setting the MarkDate string to mark - default of "Y"
        /// </summary>
        /// <param name="date">The DistributionDate to find</param>
        /// <param name="mark">Optional string to mark the TableItem as an Axis label</param>
        /// <returns></returns>
        public bool MarkByDate(DateTime date, string mark = "Y")
        {
            for (int i = 0; i < this.Count(); i++)
            {
                if (this[i].DistributionDate == date)
                {
                    this[i].MarkDate = mark;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// When used for 10K tables mark up to 10 rows as axis labels
        /// </summary>
        /// <returns>True if complete or false if already completed or not needed</returns>
        public bool MarkDates()
        {
            // don't mark dates if the type of table doesn't require it
            if (TableType != TableTypes.Currency || this.Count == 0)  //|| this[0].DistributionDate == null
                return false;

            // check if dates have already been marked
            if (this[0].MarkDate == "Y")
                return true;

            // there is a max of 10 axis titles available in Publisher - 1 will be the first value and 10 will be the last value leaving up to 8 additional available - other than first and last only values in the month of December are returned (yearly)
            this[0].MarkDate = "Y";
            if (this.Count > 1)
                this[this.Count - 1].MarkDate = "Y";

            if (this.Count > 2)
            {
                DateTime min = this[0].DistributionDate;
                DateTime max = this[this.Count - 1].DistributionDate;

                var diff = max - min;
                double range = diff.TotalDays / Generic.DAYSPERYEAR; // determine the number of years (since we are normally displaying one label per year)
                double gap = range / 9.0; // divide across the 10 possible axis labels - there are 9 gaps between the 10 labels

                var result = this.Where(v => v.DistributionDate.Month == 12).ToList();

                int first;
                for (first = 0; first < result.Count(); first++)
                {
                    if ((result[first].DistributionDate - this[0].DistributionDate).TotalDays / Generic.DAYSPERYEAR / gap > 0.7)
                        break;
                }
                for ( int i = first; i < result.Count(); i+=(int)Math.Ceiling(gap))
                {
                        if ((this[this.Count - 1].DistributionDate - result[i].DistributionDate).TotalDays / Generic.DAYSPERYEAR / gap > 0.7)
                            MarkByDate(result[i].DistributionDate);  
                }
            }
            return true;
        }

        /// <summary>
        /// Builds a 22b table using filing year and returns the number of calendarYears (=TableList.Count) and the number of negative calendar years
        /// </summary>
        /// <param name="calendarYears">Out variable to return the number of calendar years</param>
        /// <param name="negativeYears">Out variable to return the number of negative years</param>
        /// <returns>A markup language table of the TableList data</returns>
        public string GetTableString(out int calendarYears, out int negativeYears)
        {
            negativeYears = 0;
            calendarYears = Count;
            int maxScale = GetMaxScale();
            int curYear = FilingYear - calendarYears;
            
            StringBuilder tableString = new StringBuilder("<table>");
            if (FilingYear > 0)
            {
                
                for (int i = 0; i < Count; i++)
                {
                    if (this[i].ValueDecimal < 0)
                        negativeYears++;

                    tableString.Append($"<row><cell>{curYear}</cell>{this[i].GetCellValue(maxScale)}</row>");
                    curYear++;
                }
            }
            return tableString.Append("</table>").ToString(); //.Replace("&", "&amp;")
        }

        public string GetTablePivotString()
        {
            int maxScale = Math.Max(GetMaxScale(), 1);
            int curYear = FilingYear - Count;

            StringBuilder tableString = new StringBuilder("<table>");
            if (FilingYear > 0)
            {
                tableString.Append("<row>");
                for (int i = 0; i < Count; i++)
                {
                    tableString.Append($"<cell>{curYear}</cell>");
                    curYear++;
                }
                tableString.Append("</row><row>");
                for (int i = 0; i < Count; i++)
                    tableString.Append(this[i].GetCellValue(maxScale, ""));
            }
            return tableString.Append("</row></table>").ToString(); //.Replace("&", "&amp;")

        }
        /// <summary>
        /// Builds a 16, 17 or 40 (and other) style table in either English or French - 16 vs 17/40 style is determined by the presence or absence of RowLevel
        /// </summary>
        /// <param name="getFrench">Indicates if French language should be returned</param>
        /// <returns>A markup language table of the TableList data</returns>
        public string GetTableString(bool getFrench = false) //Dictionary<string, string> englishFrenchTextList, string[] valueList)brandNewFund
        {
            int maxScale = GetMaxScale();
            StringBuilder tableString = new StringBuilder("<table>");

            MarkDates();
            //this.Sort((x, y) => x.RowInt.CompareTo(y.RowInt));

            // if the table type is Number (of investments) we need to sort by the row level and then the row as the incoming data is out of order for display
            if (this.TableType == TableTypes.Number)
                this.Sort((x, y) => x.RowLevelInt == y.RowLevelInt ? x.RowInt.CompareTo(y.RowInt) : x.RowLevelInt.CompareTo(y.RowLevelInt));

            for (int i = 0; i < Count; i++)
                tableString.Append($"<row>{this[i].GetCellText(ShortMonths, TableType, getFrench)}{this[i].GetCellValue(maxScale, TableType, getFrench)}{this[i].GetCellMark()}</row>");
            
            return tableString.Append("</table>").ToString(); //.Replace("&", "&amp;")
        }
        
        /// <summary>
        /// Helper function to avoid forgetting the bool on GetTableString
        /// </summary>
        /// <returns></returns>
        public string GetTableStringFrench()
        {
            return GetTableString(true);
        }
    }
}
