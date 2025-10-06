using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Publisher_Data_Operations.Extensions;
using System.Runtime.CompilerServices;

namespace Publisher_Data_Operations_Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("n/a ", true)]
        [InlineData("N/A", true)]
        [InlineData("\t \n\u00A0", true)]
        [InlineData(null, true)]
        [InlineData("anything", false)]
        [InlineData("anything N/A", false)]
        [InlineData("\u00A0 n/A", true)]
        [InlineData("'N/A'", false)]
        [InlineData("\"N/A\"", false)]
        public void isNaOrBlankTests(string value, bool expected)
        {
            Assert.Equal(expected, value.IsNaOrBlank());
        }

        [Theory]
        [InlineData("BrandNewSeries", "New", "", "BrandSeries")]
        [InlineData("BrandNewFund", "brand", "", "NewFund")]
        [InlineData("TestNotExist", "brand", "", "TestNotExist")]
        public void replaceCaseInsensitiveTests(string value, string value2, string value3, string expected)
        {
            Assert.Equal(expected, value.ReplaceCI(value2, value3));
        }


        [Theory]
        [InlineData("n/a", false)]
        [InlineData("N/A", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        [InlineData("Y", true)]
        [InlineData("Yes", true)]
        [InlineData("y", true)]
        [InlineData("1", true)]
        [InlineData("True", true)]
        [InlineData("true", true)]
        [InlineData("false", false)]
        [InlineData(" y", true)]
        [InlineData(" 1", true)]
        public void stringToBoolTests(string value, bool expected)
        {
            Assert.Equal(expected, value.ToBool());
        }

        [Theory]
        [InlineData("12/12/2019", 2019, 12, 12)]
        [InlineData("01/01/2020", 2020, 1, 1)]
        [InlineData(" 01/03/2013", 9999, 12, 31)]
        [InlineData("2013-10-25", 2013, 10, 25)] // adding ISO date format for FSMRFP BAU/STATIC
        [InlineData("2021-09-31", 9999, 12, 31)] // invalid ISO date - sept has 30 days
        public void stringToDateTests(string value, int year, int month, int day)
        {
            Assert.Equal(new DateTime(year, month, day), value.ToDate(DateTime.MaxValue.Date));
        }

        [Theory]
        [InlineData("30/09/2020", "13/02/2009", 10)]
        [InlineData("10/10/2020", "10/10/2020", 0)]
        [InlineData("30/09/2020", "30/06/2005", 10)] //now maximum age is 10
        [InlineData("31/12/2020", "01/01/2020", 1)]
        [InlineData("30/12/2020", "01/01/2019", 1)]
        [InlineData("31/12/2020", "02/01/2020", 0)]
        [InlineData("30/12/2020", "01/01/2020", 0)]
        [InlineData("12/06/2020", "13/07/2012", 7)]
        [InlineData("01/01/2021", "27/06/2016", 4)]
        [InlineData("31/12/2021", "27/11/2020", 1)]
        public void stringAgeInCalendarYearsTests(string value, string inceptionDate, int expected)
        {
            Assert.Equal(expected, value.AgeInCalendarYears(inceptionDate));

        }

        [Theory]
        [InlineData("30/09/2020", 2020)]
        [InlineData("31/12/2020", 2021)]
        public void stringFilingYearTests(string value, int expected)
        {
            Assert.Equal(expected, value.FilingYear());
        }

        [Theory]
        [InlineData("1000", "fr-CA", "1 000 $")]
        [InlineData("-450", "fr-CA", "(450 $)")]
        [InlineData("999", "en-CA", "$999")]
        [InlineData("2543.54", "en-CA", "$2,544")]
        [InlineData("2543.45", "en-CA", "$2,543")]
        [InlineData("-2500", "en-CA", "-$2,500")]
        public void stringToCurrencyTests(string value, string culture, string expected)
        {
            string ret = value.ToCurrency(culture);
            expected = expected.Replace(' ', '\u00A0');
            Assert.Equal(expected, ret);

        }

        [Theory]
        [InlineData("11.59", "en-CA", "$11.59")]
        [InlineData("1001.65", "fr-CA", "1 001,65 $")]
        [InlineData("-1001.65", "fr-CA", "(1 001,65 $)")]
        [InlineData("-11.59", "en-CA", "-$11.59")]
        public void stringToCurrencyDecimalTests(string value, string culture, string expected)
        {
            string ret = value.ToCurrencyDecimal(culture);
            expected = expected.Replace(' ', '\u00A0');
            Assert.Equal(expected, ret);
        }

        [Theory]
        [InlineData("0.856", "fr-CA", -1, "0,856 %")]
        [InlineData("20.800", "fr-CA", -1, "20,800 %")]
        [InlineData("0.10", "en-CA", -1, "0.10%")]
        [InlineData("-0.10", "en-CA", -1, "-0.10%")]
        [InlineData("20.100", "en-CA", -1, "20.100%")]
        [InlineData("20000", "en-CA", -1, "20,000%")]
        [InlineData("20000", "fr-CA", -1, "20 000 %")]
        [InlineData("2", "fr-CA", -1, "2 %")]
        [InlineData("11.65", "en-CA", -1, "11.65%")]
        [InlineData("11.65", "en-CA", 1, "11.7%")]
        [InlineData("18", "en-CA", 1, "18.0%")]
        [InlineData("-20.800", "fr-CA", -1, "-20,800 %")]
        public void stringToPercentTests(string value, string culture, int scale, string expected)
        {
            Assert.Equal(expected.Replace(' ', '\u00A0'), value.ToPercent(culture, scale));

        }
        [Theory]
        [InlineData("15/05/2010", "15/05/2020", 10, true)]
        [InlineData("15/05/2010", "14/05/2020", 10, false)]
        [InlineData("02/06/2015", "02/06/2020", 5, true)]
        [InlineData("02/06/2015", "01/06/2020", 5, false)]
        [InlineData("29/09/2019", "29/09/2020", 1, true)]
        [InlineData("29/09/2019", "28/09/2020", 1, false)]
        [InlineData("16/07/2020", "30/09/2021", 1, true)]
        public void consecutiveYearsTests(string startdate, string endDate, int yearDif, bool expected)
        {
            Assert.Equal(expected, startdate.ConsecutiveYears(endDate, yearDif));

        }

        [Theory]
        [InlineData("13/02/2009", "30/09/2020", false)]
        [InlineData("25/05/2020", "30/09/2020", true)]
        public void yearEqualTests(string thisdate, string otherdate, bool expected)
        {
            Assert.Equal(expected, thisdate.YearEqual(otherdate));
        }

        [Theory]
        [InlineData("10", 1, -1, "en-CA", "10.0")]
        [InlineData("9.22", -1, 1, "en-CA", "9.2")]
        [InlineData("9.22", 1, -1, "en-CA", "9.22")]
        [InlineData("9.2", -1, 2, "en-CA", "9.20")]
        [InlineData("1539.2", -1, 2, "fr-CA", "1 539,20")]
        [InlineData("11204", -1, -1, "en-CA", "11,204")]
        [InlineData("11204", -1, -1, "fr-CA", "11 204")]
        [InlineData("-11204", -1, -1, "en-CA", "-11,204")]
        [InlineData("-11204", -1, -1, "fr-CA", "-11 204")]
        [InlineData(null, 1, -1, null, null)]
        public void toDecimalTests(string thisValue, int minScale, int fixedScale, string culture, string expected)
        {
            expected = expected is null ? expected : expected.Replace(' ', '\u00A0');
            Assert.Equal(expected, thisValue.ToDecimal(minScale, fixedScale, culture));
        }

        [Theory]
        [InlineData("DisplayValue", "Replaced Value", "This is the <displayvalue> string", "This is the Replaced Value string")]
        public void replaceByDictionaryTests(string key, string val, string replace, string expected)
        {
            Assert.Equal(expected, replace.ReplaceByDictionary(new Dictionary<string, string> { { key, val } }));
        }

        [Theory]
        [InlineData("<table><row><cell></cell><cell>Annual rate&#39;s<br>(as a % of the series’ value)</cell> </row> <row> <cell><p><strong>S&P Management expense ratio (MER)</strong><br>This </p></cell> <cell>•%</cell> </row> <row> <cell><strong>Trading&nbsp;expense ratio (TER)</strong><br> These are the fund’s trading costs.</cell> <cell>•%</cell> </row> <row> <cell><strong>Series&amp;Expenses</strong></cell> <cell>•%</cell> </row> </table>", "&lt;table&gt;&lt;row&gt;&lt;cell&gt;&lt;/cell&gt;&lt;cell&gt;Annual rate&#39;s&lt;br /&gt;(as a % of the series’ value)&lt;/cell&gt; &lt;/row&gt; &lt;row&gt; &lt;cell&gt;&lt;p&gt;&lt;strong&gt;S&amp;P Management expense ratio (MER)&lt;/strong&gt;&lt;br /&gt;This &lt;/p&gt;&lt;/cell&gt; &lt;cell&gt;•%&lt;/cell&gt; &lt;/row&gt; &lt;row&gt; &lt;cell&gt;&lt;strong&gt;Trading&#160;expense ratio (TER)&lt;/strong&gt;&lt;br /&gt; These are the fund’s trading costs.&lt;/cell&gt; &lt;cell&gt;•%&lt;/cell&gt; &lt;/row&gt; &lt;row&gt; &lt;cell&gt;&lt;strong&gt;Series&amp;Expenses&lt;/strong&gt;&lt;/cell&gt; &lt;cell&gt;•%&lt;/cell&gt; &lt;/row&gt; &lt;/table&gt;")]
        public void cleanXMLTests(string value, string expected)
        {
            Assert.Equal(expected, value.CleanXML());
        }

        [Theory]
        [InlineData("Classic", new[] { "Class", "Series", "Test" }, false)]
        [InlineData("Class", new[] { "Class", "Series", "Test" }, true)]
        [InlineData("Classic Series", new[] { "Class", "Series", "Test" }, true)]
        [InlineData("Classic Tests", new[] { "Class", "Series", "Test" }, false)]
        public void containsWordsTests(string check, string[] elements, bool expected)
        {
            Assert.Equal(expected, check.ContainsWords(elements));
        }

        [Theory]
        [InlineData("<strong>Test1</strong>", null, "Test1")]
        [InlineData("<strong>Test2<br/></strong>", "strong", "Test2<br/>")]
        [InlineData("<strong>Test3<sup>1</sup></strong>", "strong", "Test3<sup>1</sup>")]
        [InlineData("<strong>Test4<sup>1</sup></strong>", "sup", "<strong>Test41</strong>")]
        [InlineData("Test5<br/><break>Test2", "br", "Test5<break>Test2")]
        [InlineData("Test6<br/><break>Test2</strong>", null, "Test6Test2")]
        public void removeMarkupTests(string value, string tag, string result)
        {
            Assert.Equal(result, value.RemoveMarkup(tag));
        }

        [Theory]
        [InlineData("<strong>Test1</strong>", "Test1")]
        [InlineData("<strong>Test2<br /></strong>", "Test2<br />")]
        [InlineData("<something>Test3<sup>1</sup></something>", "Test3<sup>1</sup>")]
        [InlineData("<something>Test4<something>1</something></something>", "Test4<something>1</something>")]
        [InlineData("Test6<br/><strong>Test2</strong>", "Test6<br/><strong>Test2</strong>")]
        [InlineData("<strong>Test6<br/></strong>Test2<strong>Test4</strong>", "<strong>Test6<br/></strong>Test2<strong>Test4</strong>")]
        [InlineData("<table></table>", "")]
        public void removeBoundingMarkupTests(string value, string result)
        {
            Assert.Equal(result, value.RemoveBoundingMarkup());
        }

        [Theory]
        [InlineData("Classic123", false)]
        [InlineData("123Classic", false)]
        [InlineData("#(123,000.67)", true)]
        [InlineData("-123,000.67", true)]
        [InlineData("+123,000.67", true)]
        [InlineData("5,123,000.67", true)]
        [InlineData("<b>$(42,002.23)</b>", true)]
        [InlineData("4.250%, 2025-02-15", true)] // this is a special case for SOI - the only difference is the space
        public void isNumericTests(string value, bool expected)
        {
            Assert.Equal(expected, value.IsNumeric());
        }

        [Theory]
        [InlineData("23", true, "en-CA", "0.06")]
        [InlineData("3", false, "en-CA", "3")]
        [InlineData("956", true, "en-CA", "2.62")]
        [InlineData("886", true, "en-CA", "2.43")]
        [InlineData("23", true, "fr-CA", "0,06")]
        [InlineData("10.2", false, "en-CA", "10.2")]
        public void toYearsTest(string value, bool isDays, string culture, string expected)
        {
            Assert.Equal(expected, value.ToYears(isDays, culture));
        }

        [Theory]

        [InlineData("23", true, "en-CA", "23")]
        [InlineData("3", false, "en-CA", "1,096")]
        [InlineData("3", false, "fr-CA", "1 096")]
        [InlineData("1054", true, "en-CA", "1,054")]
        [InlineData("1054.1", true, "en-CA", "1,054.1")]
        public void toDaysTest(string value, bool isDays, string culture, string expected)
        {
            Assert.Equal(expected.Replace(' ', '\u00A0'), value.ToDays(isDays, culture));
        }


        [Theory]
        [InlineData("<table><row><cell>Total Number of Investments</cell><cell>160</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity Assets</cell><cell>9</cell></row></table>", "<table><row><cell>Total Number of Investments&lt;sup&gt;4&lt;/sup&gt;</cell><cell>160</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity</cell><cell>9</cell></row></table>", "<table><row><cell>Total Number of Investments&lt;sup&gt;4&lt;/sup&gt;</cell><cell>160</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity Assets</cell><cell>9</cell></row></table>")]
        public void replaceFirstCellMatchesTest(string value, string prodvalue, string expected)
        {
            string test = value.ReplaceFirstCellMatches(prodvalue);
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("<table><row><cell>Total Number & Investments<sup>4</sup></cell><cell>160</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity Assets</cell><cell>9</cell></row></table>", "<table><row><cell>Total Number &amp; Investments&lt;sup&gt;4&lt;/sup&gt;</cell><cell>160</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity Assets</cell><cell>9</cell></row></table>")]
        public void cleanCellContentsTest(string value, string expected)
        {
            string test = value.CleanCellContents();
            Assert.Equal(expected, test);
        }


        [Theory]
        [InlineData("<table><row><cell>Total Number & Investments<sup>4</sup></cell><cell>160</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity Assets</cell><cell>9</cell></row></table>", "<row>", 2, 85)]
        [InlineData("<table><row><cell>Total Number & Investments<sup>4</sup></cell><cell>160</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity Assets</cell><cell>9</cell></row></table>", "</row>", 2, 131)]
        [InlineData("ThisthisThiSTHIS", "this", 4, 12)] // checking ignore case
        public void IndexOfOccuranceTest(string value, string find, int occurance, int expected)
        {
            int test = value.IndexOfOccurrence(find, occurance, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("<table><row><cell>Total Number & Investments<sup>4</sup></cell><cell>160</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity Assets</cell><cell>9</cell></row></table>", "<row><cell>Test cell 1</cell><cell>Test row insert at 2</cell></row>", 2, "<table><row><cell>Total Number & Investments<sup>4</sup></cell><cell>160</cell></row><row><cell>Test cell 1</cell><cell>Test row insert at 2</cell></row><row><cell>Fixed Income</cell><cell>151</cell></row><row><cell>Equity Assets</cell><cell>9</cell></row></table>")]
        public void InsertHeaderRowTest(string value, string header, int row, string expected)
        {
            string test = value.InsertHeaderRow(header, row);
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("<table><row><cell><em>Transactions and balances between the Infrastructure Fund and FC144 Infrastructure US Inc. subsidiary was as follows:</em></cell><cell /><cell /></row><row><cell>Common Stock</cell><cell>1</cell><cell>1</cell></row><row><cell>Transactions and balances between the Private Equity Fund and FC145 Private Equity US Inc. subsidiary was as follows:</cell><cell /><cell /></row><row><cell>Common Stock</cell><cell>1</cell><cell>1</cell></row></table>", "<row><cell /></row><row><cell><strong>As at <CurrentPeriod><br />($)</strong></cell><cell><strong>As at <PriorPeriod><br />($)</strong></cell></row>", DataRowInsert.AfterDescRepeat, "<table><row><cell><em>Transactions and balances between the Infrastructure Fund and FC144 Infrastructure US Inc. subsidiary was as follows:</em></cell><cell /><cell /></row><row><cell /></row><row><cell><strong>As at <CurrentPeriod><br />($)</strong></cell><cell><strong>As at <PriorPeriod><br />($)</strong></cell></row><row><cell>Common Stock</cell><cell>1</cell><cell>1</cell></row><row><cell>Transactions and balances between the Private Equity Fund and FC145 Private Equity US Inc. subsidiary was as follows:</cell><cell /><cell /></row><row><cell /></row><row><cell><strong>As at <CurrentPeriod><br />($)</strong></cell><cell><strong>As at <PriorPeriod><br />($)</strong></cell></row><row><cell>Common Stock</cell><cell>1</cell><cell>1</cell></row></table>")]
        [InlineData("<table><row><cell>Transactions and balances between the Infrastructure Fund and FC144 Infrastructure US Inc. subsidiary was as follows:</cell><cell /><cell /></row><row><cell>Common Stock</cell><cell>1</cell><cell>1</cell></row><row><cell>Transactions and balances between the Private Equity Fund and FC145 Private Equity US Inc. subsidiary was as follows:</cell><cell /><cell /></row><row><cell>Common Stock</cell><cell>1</cell><cell>1</cell></row></table>", "<row><cell /></row><row><cell><strong>As at <CurrentPeriod><br />($)</strong></cell><cell><strong>As at <PriorPeriod><br />($)</strong></cell></row>", DataRowInsert.FirstRow, "<table><row><cell /></row><row><cell><strong>As at <CurrentPeriod><br />($)</strong></cell><cell><strong>As at <PriorPeriod><br />($)</strong></cell></row><row><cell>Transactions and balances between the Infrastructure Fund and FC144 Infrastructure US Inc. subsidiary was as follows:</cell><cell /><cell /></row><row><cell>Common Stock</cell><cell>1</cell><cell>1</cell></row><row><cell>Transactions and balances between the Private Equity Fund and FC145 Private Equity US Inc. subsidiary was as follows:</cell><cell /><cell /></row><row><cell>Common Stock</cell><cell>1</cell><cell>1</cell></row></table>")]
        [InlineData("<table><row><cell>Series A</cell><cell>2.10</cell><cell>1.65</cell><cell /><cell /></row><row><cell>Series F</cell><cell>1.10</cell><cell>0.90</cell><cell /><cell>5.66</cell></row></table>", "<row><cell>Series</cell><cell>5-1</cell><cell>1-0</cell></row>", DataRowInsert.ClearExtraColumns, "<table><row><cell>Series</cell><cell>5-1</cell><cell>1-0</cell></row><row><cell>Series A</cell><cell>2.10</cell><cell>1.65</cell></row><row><cell>Series F</cell><cell>1.10</cell><cell>0.90</cell></row></table>")]
        public void InsertHeaderRowNewTest(string value, string header, DataRowInsert dri, string expected)
        {
            string test = value.InsertHeaderRow(header, dri);
            Assert.Equal(expected, test);
        }

        //[Theory]
        //[InlineData("<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row></table>", "", 1, "<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row></table>")]
        //[InlineData("", "<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row></table>", 1, "<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row></table>")]
        //[InlineData("<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row></table>", "<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>New Row</cell><cell></cell><cell>45,000</cell></row></table>", 1, "<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row><row><cell>New Row</cell><cell></cell><cell>45,000</cell></row></table>")]
        //[InlineData("<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row></table>", "<table><row><cell></cell><cell>New Row 1</cell><cell></cell></row><row><cell>New Row 2</cell><cell></cell><cell>45,000</cell></row></table>", 0, "<table><row><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row><row><cell></cell><cell>New Row 1</cell><cell></cell></row><row><cell>New Row 2</cell><cell></cell><cell>45,000</cell></row></table>")]
        //[InlineData("<table><row rowType=\"Level1.Header\"><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row></table>", "<table></table>", 1, "<table><row rowType=\"Level1.Header\"><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row></table>")]
        //public void AppendTableTests(string table, string append, int removeRows, string expected)
        //{
        //    string test = table.AppendTable(append, removeRows);
        //    Assert.Equal(expected, test);
        //}

        [Theory]
        [InlineData("<row><cell>This text</cell><cell /><cell>Other text</cell></row>", "Other text", 3)]
        [InlineData("<row rowType=\"Test\"><cell>This text</cell><cell /><cell>Other text</cell></row>", "nonsense", -1)]
        [InlineData("<row><cell>This text</cell><cell /><cell>Other text</cell></row>", "This text", 1)]
        public void FindXMLColumnByTextTests(string rowText, string searchText, int expectedRow)
        {
            Assert.Equal(expectedRow, rowText.FindXMLColumnByText(new List<string> { { searchText } }));
        }

        [Theory]
        [InlineData("<row><cell>This text</cell><cell /><cell>Other text</cell></row>", 1, true)] // allow not recognized to pass as true
        [InlineData("<row rowType='Test'><cell>56,714</cell><cell /><cell>Other text</cell></row>", 1, true)]
        [InlineData("<row><cell>This text</cell><cell /><cell>-10.3</cell></row>", 3, false)]
        [InlineData("<row><cell>This text</cell><cell /><cell>56,714.00001</cell></row>", 3, true)]
        [InlineData("<row><cell>This text</cell><cell /><cell>(714.00001)</cell></row>", 3, false)]
        [InlineData("<row><cell>This text</cell><cell /><cell>–</cell></row>", 3, true)]
        [InlineData("<row><cell>This text</cell><cell /><cell>0</cell></row>", 3, true)]
        [InlineData("<row><cell>This text</cell><cell /><cell>(0)</cell></row>", 3, false)]
        public void IsPositiveXMLColumnValueByIndexTests(string rowText, int column, bool expected)
        {
            Assert.Equal(expected, rowText.IsPositiveXMLColumnValueByIndex(column));
        }

        [Theory]
        [InlineData("<table><row rowType=\"Level1.Header\"><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>-56,714</cell></row></table>", 3, 2, 300)]
        public void FindLastPositiveXMLRowIndexTests(string tableText, int column, int removeRows, int expected)
        {
            int test = tableText.FindLastPositiveXMLRowIndex(1, column, removeRows);
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("<table><row rowType=\"Level1.Header\"><cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell></cell></row><row><cell>Fund</cell><cell></cell><cell>As at December 31,&lt;br /&gt;2021&lt;br /&gt;($)</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>56,714</cell></row><row><cell>Disciplined Bond (iAIM)</cell><cell></cell><cell>-56,714</cell></row></table>", 3)]
        [InlineData("<row rowType='Test'><cell>56,714</cell><cell /><cell>Other text</cell></row>", 3)]
        [InlineData("<cell></cell><cell>Aggregate Value of Securities on Loan</cell><cell /><cell /><cell></cell>", 5)]
        public void XMLColumnCountTests(string tableText, int expected)
        {
            int test = tableText.XMLColumnCount();
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData(18252, "zyz")]
        [InlineData(1, "a")]
        [InlineData(0, null)]
        [InlineData(-1, null)]
        [InlineData(26, "z")]
        [InlineData(27, "aa")]
        [InlineData(50, "ax")]
        public void ToBase26Tests(int number, string expected)
        {
            string test = number.ToBase26();
            Assert.Equal(expected, test);

        }

        [Theory]
        [InlineData("zyz", 18252)]
        [InlineData("a", 1)]
        [InlineData(null, -1)]
        [InlineData("", -1)]
        [InlineData("z", 26)]
        [InlineData("aa", 27)]
        [InlineData("ax", 50)]
        public void FromBase26Tests(string number, int expected)
        {
            int test = number.FromBase26();
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("a", "a")]
        [InlineData("{", "a")]
        [InlineData("|", "b")]
        [InlineData("{|i", "abi")]
        public void Wrap26Tests(string current, string expected)
        {
            string test = current.Wrap26();
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("a", 'a', "a")]
        [InlineData("a", 'i', "{")]
        [InlineData("b", 'i', "|")]
        [InlineData("abi", 'i', "{|i")]
        public void Unrap26Tests(string current, char startChar, string expected)
        {
            string test = current.UnWrap26(startChar);
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("a", "a", "z", "b")]
        [InlineData("z", "a", "z", null)]
        [InlineData(null, "i", "ig", "i")]
        [InlineData("a", "i", "ig", "b")]
        [InlineData("g", "i", "ig", "ii")]
        [InlineData("gg", "ii", "iig", "iii")]
        [InlineData("A", "ii", "iig", null)]
        public void IncrementFieldLetterTests(string current, string first, string last, string expected)
        {
            string test = current.IncrementFieldLetter(first, last);
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("A", "A", "Z", true, 'A', "B")]
        [InlineData("A", "B", "BA", true, 'A', "BB")]
        [InlineData("BZ", "B", "BA", true, 'A', "BA")]
        [InlineData("A", "B", "BB", true, 'a', null)]
        [InlineData("ig", "i", "ih", false, 'a', "ih")]
        [InlineData("ih", "i", "ih", false, 'a', null)]
        public void IncrementFieldLetterAdvancedTests(string current, string first, string last, bool restricted, char startLetter, string expected)
        {
            string test = current.IncrementFieldLetter(first, last, restricted, startLetter);
            Assert.Equal(expected, test);
        }

        [Theory]
        [InlineData("M11a", "M11")]
        [InlineData("FF45ab45", "FF45")]
        [InlineData(null, null)]
        [InlineData("", "")]
        public void PartialFieldNameTests(string fieldName, string expected)
        {
            string test = fieldName.GetFieldNamePrefix();
            Assert.Equal(expected, test);
        }

    }
}

