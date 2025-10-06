using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Hosting;

namespace Publisher_Data_Operations.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Converts text values of 'Y' or strings beginning with 'Y" or the number "1" to bool true or returns false for anything else
        /// </summary>
        /// <param name="value">The string value to convert</param>
        /// <returns></returns>
        public static bool ToBool(this string value)
        {
            if (value is null)
                return false;
            else
            {
                value = value.Trim();
                if (value.StartsWith("Y", StringComparison.OrdinalIgnoreCase))
                    return true;
                else if (value == "1")
                    return true;
                else if (value.Equals("True", StringComparison.OrdinalIgnoreCase))
                    return true;
                else if (bool.TryParse(value, out bool val))
                    return val;
            }
            return false;
        }

        /// <summary>
        /// If the value contains an N/A or is blank or null return true otherwise return false
        /// </summary>
        /// <param name="value">the string to check</param>
        /// <returns></returns>
        public static bool IsNaOrBlank(this string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase)) //switch to built in convenience function
                return true;
            return false;
        }

        public static bool IsNotNaOrBlank(this string value)
        {
            if (value.Trim().Equals("!N/A", StringComparison.OrdinalIgnoreCase)) // Specific !N/A added for scenario to match anything but N/A
                return true;
            return false;
        }

        public static bool IsNa(this string value)
        {
            if (value.Trim().Equals("N/A", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        public static bool IsReq(this string value)
        {
            if (value.Trim().Equals("Req", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }

        [Obsolete("Use IsNaOrBlank for the boolean value")]
        public static bool NaOrBlank(this string value)
        {
            return value.IsNaOrBlank();
        }

        public static string NaOrBlankNull(this string value)
        {
            if (!value.IsNaOrBlank())
                return value.Trim();
            return null;
        }

        public static string RemoveBoundingMarkup(this string value)
        {
            if (value != null && value.Length > 0 && value.IndexOf('<') == 0 && value.Contains('>'))
            {
                string tag = value.Substring(1, value.IndexOf('>') - 1);
                if (tag.Length > 0 && value.EndsWith($"</{tag}>", StringComparison.OrdinalIgnoreCase))
                {
                    string temp = value.Substring(value.IndexOf('>') + 1, value.Length - $"<{tag}></{tag}>".Length);
                    if (temp.IsValidXML())
                        return temp;
                }

                // if the string starts and ends with the same tag remove it.
            }
            return value;
        }

        public static string RemoveMarkup(this string value, string tag = null)
        {
            try
            {
                if (tag != null && tag.Length > 0)
                    return Regex.Replace(value, @"</?(?i:" + Regex.Escape(tag) + @"\b)(.|\n)*?>", string.Empty); // strip specific HTML tags
                else
                { // strip the HTML tags to start
                    return Regex.Replace(value, @"</?[a-zA-Z][^>]*>", String.Empty);
                }         
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return value;


        }
        /// <summary>
        /// Strip all HTML style tags and check if what is left is a number - numbers can contain - - + . ( ) , – $ # % (both minus and dash included) (, ) included for 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool IsNumeric(this string value)
        {
            if (value is null)
                return false;
            value = value.RemoveMarkup();
            if (value.Length == 0)
                return false;
            //string temp = Regex.Escape(",.()-+–$#%"); //temp = "\\(\\)-\\+–\\$\\#%"
            Match m = Regex.Match(value, @"^[0-9,.\(\)--–%\+\$\#(,\s?)]*$"); // find a value that is only a number
            if (m is null) // something went wrong? false
                return false;
            if (m.Captures.Count != 1) // less or more than one capture
                return false;
            if (value.Replace(m.Captures[0].Value, string.Empty).Length > 0) // something left in the value after removing the number
                return false;
            return true;
        }

        public static bool IsValidXML(this string value, bool addRoot = true)
        {
            if (ParameterValidator.IsValidXMLStatic(value, addRoot) == true.ToString())
                return true;

            return false;
        }

        /// <summary>
        /// Check the string to see if it has any XML elements - essentially if it has right angle brackets (possibly after replacement) then we return true
        /// </summary>
        /// <param name="test"></param>
        /// <returns></returns>
        public static bool ContainsXML(this string value)
        {
            return value.Contains('<') && value.Contains('>');
        }

        /// <summary>
        /// Convert a date from a fixed string format to a DateTime
        /// </summary>
        /// <param name="value">the date as a string</param>
        /// /// <param name="nullValue">the value to return if an invalid value is passed</param>
        /// <returns></returns>
        public static DateTime ToDate(this string value, DateTime nullValue)
        {
            DateTime theDate = nullValue;
            if (value.IsNaOrBlank())
                return nullValue;

            if (DateTime.TryParseExact(value, new string[] { "dd/MM/yyyy", "d/MM/yyyy", "yyyy-MM-dd" }, System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out theDate)) //ISO 8601 format added
                return theDate;

            return nullValue;
        }

        public static DateTime ToDateUS(this string value, DateTime nullValue)
        {
            DateTime theDate = nullValue;
            if (value.IsNaOrBlank())
                return nullValue;

            if (DateTime.TryParseExact(value, new string[] { "M/d/yyyy","yyyy-MM-dd" }, System.Globalization.CultureInfo.InvariantCulture, DateTimeStyles.None, out theDate)) //ISO 8601 format added
                return theDate;

            return nullValue;
        }

        public static bool IsDate(this string value)
        {
            if (value.ToDate(DateTime.MinValue) != DateTime.MinValue)
                return true;

            return false;
        }
        public static bool ContainsWords(this string check, string[] elementList)
        {
            return Regex.Match(check, @"\b(" + Regex.Escape(string.Join("~~~", elementList)).Replace("~~~", "|") + @")\b", RegexOptions.IgnoreCase).Success;
        }
        /// <summary>
        /// Replace text in a string without needing the input and search case to match.
        /// https://stackoverflow.com/questions/6275980/string-replace-ignoring-case
        /// </summary>
        /// <param name="input">The string</param>
        /// <param name="search">Find in input</param>
        /// <param name="replacement">replace search</param>
        /// <returns></returns>
        public static string ReplaceCI(this string input, string search, string replacement)
        {
            if (replacement is null)
                return input;

            string result = Regex.Replace(
                input,
                Regex.Escape(search),
                replacement.Replace("$", "$$"),
                RegexOptions.IgnoreCase
            );
            return result;
        }

        /// Modified based on http://metadeveloper.blogspot.com/2008/06/regex-replace-multiple-strings-in.html
        /// <summary>
        /// Replace all the keys (surrounded with <>) found in the string with the value - uses case insensitive Regex for replacement
        /// </summary>
        /// <param name="input">The string to perform replacement on</param>
        /// <param name="replace">The dictionary containing key/value pairs to replace</param>
        /// <returns>The string after replacement</returns>
        public static string ReplaceByDictionary(this string input, Dictionary<string, string> replace, bool appendBrackets = true)
        {
            /*
            foreach (KeyValuePair<string, string> keyVal in replace)
                input = input.ReplaceCI("<" + keyVal.Key + ">", keyVal.Value);
            return input;
            */
            if (replace != null && replace.Keys.Count > 0)
            {
                Dictionary<string, string> modReplacements = new Dictionary<string, string>(replace.Count);
                foreach (KeyValuePair<string, string> keyVal in replace)
                    modReplacements.Add(Regex.Escape(appendBrackets ? "<" + keyVal.Key.ToUpper() + ">" : keyVal.Key.ToUpper()), keyVal.Value is null ? null : Regex.Escape(keyVal.Value));

                return Regex.Unescape(Regex.Replace(input,
                                    "(" + String.Join("|", modReplacements.Keys.ToArray()) + ")",
                                    delegate (Match m) { return modReplacements[Regex.Escape(m.Value.ToUpper())]; }
                                    , RegexOptions.IgnoreCase));
            }
            return input;
        }

        /// <summary>
        /// Handles string dates for AgeInCalendarYears function
        /// </summary>
        /// <param name="dataAsDate">AsAtData</param>
        /// <param name="inceptionDate">Inception or FirstOffering</param>
        /// <returns></returns>
        public static int AgeInCalendarYears(this string dataAsDate, string inceptionDate)
        {
            return AgeInCalendarYears(dataAsDate.ToDate(DateTime.MinValue), inceptionDate.ToDate(DateTime.MaxValue));
        }

        public static int AgeInYearsBNY(this string dataAsDate, string inceptionDate)
        {
            DateTime asAt = dataAsDate.ToDateUS(DateTime.MinValue);
            DateTime inception = inceptionDate.ToDateUS(DateTime.MaxValue);

            Double years = Math.Min(asAt.DaysDiff(inception) / Generic.DAYSPERYEAR , 10);
            return (int)Math.Floor(years);
        }
        /// <summary>
        /// Calculate the age of a security in calendar years between the Data As Date and Inception date
        /// </summary>
        /// <param name="asDate">DateTime AsAtData</param>
        /// <param name="incDate">DateTime Inception or FirstOffering</param>
        /// <returns></returns>
        public static int AgeInCalendarYears(this DateTime asDate, DateTime incDate)
        {
            if (!(incDate.Month == 1 && incDate.Day == 1))
                incDate = incDate.AddYears(1);

            if (asDate.Month == 12 && asDate.Day == 31)
                asDate = asDate.AddYears(1);

            if (asDate.Year - incDate.Year > 0)
                return Math.Min((asDate.Year - incDate.Year), 10);
            else
                return 0;

        }

        /// <summary>
        /// Interface to FilingYear allowing string formatted dates
        /// </summary>
        /// <param name="filingDate">The filing date in string format</param>
        /// <returns>The filing year as an int</returns>
        public static int FilingYear(this string filingDate)
        {
            return FilingYear(filingDate.ToDate(DateTime.MinValue));
        }

        /// <summary>
        /// Determine the filing year based on the provided filing date - if the day is the last day of the year then return the next year
        /// </summary>
        /// <param name="filingDate">the filing data DateTime</param>
        /// <returns>the filing year as an int</returns>
        public static int FilingYear(this DateTime filingDate)
        {
            if (filingDate.Month == 12 && filingDate.Day == 31)
                filingDate = filingDate.AddYears(1);

            return filingDate.Year;
        }

        /// <summary>
        /// Convert a string to it's equivalent currency value using the appropriate culture
        /// Note that spaces are converted to non breaking spaces for consistency
        /// </summary>
        /// <param name="amount">The string to convert to a currency</param>
        /// <param name="culture">The culture code for the conversion - defaults to en-CA</param>
        /// <param name="pattern">The opptional pattern for the string conversion - default without decimals</param>
        /// <returns>The formatted currency string</returns>
        public static string ToCurrency(this string amount, string culture = "en-CA", string pattern = "C0")
        {
            NumberFormatInfo nfi = CultureInfo.CreateSpecificCulture(culture).NumberFormat;     // use a cultural number format so the percentage decimal precision can be set.
            if (culture == "fr-CA")
                nfi.CurrencyNegativePattern = 15; // For some reason the pattern changed from 15 to 8 in .Net 6/7 - https://learn.microsoft.com/en-us/dotnet/api/system.globalization.numberformatinfo.currencynegativepattern?view=net-7.0 - https://github.com/dotnet/runtime/issues/70789
            if (int.TryParse(amount, out int parsed))
                return parsed.ToString(pattern, nfi).Replace(' ', '\u00A0'); //use non breaking spaces
            else if (double.TryParse(amount, out double parsedDouble))
                return parsedDouble.ToString(pattern, nfi).Replace(' ', '\u00A0'); //use non breaking spaces
            return string.Empty;
        }

        /// <summary>
        /// Use the existing ToCurrency by passing a pattern indicating 2 decimal places
        /// </summary>
        /// <param name="amount">The string to convert to a currency</param>
        /// <param name="culture">The culture code for the conversion - defaults to en-CA</param>
        /// <returns>The formatted currency string</returns>
        public static string ToCurrencyDecimal(this string amount, string culture = "en-CA")
        {
            return amount.ToCurrency(culture, "C2");
        }

        /// <summary>
        /// Determine the number of digits after the decimal point (scale) using the SqlDecimal type
        /// </summary>
        /// <param name="check">The decimal number</param>
        /// <returns>The number of digits after the decimal point</returns>
        public static int GetScale(this decimal check)
        {
            System.Data.SqlTypes.SqlDecimal temp = new System.Data.SqlTypes.SqlDecimal(check); //a bit odd but the SqlDecimal type is the easiest/fastest way to determine the scale (and precision) of a decimal
            return temp.Scale; //return the number of digits to the scale determined by the SqlDecimal type
        }

        /// <summary>
        /// Determine the number of digits after the decimal point (scale) of a string value
        /// </summary>
        /// <param name="value">The string value to check</param>
        /// <returns>Number of digits after the decimal</returns>
        public static int GetScale(this string value)
        {
            if (decimal.TryParse(value, out decimal parsed))    //decimal maintains trailing zeros
                return GetScale(parsed);

            return 0; // return a 0 for the scale instead of -1 when the scale can't be determined
        }

        /// <summary>
        /// Converts a string to it's equivalent percentage assuming that the value is already a percentage (not a decimal representation of a percentage)
        /// Note that spaces are converted to non breaking spaces for consistency
        /// </summary>
        /// <param name="value">The string to display as a percentage</param>
        /// <param name="culture">The culture code for the conversion - defaults to en-CA</param>
        /// <returns>The formatted percentage string</returns>
        public static string ToPercent(this string value, string culture = "en-CA", int scale = -1)
        {
            if (decimal.TryParse(value, out decimal parsed))    //decimal maintains trailing zeros
            {
                NumberFormatInfo nfi = CultureInfo.CreateSpecificCulture(culture).NumberFormat;     // use a cultural number format so the percentage decimal precision can be set.
                nfi.PercentDecimalDigits = scale == -1 ? GetScale(parsed) : scale;  //set the number of digits to the scale determined by GetScale
                if (culture == "en-CA" || culture == "en-US")
                    nfi.PercentPositivePattern = 1; // force the percent positive pattern to 1 as it is supposed to be 0 for English CA and US
                nfi.PercentNegativePattern = nfi.PercentPositivePattern; // make the positive and negative match

                return (parsed / 100).ToString("P", nfi).Replace(' ', '\u00A0'); //the percent format assumes that the number is a percentage expresses as a decimal where we are using a percent that is missing the symbol /100 to compensate - also uses non breaking spaces
            }
            return string.Empty;
        }
        public static string ToPercent(this decimal? parsed, string culture = "en-CA", int scale = -1)
        {
            if (parsed.HasValue)
                return ((decimal)parsed).ToPercent(culture, scale);

            return string.Empty;
        }

        public static string ToPercent(this decimal parsed, string culture = "en-CA", int scale = -1)
        {
            NumberFormatInfo nfi = CultureInfo.CreateSpecificCulture(culture).NumberFormat;     // use a cultural number format so the percentage decimal precision can be set.
            nfi.PercentDecimalDigits = scale == -1 ? GetScale(parsed) : scale;  //set the number of digits to the scale determined by GetScale
            if (culture == "en-CA" || culture == "en-US")
                nfi.PercentPositivePattern = 1; // force the percent positive pattern to 1 as it is supposed to be 0 for English CA and US
            nfi.PercentNegativePattern = nfi.PercentPositivePattern; // make the positive and negative match

            return (parsed / 100).ToString("P", nfi).Replace(' ', '\u00A0'); //the percent format assumes that the number is a percentage expresses as a decimal where we are using a percent that is missing the symbol /100 to compensate - also uses non breaking spaces
        }

        public static string ToDecimal(this string value, string culture)
        {
            return value.ToDecimal(-1, -1, culture);
        }

        /// <summary>
        /// Convert string value to decimal value with either a fixed or minimum scale
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minScale"></param>
        /// <param name="fixedScale"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static string ToDecimal(this string value, int minScale = -1, int fixedScale = -1, string culture = "en-CA")
        {
            if (decimal.TryParse(value, out decimal parsed) && culture != null)    //decimal maintains trailing zeros
            {
                return parsed.ToDecimal(minScale, fixedScale, culture);
            }
            return null;
        }

        public static string ToDecimal(this decimal? parsed, int minScale = -1, int fixedScale = -1, string culture = "en-CA")
        {
            if (parsed.HasValue)
                return ((decimal)parsed).ToDecimal(minScale, fixedScale, culture);

            return string.Empty;
        }

        /// <summary>
        /// Seperate ToDecimal conversion for decimal values
        /// </summary>
        /// <param name="parsed"></param>
        /// <param name="minScale"></param>
        /// <param name="fixedScale"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static string ToDecimal(this decimal parsed, int minScale = -1, int fixedScale = -1, string culture = "en-CA")
        {
            NumberFormatInfo nfi = CultureInfo.CreateSpecificCulture(culture).NumberFormat;     // use a cultural number format so the percentage decimal precision can be set.
            if (fixedScale < 0)
            {
                if (minScale < 0)
                    nfi.NumberDecimalDigits = GetScale(parsed);  //set the number of digits to the scale determined by GetScale
                else
                    nfi.NumberDecimalDigits = Math.Max(minScale, GetScale(parsed));
            }
            else
                nfi.NumberDecimalDigits = fixedScale; //else we have a fixed number for the scale

            return parsed.ToString("N", nfi).Replace(' ', '\u00A0'); //also uses non breaking spaces
        }



        /// <summary>
        /// convert to decimal value 
        /// </summary>
        /// <param name="value">string value</param>
        /// <returns>decimal value</returns>
        public static string ToMinimumOneDecimal(this string value)
        {
            /*
            if (value.Trim().IndexOf('.', 0) == -1)
            {
                return value.Trim() + ".0";
            }
            return value;*/

            return value.ToDecimal(1);

        }
        public static double DaysDiff(this DateTime first, DateTime second)
        {
            return Math.Abs((first - second).TotalDays);
        }

        public static bool ConsecutiveYears(this DateTime first, DateTime second, int years)
        {
            if (first != DateTime.MaxValue && first != DateTime.MinValue && second != DateTime.MaxValue && second != DateTime.MinValue)
            {
                if (first.DaysDiff(second) >= (years * Generic.DAYSPERYEAR))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Calculate if the number of consecutive years between two dates meets or exceeds the provided number of years 
        /// </summary>
        /// <param name="dateStart">The start date as a string</param>
        /// <param name="dateEnd">The end date as a string</param>
        /// <param name="months">The number of months to check</param>
        /// <returns>Bool indicating if the number of years was equal or greater than the passed amount</returns>
        public static bool ConsecutiveYears(this string dateStart, string dateEnd, int years)
        {
            DateTime start = dateStart.ToDate(DateTime.MaxValue);
            DateTime end = dateEnd.ToDate(DateTime.MinValue);

            return start.ConsecutiveYears(end, years);
        }

        /// <summary>
        /// Taking two strings containing dates convert them and return a bool indicating if the year is the same
        /// </summary>
        /// <param name="mainDate">A date as a string</param>
        /// <param name="otherDate">Another date as a string</param>
        /// <returns>Bool indicating if year is the same</returns>
        public static bool YearEqual(this string mainDate, string otherDate)
        {
            DateTime date1 = mainDate.ToDate(DateTime.MaxValue);
            DateTime date2 = otherDate.ToDate(DateTime.MinValue);

            if (date1.Year == date2.Year)
                return true;
            return false;
        }

        /// <summary>
        /// Convert value which may be a year or day value into a year value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isDays"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static string ToYears(this string value, bool isDays, string culture = "en-CA")
        {
            if (decimal.TryParse(value, out decimal parsed) && culture != null)
            {
                NumberFormatInfo nfi = CultureInfo.CreateSpecificCulture(culture).NumberFormat;
                nfi.NumberDecimalDigits = Math.Min(GetScale(parsed), 2);

                if (isDays)
                {
                    parsed = parsed / (decimal)Generic.DAYSPERYEAR;
                    nfi.NumberDecimalDigits = Math.Min(GetScale(parsed), 2);
                }
                return parsed.ToString("N", nfi);
            }
            return string.Empty;
        }

        /// <summary>
        /// Convert a value which may be a year or day value into a day value
        /// </summary>
        /// <param name="value"></param>
        /// <param name="isDays"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static string ToDays(this string value, bool isDays, string culture = "en-CA")
        {

            if (decimal.TryParse(value, out decimal parsed) && culture != null)
            {
                NumberFormatInfo nfi = CultureInfo.CreateSpecificCulture(culture).NumberFormat;
                nfi.NumberDecimalDigits = GetScale(parsed);

                if (!isDays)
                {
                    nfi.NumberDecimalDigits = 0;
                    parsed = parsed * (decimal)Generic.DAYSPERYEAR;
                }
                return parsed.ToString("N", nfi);
            }
            return string.Empty;
        }

        public static string ToMaxLength(this string value, int maxLength)
        {
            if (value.IsNaOrBlank()) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        public static int CountOccurances(this string value, string find)
        {
            int count = 0;
            int a = 0;
            while ((a = value.IndexOf(find, a)) != -1)
            {
                a += find.Length;
                count++;
            }
            return count;
        }

        /// <summary>
        /// Escape single quotes in SQL text
        /// </summary>
        /// <param name="value">The SQL string</param>
        /// <returns>The escaped SQL string</returns>
        public static string EscapeSQL(this string value)
        {
            return value.Replace("'", "''");
        }

        //public static string CleanXML(this string value)
        //{
        //    Dictionary<string, string> replacements = new Dictionary<string, string>() { { "&", "&amp;" }, { "<", "&lt;" }, { ">", "&gt;" }, { "\"", "&quot;" }, { "'", "&#39;" }};
        //    Dictionary<string, string> fixes = new Dictionary<string, string>() { { "&lt;table&gt;", "<table>" }, { "&lt;/table&gt;", "</table>" }, { "&lt;row&gt;", "<row>" }, { "&lt;/row&gt;", "</row>" }, { "&lt;cell&gt;", "<cell>" }, { "&lt;/cell&gt;", "</cell>" } };
        //    return value.ReplaceByDictionary(replacements, false).ReplaceByDictionary(fixes, false);

        //}

        public static string RemoveHTML(this string value)
        {
            return Regex.Replace(value, "<.+?>", string.Empty); //<[^>]*>
        }

        public static string StripTagsExceptAlpha(this string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside && Char.IsLetter(let))
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex).ToLower();
        }

        public static string RemoveExceptAlpha(this string value)
        {
            return string.Concat(value.Where(c => Char.IsLetter(c))).ToLower();
        }

        public static string RemoveExceptAlphaNumeric(this string value)
        {
            return string.Concat(value.Where(c => Char.IsLetter(c) || Char.IsDigit(c))).ToLower();
        }

        public static string RestoreAngleBrackets(this string value)
        {
            return value.Replace("&lt;", "<").Replace("&gt;", ">");
        }

        public static string CleanXML(this string rawXML)
        {
            string temp = Regex.Replace(rawXML, @"\&(?![a-zA-Z#\d]+;)", "&amp;"); // use regex to replace only when the & is not already escaped or used in an escape// rawXML.Replace("&", "&amp;"); // always do this one first
            //string temp = rawXML.Replace("&", "&amp;");
            temp = temp.Replace("<nbh>", "&#8209;"); // imports might contain the non-breaking hyphen
            temp = temp.Replace("<nbs>", "&#160;"); // imports might contain the non-breaking space (this is the XML version)
            temp = temp.Replace("&nbsp;", "&#160;"); // convert to XML version
            temp = temp.ReplaceCI("<br>", "<br />");
            temp = temp.ReplaceCI("< br/>", "<br />");
            temp = temp.ReplaceCI("<br  />", "<br />");
            temp = temp.ReplaceCI("</br>", "<br />");
            //temp = temp.Replace(Environment.NewLine, "<br />");
            //temp = temp.Replace("\n", "<br />"); //look for any remaining line feeds
            temp = temp.Replace("<", "&lt;"); // replace all the remaining < and > 
            temp = temp.Replace(">", "&gt;");
            temp = temp.Replace("\"", "&quot;");
            temp = temp.Replace("'", "&#39;");

            //(CONTENT,'&', '&amp;'),'<', '&lt;'),'>', '&gt;'),'"', '&quot;'),'''', '&#39;') W
            return temp;
        }

        public static string ExcelTextClean(this string value)
        {
            string temp = value.ReplaceCI("<br>", "<br />");
            temp = Regex.Replace(temp, @"\&(?![a-zA-Z#\d]+;)", "&amp;");
            temp = temp.ReplaceCI("< br/>", "<br />");
            temp = temp.ReplaceCI("<br  />", "<br />");
            temp = temp.ReplaceCI("</br>", "<br />");
            temp = temp.Replace(Environment.NewLine, "<br />");
            temp = temp.Replace("\n", "<br />"); //look for any remaining line feeds
            temp = temp.Replace("<nbh>", "-"); // imports might contain the non-breaking hyphen // 20211028 - TinyMCE/Composition doesn't support &#8209;
            temp = temp.Replace("<nbs>", "&#160;"); // imports might contain the non-breaking space (this is the XML version)
            temp = temp.Replace("&nbsp;", "&#160;"); // &nbsp; will fail XML validation - change it to the valid XML version as the cleaner will handle this on export
            return temp;
        }

        public static string IncomingHTMLtoXML(this string rawHTML)
        {
            string temp = rawHTML.Replace("& ", "&amp; ");
            temp = temp.Replace("<nbh>", "-"); // imports might contain the non-breaking hyphen // 20211028 - TinyMCE/Composition doesn't support &#8209;
            temp = temp.Replace("<nbs>", "&#160;"); // imports might contain the non-breaking space
            //temp = temp.Replace("<", "&lt;");
            //temp = temp.Replace(">", "&gt;");
            temp = temp.Replace("\"", "&quot;");
            temp = temp.Replace("'", "&#39;");
            temp = temp.Replace("&nbsp;", "&#160;");
            temp = temp.ReplaceCI("<br>", "<br />");
            temp = temp.Replace(Environment.NewLine, "<br />");
            temp = temp.Replace("\n", "<br />"); //look for any  remaining line feeds


            return temp;
        }

        public static string OutgoingHTMLtoXML(this string rawHTML)
        {
            string temp = rawHTML.Replace("& ", "&amp; ");
            temp = temp.Replace("<nbh>", "-"); // imports might contain the non-breaking hyphen // 20211028 - TinyMCE/Composition doesn't support &#8209;
            temp = temp.Replace("<nbs>", "&#160;"); // imports might contain the non-breaking space
            temp = temp.Replace("\"", "&quot;");
            temp = temp.Replace("'", "&#39;");
            temp = temp.Replace("&nbsp;", "&#160;");
            temp = temp.ReplaceCI("<br>", "<br />");
            temp = temp.Replace(Environment.NewLine, "<br />");
            temp = temp.Replace("\n", "<br />"); //look for any  remaining line feeds
            temp = temp.Replace("<", "&lt;"); // including br tags
            temp = temp.Replace(">", "&gt;");


            return temp;
        }

        public static string ReplaceFirstCellMatches(this string textOne, string textTwo)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();
            MatchCollection mcE = Regex.Matches(textOne, @"<row>\s*<cell>\s*(.*?)\s*</cell>");
            foreach (Match m in mcE)
                if (m.Success && m.Groups.Count == 2 && m.Groups[1].Value.Length > 0)
                    replacements.Add(m.Groups[1].Value, m.Groups[1].Value);

            MatchCollection mcEP = Regex.Matches(textTwo, @"<row>\s*<cell>\s*(.*?)\s*</cell>");
            foreach (Match m in mcEP)
                if (m.Success && m.Groups.Count == 2 && m.Groups[1].Value.Length > 0)
                {
                    KeyValuePair<string, string> fv = replacements.FirstOrDefault(x => x.Value.StripTagsExceptAlpha() == m.Groups[1].Value.IncomingXMLtoHTML().StripTagsExceptAlpha());
                    if (fv.Key != null)
                        replacements[fv.Key] = m.Groups[1].Value;
                }

            foreach (KeyValuePair<string, string> kvp in replacements)
                if (kvp.Key != kvp.Value)
                    textOne = textOne.Replace(kvp.Key, kvp.Value);

            return textOne;
        }

        public static Dictionary<string, string> FindAllHTMLTokens(this string input)
        {
            Dictionary<string, string> found = new Dictionary<string, string>();
            MatchCollection mcE = Regex.Matches(input, @"<(.+?)>");
            foreach (Match m in mcE)
                if (m.Success && m.Groups.Count == 2 && m.Groups[1].Value.Length > 0 && !found.ContainsKey(m.Groups[1].Value))
                    found.Add(m.Groups[1].Value, "<" + m.Groups[1].Value + ">");

            return found;
        }

        public static string InsertHeaderRow(this string table, string headerRow, int insertAtRow = 1)
        {
            // ToDo: What if the number of columns don't match?
            if (table.IndexOf("<table>", StringComparison.OrdinalIgnoreCase) == 0 && headerRow.IndexOf("<row>", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (insertAtRow < 2)
                    return table.ReplaceCI("<table>", "<table>" + headerRow);
                else
                {
                    int pos = table.IndexOfOccurrence("<row>", insertAtRow, StringComparison.OrdinalIgnoreCase);
                    if (pos >= 0)
                        return table.Substring(0, pos) + headerRow + table.Substring(pos);
                    else
                        return table.ReplaceCI("</table>", headerRow + "</table>");
                }
            }
            return table;
        }

        public static string InsertHeaderRow(this string table, string headerRow, DataRowInsert dri = DataRowInsert.FirstRow)
        {
            // ToDo: What if the number of columns don't match?
            if (table.IndexOf("<table>", StringComparison.OrdinalIgnoreCase) == 0 && headerRow.IndexOf("<row", StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (dri == DataRowInsert.FirstRow)
                    return table.ReplaceCI("<table>", "<table>" + headerRow);
                else if (dri == DataRowInsert.AfterDescRepeat || dri == DataRowInsert.AfterColumnChange)
                {
                    StringBuilder sb = new StringBuilder("<table>");
                    string[] rows = table.ReplaceCI("<table>", string.Empty).ReplaceCI("</table>", string.Empty).Split(new[] { "<row>", "<row />" }, StringSplitOptions.RemoveEmptyEntries);
                    string prevValue = "|NEWVALUE|";
                    foreach (string row in rows)
                    {
                        if (row.IndexOf("</row>", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            sb.Append("<row>");
                            if (dri == DataRowInsert.AfterDescRepeat)
                            {
                                string sum = string.Empty;
                                string[] columns = row.Split(new[] { "<cell>", "<cell />" }, StringSplitOptions.RemoveEmptyEntries);
                                sb.Append(row);
                                for (int c = 0; c < columns.Count(); c++)
                                    sum += columns[c].ReplaceCI("</cell>", string.Empty).ReplaceCI("</row>", string.Empty);

                                if (sum == columns[0].ReplaceCI("</cell>", string.Empty).ReplaceCI("</row>", string.Empty)) // if the only value on the row is the first column then add the header row after
                                    sb.Append(headerRow);
                            }
                            else if (dri == DataRowInsert.AfterColumnChange) // in this table when the first column value changes (N/A or blank are not changes) move the first column to a row by itself, add the repeating header row, add the rest of the columns.
                            {
                                string[] columns = row.Split(new[] { "<cell>", "<cell />" }, StringSplitOptions.None);
                                string curValue = columns[1].ReplaceCI("</cell>", string.Empty).ReplaceCI("</row>", string.Empty); // first column from the split is always nothing since there is nothing before <cell>
                                if (prevValue != curValue && !curValue.IsNaOrBlank())
                                {
                                    sb.Append($"<cell>{curValue}</cell>");
                                    for (int c = 3; c < columns.Count(); c++)
                                        sb.Append("<cell />");

                                    sb.Append($"</row>{headerRow}<row>");
                                    prevValue = curValue;
                                }
                                for (int c = 2; c < columns.Count(); c++) // no change on first column so skip the first column and output all other columns
                                    sb.Append($"<cell>{columns[c].ReplaceCI("</cell>", string.Empty).ReplaceCI("</row>", string.Empty)}</cell>");

                                sb.Append("</row>");
                            }
                        }
                    }
                    return sb.Append("</table>").ToString();
                }
                else if (dri == DataRowInsert.ClearExtraColumns) // this method removes extra columns in the data to match the header - assumed left to right
                {
                    int headerColumns = headerRow.XMLColumnCount();

                    if (headerColumns < table.XMLColumnCount())
                    {
                        PDI_DataTable contentTable = table.XMLtoDataTable(); // easier to remove columns from a datatable but this currently strips rowtypes - fixed rowtypes
                        for (int i = contentTable.Columns.Count - 1; i >= headerColumns; i--)
                            contentTable.Columns.RemoveAt(i);
                        return contentTable.DataTabletoXML().ReplaceCI("<table>", "<table>" + headerRow);
                    }
                    else // number of columns in header and content match
                        return table.ReplaceCI("<table>", "<table>" + headerRow);
                }
            }
            return table;
        }

        public static int IndexOfOccurrence(this string text, string find, int occurrence, StringComparison compare = StringComparison.OrdinalIgnoreCase)
        {
            int index = 0;
            int counter = 0;
            while ((index = text.IndexOf(find, index, compare)) != -1)
            {
                counter++;
                //have we found the occurrence we want?
                if (counter == occurrence)
                    return index;
                index += find.Length;
            }
            return -1;
        }

        public static string IncomingXMLtoHTML(this string rawXML)
        {
            rawXML = rawXML.Replace("&lt;", "<");
            rawXML = rawXML.Replace("&gt;", ">");
            return rawXML;
        }

        public static string CleanCellContents(this string value)
        {
            int start = 0;
            int cellStart = 0;
            int cellEnd = 0;

            if (value is null || value.Length < 1 || !value.Contains("<cell>"))
                return value;

            StringBuilder output = new StringBuilder(value.Length);
            while (start < value.Length)
            {
                cellStart = value.IndexOf("<cell>", start, StringComparison.OrdinalIgnoreCase);
                cellEnd = value.IndexOf("</cell>", start, StringComparison.OrdinalIgnoreCase);
                if (cellStart > 0 && cellEnd > 0)
                {
                    output.Append(value.Substring(start, cellStart + 6 - start)); // add up to the end of <cell>
                    output.Append(value.Substring(cellStart + 6, cellEnd - 6 - cellStart).CleanXML());
                    output.Append(value.Substring(cellEnd, 7));
                    start = cellEnd + 7;
                }
                else
                {
                    output.Append(value.Substring(start));
                    start = value.Length;
                }
            }
            return output.ToString();
        }

        public static string RemoveTableRows(this string table, int removeRows)
        {
            int removeTo = -1;
            if (removeRows > 0)
            {
                removeTo = table.IndexOfOccurrence("</row>", removeRows, StringComparison.OrdinalIgnoreCase);
                if (removeTo >= 0)
                    removeTo += 6;
            }
            else
                removeTo = table.IndexOf("<row>", StringComparison.OrdinalIgnoreCase);

            if (removeTo >= 0)
                return table.Substring(removeTo);

            return table;
        }


        /// <summary>
        /// Return the 1 based index of the column containing the provided text - only searches a single row
        /// </summary>
        /// <param name="rowText">The text contents of an XML formatted row</param>
        /// <param name="searchText">The text to find</param>
        /// <returns>-1 if not found otherwise the 1 based column index</returns>
        public static int FindXMLColumnByText(this string rowText, List<string> searchText)
        {
            string[] columns = rowText.Split(new[] { "</cell>", "<cell />" }, StringSplitOptions.None);
            foreach (string searchFor in searchText)
            {
                for (int i = 0; i < columns.Count(); i++)
                {
                    if (columns[i].IndexOf(searchFor, StringComparison.OrdinalIgnoreCase) >= 0)
                        return i + 1;
                }
            }
            return -1;
        }

        /// <summary>
        /// Given an xml representation of a table or row count the number of columns in the first row 
        /// </summary>
        /// <param name="text">An XML formatted string </param>
        /// <returns>The number of columns denoted by <cell></cell> or <cell /></returns>
        public static int XMLColumnCount(this string text)
        {
            string workingText = text;
            if (workingText.Contains("<row"))
            {
                // grab only the first row
                string[] workingRows = workingText.Split(new[] { "</row>", "<row />" }, StringSplitOptions.None);
                if (workingRows.Length > 0)
                    workingText = workingRows[0];
            }

            if (workingText.Contains("<cell"))
            {
                string[] workingCols = workingText.Split(new[] { "</cell>", "<cell />" }, StringSplitOptions.None);
                return workingCols.Length - 1; // the last </cell> tag will create an empty string in the split
            }
            return -1;
        }

        internal static string XMLColumnValueByIndex(this string rowText, int column)
        {
            string[] columns = rowText.Split(new[] { "</cell>", "<cell />" }, StringSplitOptions.None);
            if (column <= columns.Count()) // less than or equal check for because value is one based instead of zero
                return columns[column - 1].RemoveHTML(); //.Replace(",", string.Empty)

            return null;
        }

        public static bool IsBlankOrNaXMLColumnValueByIndex(this string rowText, int oneBasedColumn)
        {
            string val = rowText.XMLColumnValueByIndex(oneBasedColumn);
            if (val != null && !val.IsNaOrBlank())
                return false;

            return true;
        }

        /// <summary>
        /// Return true if positive or 0 - false otherwise
        /// </summary>
        /// <param name="rowText"></param>
        /// <param name="oneBasedColumn"></param>
        /// <returns></returns>
        public static bool IsPositiveXMLColumnValueByIndex(this string rowText, int oneBasedColumn)
        {
            string val = rowText.XMLColumnValueByIndex(oneBasedColumn);
            if (val != null && !val.IsNaOrBlank())
            {
                NumberStyles style = NumberStyles.AllowParentheses | NumberStyles.AllowTrailingSign | NumberStyles.Float | NumberStyles.AllowThousands;

                if (val == "(0)") // Special handling for SF12b/c (Forwards) - the string value is only the rounded value so - would be negative or positive. They will now show the specific 0 vs (0) after rounding but of course a negative (or positive) 0 is nonsensical so we need to handle it specifically or it will be returned as positive. 
                    return false;
                else if (val.Contains("."))
                {
                    if (double.TryParse(val, style, null, out double pDouble))
                        if (pDouble >= 0)
                            return true;
                        else
                            return false;
                }
                else if (int.TryParse(val, style, null, out int pInt))
                {
                    if (pInt >= 0) // return zero's as positive for simplicity
                        return true;
                    else
                        return false;
                }
            }
            return true; // not a number
        }

        public static int FindLastPositiveXMLRowIndex(this string tableText, int checkColumn, int removeRows, int requiredColumn = -1, int startPos = 0)
        {
            if (tableText is null || tableText.Length < 1)
                return -1;

            if (removeRows > 0)
                startPos = tableText.IndexOfOccurrence("</row>", removeRows, StringComparison.OrdinalIgnoreCase);

            if (startPos > 0)
                startPos += 6;
            else if (startPos < 0)
                return startPos;

            //if (tableText.Contains("<row />")) // if we have already added the blank row between positive and negative we can short cut looking for the positive and negative column values - not including this will cause sorting issues
            //    return tableText.IndexOf("<row />");

            while (startPos < tableText.Length)
            {
                int curPos = tableText.IndexOf("</row>", startPos, StringComparison.OrdinalIgnoreCase);
                if (curPos > 0)
                {
                    if (tableText.Substring(startPos, curPos - startPos).IsPositiveXMLColumnValueByIndex(checkColumn) && (requiredColumn <= 0 || !tableText.Substring(startPos, curPos - startPos).IsBlankOrNaXMLColumnValueByIndex(requiredColumn)))
                        startPos = curPos + 6;
                    else
                        return startPos;
                }
                else
                    startPos = tableText.Length;
            }
            return -1;
        }

        /// <summary>
        /// This version of the extract returns the number of rows specified - used to extract the header
        /// </summary>
        /// <param name="tableText">The table source in XML string format</param>
        /// <param name="returnRows">The number of rows to return</param>
        /// <returns>A string containing the first returnRows rows</returns>
        public static string ExtractXMLRows(this string tableText, int returnRows)
        {
            if (!tableText.Contains("<table") || !tableText.Contains("<row"))
                return string.Empty;

            string[] rows = tableText.ReplaceCI("<table>", string.Empty).ReplaceCI("</table>", string.Empty).ReplaceCI("<table/>", string.Empty).ReplaceCI("<table />", string.Empty).Split(new[] { "<row" }, StringSplitOptions.RemoveEmptyEntries);
            string header = string.Empty;
            for (int i = 0; i < returnRows; i++)
                header += "<row" + rows[i]; // readd the header rows

            return header;

        }

        public static string ExtractXMLRows(this string tableText, int checkColumn, int removeRows, int direction = 1, int requiredColumn = -1, int startPos = 0)
        {
            if (!tableText.Contains("<table"))
                return string.Empty;

            if (removeRows > 0 && startPos == 0)
            {
                startPos = tableText.IndexOfOccurrence("</row>", removeRows, StringComparison.OrdinalIgnoreCase);
                if (startPos > 0)
                    startPos += 6;
            }

            int extractStart = -1;
            int extractEnd = -1;

            if (direction < 0) // find the start of the negative values
            {
                while (startPos < tableText.Length)
                {
                    int curPos = tableText.IndexOf("</row>", startPos, StringComparison.OrdinalIgnoreCase);
                    if (curPos > 0)
                    {
                        if (tableText.Substring(startPos, curPos - startPos).IsPositiveXMLColumnValueByIndex(checkColumn) || (requiredColumn > 0 && tableText.Substring(startPos, curPos - startPos).IsBlankOrNaXMLColumnValueByIndex(requiredColumn)))
                            startPos = curPos + 6;
                        else
                            break;
                    }
                    else
                        startPos = tableText.Length;
                }
            }

            extractStart = startPos;
            while (startPos < tableText.Length)
            {
                int curPos = tableText.IndexOf("</row>", startPos, StringComparison.OrdinalIgnoreCase);
                if (curPos > 0)
                {
                    if (requiredColumn <= 0 || !tableText.Substring(startPos, curPos - startPos).IsBlankOrNaXMLColumnValueByIndex(requiredColumn))
                    {
                        if ((direction > 0 && tableText.Substring(startPos, curPos - startPos).IsPositiveXMLColumnValueByIndex(checkColumn)) || (direction < 0 && !tableText.Substring(startPos, curPos - startPos).IsPositiveXMLColumnValueByIndex(checkColumn)))
                            startPos = curPos + 6;
                        else
                            break;
                    }
                    else
                        break;
                }
                else
                    startPos = tableText.Length;
            }
            extractEnd = startPos;

            if (extractEnd != extractStart)
                return tableText.Substring(extractStart, extractEnd - extractStart).ReplaceCI("</table>", string.Empty);
            else
                return string.Empty;
        }

        /// <summary>
        /// Converts an integer to a base 26 representation using the start character as the first letter
        /// </summary>
        /// <param name="number">The integer to convert</param>
        /// <param name="startChar">The start point of the 26 characters</param>
        /// <returns>String of characters as the base 26 representation of the integer</returns>
        public static string ToBase26(this int number, char startChar = 'a')
        {
            if (number <= 0)
                return null;

            var array = new LinkedList<int>();

            while (number > 26)
            {
                int value = number % 26;
                if (value == 0)
                {
                    number = number / 26 - 1;
                    array.AddFirst(26);
                }
                else
                {
                    number /= 26;
                    array.AddFirst(value);
                }
            }

            if (number > 0)
                array.AddFirst(number);

            return new string(array.Select(s => (char)(startChar + s - 1)).ToArray());
        }

        /// <summary>
        /// Converts a "number" in base 26 to an integer using the start character to determine the character to int values
        /// </summary>
        /// <param name="value">The base 26 number as a string of characters</param>
        /// <param name="startChar">The start point of the the 26 characters</param>
        /// <returns>An integer value that equals the provided string</returns>
        public static int FromBase26(this string value, char startChar = 'a')
        {
            if (value is null || value.Length == 0)
                return -1;

            int sum = 0;
            for (int i = 0; i < value.Length; i++)
            {
                sum *= 26;
                sum += (value[i] - startChar + 1); // providing 'A' for startChar will return upper case sequence
            }

            return sum;
        }

        /// <summary>
        /// When the string has characters exceeding the start character plus 25 (26 values) "wrap" the string back to the beginning
        /// when the start character of the sequence is not 'a' then the characters 'a' to ? come after 'z' 
        /// </summary>
        /// <param name="current">The current string that may exceed 'z'</param>
        /// <param name="startChar">The start of the 26 character sequence</param>
        /// <returns>The string wrapped to fit within 26 characters of the start character</returns>
        public static string Wrap26(this string current, char startChar = 'a')
        {
            if (current is null || current.Length < 1)
                return current;

            string ret = string.Empty;
            foreach (char c in current)
            {
                if (c > startChar + 25)
                    ret += (char)(startChar + (c - (startChar + 26)));
                else
                    ret += c;
            }
            return ret;

        }

        /// <summary>
        /// Performs the inverse of Wrap26 and takes a wrapped sequence back into it's unwrapped representation that may exceed the character 'z'
        /// </summary>
        /// <param name="current">The wrapped string</param>
        /// <param name="startChar">The point at which the wrapping occurred</param>
        /// <returns>The base 26 string in it's original unwrapped state</returns>
        public static string UnWrap26(this string current, char startChar = 'a')
        {
            if (current is null || current.Length < 1)
                return current;

            string ret = string.Empty;
            foreach (char c in current)
            {
                if (c < startChar)
                    ret += (char)(startChar + 26 - (startChar - c));
                else
                    ret += c;
            }
            return ret;
        }

        /// <summary>
        /// Given a starting point of characters provide the next character available based on the 26 characters in the alphabet - add additional "columns" of characters until the last string is reached
        /// </summary>
        /// <param name="current">The current field letter codes</param>
        /// <param name="first">The start point of the field sequence defaults to "a"</param>
        /// <param name="last">The last valid sequence defaults to "zzz"</param>
        /// <param name="restricted">Disable the characters 'f' and 'h' from appearing in the string default true</param>
        /// <param name="startLetter">The start point for the 26 valid characters default 'a'</param>
        /// <returns></returns>
        public static string IncrementFieldLetter(this string current, string first = "a", string last = "zzz", bool restricted = true, char startLetter = 'a')
        {
            if (current is null || current.Length < 1)
                return first;

            
            char lastChar = first[first.Length - 1];    // the last character in the first string determines the wrap point for the sequence


            foreach (char c in current)
                if (c > startLetter + 25 || c < startLetter) // check if the current value is out of range
                    return null;

            int newBase = current.UnWrap26(lastChar).FromBase26(lastChar) + 1; // unwrap the current string based on the wrap point and convert to base 26 with the start character and increment
            string newString = newBase.ToBase26(lastChar).Wrap26(startLetter); // convert the newly incremented number to base26 using the wrap point and wrap using the startLetter (default 'a')

            while (restricted && (newString.Contains('f') || newString.Contains('h'))) // h and f are restricted from any position in the output field letter codes - increment until neither are present
            {
                newBase += 1;
                newString = newBase.ToBase26(lastChar).Wrap26(startLetter);
            }
      
            if (newString.UnWrap26(lastChar).FromBase26(lastChar) > last.UnWrap26(lastChar).FromBase26(lastChar)) // compensate for the offset by using UnWrap and check that we haven't exceeded the max element
                return null;

            return newString;
        }

        public static string GetFieldNamePrefix(this string fieldName)
        {
            if (fieldName != null && fieldName.Length > 0)
            {
                Match m = Regex.Match(fieldName, @"^([A-Z]+\d+)(\S*)");
                if (m.Success && m.Groups.Count > 1)
                    return m.Groups[1].ToString();   
            }
            return fieldName;
        }
        
        // https://www.emoreau.com/Entries/Articles/2017/02/Net-code-to-convert-numbers-to-words.aspx

        public static string ConvertNumberToWords(int pValue, string culture = "en-CA")
        {
            string strReturn;
            if (pValue < 0)
                throw new NotSupportedException("negative numbers not supported");
            else if (pValue == 0)
                strReturn = culture == "en-CA" ? "zero" : "zéro";
            else if (pValue < 10)
                strReturn = ConvertDigitToWords(pValue, culture);
            else if (pValue < 20)
                strReturn = ConvertTeensToWords(pValue, culture);
            else if (pValue < 100)
                strReturn = ConvertHighTensToWords(pValue, culture);
            else if (pValue < 1000)
                strReturn = ConvertBigNumberToWords(pValue, 100, "hundred", culture);
            else if (pValue < 1000000)
                strReturn = ConvertBigNumberToWords(pValue, 1000, "thousand", culture);
            else if (pValue < 1000000000)
                strReturn = ConvertBigNumberToWords(pValue, 1000000, "million", culture);
            else
                throw new NotSupportedException("Number is too large!!!");

            if (culture == "fr-CA")
            {
                if (strReturn.EndsWith("quatre-vingt"))
                {
                    //another French exception
                    strReturn += "s";
                }
            }
            return strReturn;
        }

        private static string ConvertDigitToWords(int pValue, string culture = "en-CA")
        
        {
            switch (pValue)
            {
                case 0: return "";
                case 1: return culture == "en-CA" ? "one" : "un";
                case 2: return culture == "en-CA" ? "two" : "deux";
                case 3: return culture == "en-CA" ? "three" : "trois";
                case 4: return culture == "en-CA" ? "four" : "quatre";
                case 5: return culture == "en-CA" ? "five" : "cinq";
                case 6: return "six";
                case 7: return culture == "en-CA" ? "seven" : "sept";
                case 8: return culture == "en-CA" ? "eight" : "huit";
                case 9: return culture == "en-CA" ? "nine" : "neuf";
                default:
                    throw new IndexOutOfRangeException($"{pValue} not a digit");
            }
        }

        //assumes a number between 10 & 19
        private static string ConvertTeensToWords(int pValue, string culture = "en-CA")
        {
            switch (pValue)
            {
                case 10: return culture == "en-CA" ? "ten" : "dix";
                case 11: return culture == "en-CA" ? "eleven" : "onze";
                case 12: return culture == "en-CA" ? "twelve" : "douze";
                case 13: return culture == "en-CA" ? "thirteen" : "treize";
                case 14: return culture == "en-CA" ? "fourteen" : "quatorze";
                case 15: return culture == "en-CA" ? "fifteen" : "quinze";
                case 16: return culture == "en-CA" ? "sixteen" : "seize";
                case 17: return culture == "en-CA" ? "seventeen" : "dix-sept";
                case 18: return culture == "en-CA" ? "eighteen" : "dix-huit";
                case 19: return culture == "en-CA" ? "nineteen" : "dix-neuf";
                default:
                    throw new IndexOutOfRangeException($"{pValue} not a teen");
            }
        }

        //assumes a number between 20 and 99
        private static string ConvertHighTensToWords(int pValue, string culture = "en-CA")
        {
            int tensDigit = (int)(Math.Floor((double)pValue / 10.0));

            string tensStr;
            switch (tensDigit)
            {
                case 2: tensStr = culture == "en-CA" ? "twenty" : "vingt"; break;
                case 3: tensStr = culture == "en-CA" ? "thirty" : "trente"; break;
                case 4: tensStr = culture == "en-CA" ? "forty" : "quarante"; break;
                case 5: tensStr = culture == "en-CA" ? "fifty" : "cinquante"; break;
                case 6: tensStr = culture == "en-CA" ? "sixty" : "soixante"; break;
                case 7: tensStr = culture == "en-CA" ? "seventy" : "soixante-dix"; break;
                case 8: tensStr = culture == "en-CA" ? "eighty" : "quatre-vingt"; break;
                case 9: tensStr = culture == "en-CA" ? "ninety" : "quatre-vingt-dix"; break;
                default:
                    throw new IndexOutOfRangeException($"{pValue} not in range 20-99");
            }

            if (pValue % 10 == 0) return tensStr;

            //French sometime has a prefix in front of 1
            string strPrefix = string.Empty;
            if (culture == "fr-CA" && (tensDigit < 8) && (pValue - tensDigit * 10 == 1))
                strPrefix = "-et";

            string onesStr;
            if (culture == "fr-CA" && (tensDigit == 7 || tensDigit == 9))
            {
                tensStr = ConvertHighTensToWords(10 * (tensDigit - 1), culture);
                onesStr = ConvertTeensToWords(10 + pValue - tensDigit * 10, culture);
            }
            else
                onesStr = ConvertDigitToWords(pValue - tensDigit * 10, culture);

            return tensStr + strPrefix + "-" + onesStr;
        }

        // Use this to convert any integer bigger than 99
        private static string ConvertBigNumberToWords(int pValue, int baseNum, string baseNumStr, string culture = "en-CA")
        {
            // special case: use commas to separate portions of the number, unless we are in the hundreds
            string separator;
            if (culture == "fr-CA")
                separator = " ";
            else
                separator = (baseNumStr != "hundred") ? ", " : " ";

            // Strategy: translate the first portion of the number, then recursively translate the remaining sections.
            // Step 1: strip off first portion, and convert it to string:
            int bigPart = (int)(Math.Floor((double)pValue / baseNum));
            string bigPartStr;
            if (culture == "fr-CA")
            {
                string baseNumStrFrench;
                switch (baseNumStr)
                {
                    case "hundred":
                        baseNumStrFrench = "cent";
                        break;
                    case "thousand":
                        baseNumStrFrench = "mille";
                        break;
                    case "million":
                        baseNumStrFrench = "million";
                        break;
                    case "billion":
                        baseNumStrFrench = "milliard";
                        break;
                    default:
                        baseNumStrFrench = "????";
                        break;
                }
                if (bigPart == 1 && pValue < 1000000)
                    bigPartStr = baseNumStrFrench;
                else
                    bigPartStr = ConvertNumberToWords(bigPart, culture) + " " + baseNumStrFrench;
            }
            else
                bigPartStr = ConvertNumberToWords(bigPart, culture) + " " + baseNumStr;

            // Step 2: check to see whether we're done:
            if (pValue % baseNum == 0)
            {
                if (culture == "fr-CA")
                {
                    if (bigPart > 1)
                    {
                        //in French, a s is required to cent/mille/million/milliard if there is a value in front but nothing after
                        return bigPartStr + "s";
                    }
                    else
                        return bigPartStr;
                }
                else
                    return bigPartStr;
            }

            // Step 3: concatenate 1st part of string with recursively generated remainder:
            int restOfNumber = pValue - bigPart * baseNum;
            return bigPartStr + separator + ConvertNumberToWords(restOfNumber, culture);
        }

        /// <summary>
        /// https://www.danylkoweb.com/Blog/10-extremely-useful-net-extension-methods-8J
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Stream ToStream(this string str)
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(str);
            //byte[] byteArray = Encoding.ASCII.GetBytes(str);
            return new MemoryStream(byteArray);
        }
        public static string ToString(this Stream stream)
        {
            var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
        /// <summary>
        /// Copy from one stream to another.
        /// Example:
        /// using(var stream = response.GetResponseStream())
        /// using(var ms = new MemoryStream())
        /// {
        ///     stream.CopyTo(ms);
        ///      // Do something with copied data
        /// }
        /// </summary>
        /// <param name="fromStream">From stream.</param>
        /// <param name="toStream">To stream.</param>
        public static void CopyTo(this Stream fromStream, Stream toStream)
        {
            if (fromStream == null)
                throw new ArgumentNullException("fromStream");
            if (toStream == null)
                throw new ArgumentNullException("toStream");
            var bytes = new byte[8092];
            int dataRead;
            while ((dataRead = fromStream.Read(bytes, 0, bytes.Length)) > 0)
                toStream.Write(bytes, 0, dataRead);
        }

    }
}
