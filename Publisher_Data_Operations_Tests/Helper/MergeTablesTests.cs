namespace Publisher_Data_Operations_Tests.Helper
{
    using Publisher_Data_Operations.Helper;
    using System;
    using Xunit;
    using Publisher_Data_Operations.Extensions;
    using System.Data;
    using System.Xml;
    using System.Collections.Generic;
    using System.Linq;

    public class MergeTablesSettingsTests
    {
        private MergeTablesSettings _testClass;
        private int _checkRow;
        private int _checkCol;
        private int _desiredRows;
        private int _desiredColumns;
        private string _interimIndicator;
        private int _interimOffset;

        public MergeTablesSettingsTests()
        {
            _checkRow = 1457362;
            _checkCol = 1518743494;
            _desiredRows = 62023840;
            _desiredColumns = 1089103889;
            _interimIndicator = "Something";
            _interimOffset = -1;
            _testClass = new MergeTablesSettings(_checkRow, _checkCol, _desiredRows, _desiredColumns, _interimIndicator, _interimOffset);
        }

        [Fact]
        public void CanConstruct()
        {
            var instance = new MergeTablesSettings(_checkRow, _checkCol, _desiredRows, _desiredColumns, _interimIndicator, _interimOffset);
            Assert.NotNull(instance);
        }

        [Fact]
        public void CanSetAndGetCheckField()
        {
            var testValue = new Tuple<int, int>(164263676, 1712675577);
            _testClass.CheckField = testValue;
            Assert.Equal(testValue, _testClass.CheckField);
        }

        [Fact]
        public void DesiredRowsIsInitializedCorrectly()
        {
            Assert.Equal(_desiredRows, _testClass.DesiredRows);
        }

        [Fact]
        public void CanSetAndGetDesiredRows()
        {
            var testValue = 1232352110;
            _testClass.DesiredRows = testValue;
            Assert.Equal(testValue, _testClass.DesiredRows);
        }

        [Fact]
        public void DesiredColumnsIsInitializedCorrectly()
        {
            Assert.Equal(_desiredColumns, _testClass.DesiredColumns);
        }

        [Fact]
        public void CanSetAndGetInterimIndicator()
        {
            var testValue = "Test_FSDFS_123423";
            _testClass.InterimIndicator = testValue;
            Assert.Equal(testValue, _testClass.InterimIndicator);
        }

        [Fact]
        public void InterimIndicatorIsInitializedCorrectly()
        {
            Assert.Equal(_interimIndicator, _testClass.InterimIndicator);
        }

        [Fact]
        public void CanSetAndGetInterimOffset()
        {
            var testValue = 1767183365;
            _testClass.InterimOffset = testValue;
            Assert.Equal(testValue, _testClass.InterimOffset);
        }

        [Fact]
        public void InterimOffsetIsInitializedCorrectly()
        {
            Assert.Equal(_interimOffset, _testClass.InterimOffset);
        }

        [Fact]
        public void CanSetAndGetDesiredColumns()
        {
            var testValue = 1767183365;
            _testClass.DesiredColumns = testValue;
            Assert.Equal(testValue, _testClass.DesiredColumns);
        }
    }

    public class MergeTablesTests
    {
        private MergeTables _testClass;
        private int _checkRow;
        private int _checkCol;
        private int _desiredRows;
        private int _desiredColumns;
        private string _interimIndicator;
        private int _interimOffset;
        private MergeTablesSettings _mergeSettings;

        public MergeTablesTests()
        {
            _checkRow = 6997592;
            _checkCol = 2141873666;
            _desiredRows = 850339174;
            _desiredColumns = 1051132671;
            _interimIndicator = "Nothing";
            _interimOffset = -1;
            _mergeSettings = new MergeTablesSettings(353842193, 332329746, 1334789478, 879394889, "Text", -1);
            _testClass = new MergeTables(_checkRow, _checkCol, _desiredRows, _desiredColumns, _interimIndicator, _interimOffset);
        }

        public class MergeTablesTestsData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] {

                    "<table><row><cell>Ratios and Supplemental Data</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell></cell><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018</cell><cell>2017</cell></row><row><cell>Total net asset value (in 000s)(4)</cell><cell>$113,134</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Number of units outstanding (in 000s)(4)</cell><cell>9,333</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Management expense ratio (&quot;MER&quot;)(5)</cell><cell>1.14%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>MER before waivers or absorptions(5)</cell><cell>1.14%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>Trading expense ratio(6)</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell></row><row><cell>Portfolio turnover rate(7)</cell><cell>20.88%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>Total net asset value per unit(4)</cell><cell>$12.12</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row></table>"
                    , "M12a"
                    , "<table><row><cell>Ratios and Supplemental Data</cell><cell /><cell /><cell /><cell /><cell /></row><row><cell /><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018</cell></row><row><cell>Total net asset value (in 000s)(4)</cell><cell>$113,134</cell><cell>$150,003</cell><cell>$190,204</cell><cell>$173,750</cell><cell>$175,288</cell></row><row><cell>Number of units outstanding (in 000s)(4)</cell><cell>9,333</cell><cell>10,955</cell><cell>13,253</cell><cell>12,656</cell><cell>13,292</cell></row><row><cell>Management expense ratio (&quot;MER&quot;)(5)</cell><cell>1.14%</cell><cell>1.15%</cell><cell>1.15%</cell><cell>1.16%</cell><cell>1.14%</cell></row><row><cell>MER before waivers or absorptions(5)</cell><cell>1.14%</cell><cell>1.15%</cell><cell>1.15%</cell><cell>1.16%</cell><cell>1.14%</cell></row><row><cell>Trading expense ratio(6)</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell></row><row><cell>Portfolio turnover rate(7)</cell><cell>20.88%</cell><cell>52.26%</cell><cell>59.59%</cell><cell>31.61%</cell><cell>43.39%</cell></row><row><cell>Total net asset value per unit(4)</cell><cell>$12.12</cell><cell>$13.69</cell><cell>$14.35</cell><cell>$13.73</cell><cell>$13.19</cell></row></table>"
                };
                yield return new object[] { // M11a - Keep header values from current - extra columns in current dropped  the %- in history are kept in the next column

                    "<table><row><cell>HSBC Canadian Bond Fund - Investor Series - Net Assets per Unit<sup>(1)</sup></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Year(s) ended Some Date</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell></cell><cell>2023</cell><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018 DROPPED</cell></row><row><cell>Net assets per unit, beginning of period(2)</cell><cell>$14.69</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Increase (decrease) from operations:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Total revenue</cell><cell>0.16</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total expenses</cell><cell>(0.06)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Realized gains (losses)</cell><cell>(0.16)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Unrealized gains (losses)</cell><cell>(1.44)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total increase (decrease) from operations(2)</cell><cell>$(1.50)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Distributions to unitholders:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>From net investment income (excluding dividends)</cell><cell>(0.11)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From dividends</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From capital gains</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Return of capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total annual distributions(2, 3)</cell><cell>$(0.11)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Net assets per unit, end of period(2)</cell><cell>$121.12</cell><cell>$99.99</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row></table>"
                    , "M11a"
                    , "<table><row><cell>HSBC Canadian Bond Fund - Investor Series - Net Assets per Unit<sup>(1)</sup></cell><cell /><cell /><cell /><cell /><cell /></row><row><cell>Year(s) ended Some Date</cell><cell /><cell /><cell /><cell /><cell /></row><row><cell /><cell>2023</cell><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell></row><row><cell>Net assets per unit, beginning of period(2)</cell><cell>$14.69</cell><cell>$–</cell><cell>$14.35</cell><cell>$13.73</cell><cell>$13.19</cell></row><row><cell>Increase (decrease) from operations:</cell><cell /><cell /><cell /><cell /><cell /></row><row><cell>Total revenue</cell><cell>0.16</cell><cell>–</cell><cell>0.38</cell><cell>0.40</cell><cell>0.42</cell></row><row><cell>Total expenses</cell><cell>(0.06)</cell><cell>–</cell><cell>(0.16)</cell><cell>(0.16)</cell><cell>(0.16)</cell></row><row><cell>Realized gains (losses)</cell><cell>(0.16)</cell><cell>–</cell><cell>0.05</cell><cell>0.42</cell><cell>0.08</cell></row><row><cell>Unrealized gains (losses)</cell><cell>(1.44)</cell><cell>–</cell><cell>(0.74)</cell><cell>0.47</cell><cell>0.49</cell></row><row><cell>Total increase (decrease) from operations(2)</cell><cell>$(1.50)</cell><cell>$–</cell><cell>$(0.47)</cell><cell>$1.13</cell><cell>$0.83</cell></row><row><cell>Distributions to unitholders:</cell><cell /><cell /><cell /><cell /><cell /></row><row><cell>From net investment income (excluding dividends)</cell><cell>(0.11)</cell><cell>–</cell><cell>(0.23)</cell><cell>(0.25)</cell><cell>(0.27)</cell></row><row><cell>From dividends</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From capital gains</cell><cell>–</cell><cell>–</cell><cell>(0.02)</cell><cell>(0.27)</cell><cell>(0.01)</cell></row><row><cell>Return of capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total annual distributions(2, 3)</cell><cell>$(0.11)</cell><cell>$–</cell><cell>$(0.25)</cell><cell>$(0.52)</cell><cell>$(0.28)</cell></row><row><cell>Net assets per unit, end of period(2)</cell><cell>$121.12</cell><cell>$–</cell><cell>$13.69</cell><cell>$14.35</cell><cell>$13.73</cell></row></table>"
                };
                yield return new object[] { // M11a -  extra columns in current dropped - rerun but keep the headers from incoming - the %- in history are updated with the new values

                    "<table><row><cell>HSBC Canadian Bond Fund - Investor Series - Net Assets per Unit<sup>(1)</sup></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Year(s) ended Some Date</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell></cell><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018 Custom Header</cell><cell>2017 DROPPED</cell></row><row><cell>Net assets per unit, beginning of period(2)</cell><cell>$14.69</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Increase (decrease) from operations:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Total revenue</cell><cell>0.16</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total expenses</cell><cell>(0.06)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Realized gains (losses)</cell><cell>(0.16)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Unrealized gains (losses)</cell><cell>(1.44)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total increase (decrease) from operations(2)</cell><cell>$(1.50)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Distributions to unitholders:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>From net investment income (excluding dividends)</cell><cell>(0.11)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From dividends</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From capital gains</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Return of capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total annual distributions(2, 3)</cell><cell>$(0.11)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Net assets per unit, end of period(2)</cell><cell>$121.12</cell><cell>$99.99</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row></table>"
                    , "M11a"
                    , "<table><row><cell>HSBC Canadian Bond Fund - Investor Series - Net Assets per Unit<sup>(1)</sup></cell><cell /><cell /><cell /><cell /><cell /></row><row><cell>Year(s) ended Some Date</cell><cell /><cell /><cell /><cell /><cell /></row><row><cell /><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018 Custom Header</cell></row><row><cell>Net assets per unit, beginning of period(2)</cell><cell>$14.69</cell><cell>$14.35</cell><cell>$13.73</cell><cell>$13.19</cell><cell>$13.51</cell></row><row><cell>Increase (decrease) from operations:</cell><cell /><cell /><cell /><cell /><cell /></row><row><cell>Total revenue</cell><cell>0.16</cell><cell>0.38</cell><cell>0.40</cell><cell>0.42</cell><cell>0.44</cell></row><row><cell>Total expenses</cell><cell>(0.06)</cell><cell>(0.16)</cell><cell>(0.16)</cell><cell>(0.16)</cell><cell>(0.15)</cell></row><row><cell>Realized gains (losses)</cell><cell>(0.16)</cell><cell>0.05</cell><cell>0.42</cell><cell>0.08</cell><cell>0.02</cell></row><row><cell>Unrealized gains (losses)</cell><cell>(1.44)</cell><cell>(0.74)</cell><cell>0.47</cell><cell>0.49</cell><cell>(0.34)</cell></row><row><cell>Total increase (decrease) from operations(2)</cell><cell>$(1.50)</cell><cell>$(0.47)</cell><cell>$1.13</cell><cell>$0.83</cell><cell>$(0.03)</cell></row><row><cell>Distributions to unitholders:</cell><cell /><cell /><cell /><cell /><cell /></row><row><cell>From net investment income (excluding dividends)</cell><cell>(0.11)</cell><cell>(0.23)</cell><cell>(0.25)</cell><cell>(0.27)</cell><cell>(0.29)</cell></row><row><cell>From dividends</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From capital gains</cell><cell>–</cell><cell>(0.02)</cell><cell>(0.27)</cell><cell>(0.01)</cell><cell>(0.03)</cell></row><row><cell>Return of capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total annual distributions(2, 3)</cell><cell>$(0.11)</cell><cell>$(0.25)</cell><cell>$(0.52)</cell><cell>$(0.28)</cell><cell>$(0.32)</cell></row><row><cell>Net assets per unit, end of period(2)</cell><cell>$121.12</cell><cell>$13.69</cell><cell>$14.35</cell><cell>$13.73</cell><cell>$13.19</cell></row></table>"
                };
                yield return new object[] {

                    "<table><row><cell>Mar.&lt;br&gt;2023</cell><cell>8.05</cell></row></table>" // annual current with interim historic with < 11 rows
                    , "M15i"
                    , "<table><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2014&lt;sup&gt;†&lt;/sup&gt;</cell><cell>17.4</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2015</cell><cell>4.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2016</cell><cell>-5.7</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2017</cell><cell>15.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2018</cell><cell>-0.1</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2019</cell><cell>6.4</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2020</cell><cell>-10.7</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2021</cell><cell>23.9</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt;2022</cell><cell>10.2</cell></row><row><cell>Mar.&lt;br&gt;2023</cell><cell>8.05</cell></row></table>"
                };
                yield return new object[] {

                    "<table><row><cell>Sep. 31&lt;br&gt;2022</cell><cell>8.05</cell></row></table>" // interim rerun - not detected as a rerun because the incoming data has a day and the historic doesn't
                    , "M15i"
                    , "<table><row rowtype=\"\"><cell>Dec.&lt;br&gt; 2012</cell><cell>9.1</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2014&lt;sup&gt;†&lt;/sup&gt;</cell><cell>17.4</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2015</cell><cell>4.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2016</cell><cell>-5.7</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2017</cell><cell>15.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2018</cell><cell>-0.1</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2019</cell><cell>6.4</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2020</cell><cell>-10.7</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2021</cell><cell>23.9</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt;2022</cell><cell>10.2</cell></row><row><cell>Sep. 31&lt;br&gt;2022</cell><cell>8.05</cell></row></table>"
                };
                yield return new object[] {

                    "<table><row rowtype=\"Level1.SubHeader\"><cell>Sep.&lt;br&gt;2022</cell><cell>8.05</cell></row></table>" // interim rerun - This one is a real rerun without the day
                    , "M15i"
                    , "<table><row rowtype=\"\"><cell>Dec.&lt;br&gt; 2012</cell><cell>9.1</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2014&lt;sup&gt;†&lt;/sup&gt;</cell><cell>17.4</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2015</cell><cell>4.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2016</cell><cell>-5.7</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2017</cell><cell>15.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2018</cell><cell>-0.1</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2019</cell><cell>6.4</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2020</cell><cell>-10.7</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2021</cell><cell>23.9</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt;2022</cell><cell>10.2</cell></row><row rowtype=\"Level1.SubHeader\"><cell>Sep.&lt;br&gt;2022</cell><cell>8.05</cell></row></table>"
                };
                yield return new object[] {

                    "<table><row><cell>Mar.&lt;br&gt;2023</cell><cell>8.05</cell></row></table>" // interim historic with annual current
                    , "M15m"
                    ,"<table><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2014&lt;sup&gt;†&lt;/sup&gt;</cell><cell>17.9</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2015</cell><cell>4.8</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2016</cell><cell>-5.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2017</cell><cell>15.8</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2018</cell><cell>0.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2019</cell><cell>6.8</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2020</cell><cell>-10.3</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt; 2021</cell><cell>24.5</cell></row><row rowtype=\"\"><cell>Mar.&lt;br&gt;2022</cell><cell>10.7</cell></row><row><cell>Mar.&lt;br&gt;2023</cell><cell>8.05</cell></row></table>"
                };
                yield return new object[] {

                    "<table><row><cell>Mar.&lt;br&gt;2023</cell><cell>8.05</cell></row></table>" // No history field - no settings field
                    , "NR999"
                    ,null
                };
                yield return new object[] {

                    "<table><row><cell>Mar.&lt;br&gt;2023</cell><cell>8.05</cell></row></table>" // No history field - return current
                    , "M12bbbbbbb"
                    ,"<table><row><cell>Mar.&lt;br&gt;2023</cell><cell>8.05</cell></row></table>"
                };
                yield return new object[] { // semi annual history - drop column and update with new

                    "<table><row><cell>Ratios and Supplemental Data</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell></cell><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018</cell><cell>2017</cell></row><row><cell>Total net asset value (in 000s)(4)</cell><cell>$113,134</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Number of units outstanding (in 000s)(4)</cell><cell>9,333</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Management expense ratio (&quot;MER&quot;)(5)</cell><cell>1.14%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>MER before waivers or absorptions(5)</cell><cell>1.14%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>Trading expense ratio(6)</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell></row><row><cell>Portfolio turnover rate(7)</cell><cell>20.88%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell><cell>0.00%</cell></row><row><cell>Total net asset value per unit(4)</cell><cell>$12.12</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row></table>"
                    , "M12c"
                    , "<table><row><cell>Ratios and Supplemental Data</cell><cell /><cell /><cell /><cell /><cell /></row><row><cell /><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018</cell></row><row><cell>Total net asset value (in 000s)(4)</cell><cell>$113,134</cell><cell>$170,299</cell><cell>$246,114</cell><cell>$174,492</cell><cell>$139,247</cell></row><row><cell>Number of units outstanding (in 000s)(4)</cell><cell>9,333</cell><cell>15,259</cell><cell>21,037</cell><cell>15,596</cell><cell>12,961</cell></row><row><cell>Management expense ratio (&quot;MER&quot;)(5)</cell><cell>1.14%</cell><cell>0.87%</cell><cell>0.88%</cell><cell>0.88%</cell><cell>0.86%</cell></row><row><cell>MER before waivers or absorptions(5)</cell><cell>1.14%</cell><cell>0.87%</cell><cell>0.88%</cell><cell>0.88%</cell><cell>0.86%</cell></row><row><cell>Trading expense ratio(6)</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell><cell>n/a</cell></row><row><cell>Portfolio turnover rate(7)</cell><cell>20.88%</cell><cell>52.26%</cell><cell>59.59%</cell><cell>31.61%</cell><cell>43.39%</cell></row><row><cell>Total net asset value per unit(4)</cell><cell>$12.12</cell><cell>$11.16</cell><cell>$11.70</cell><cell>$11.19</cell><cell>$10.74</cell></row></table>"
                };
                yield return new object[] { // M11a semi annual input is missing a column resulting in a missing header row for 2017 data

                    "<table><row><cell>HSBC Canadian Bond Fund - Investor Series - Net Assets per Unit<sup>(1)</sup></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Period ended June 30, 2022, and year(s) ended December 31</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell></cell><cell>June 30,<br />2022</cell><cell>Dec. 31,<br/>2021</cell><cell>Dec. 31,<br/>2020</cell><cell>Dec. 31,<br/>2019</cell><cell>Dec. 31,<br/>2018</cell></row><row><cell>Net assets per unit, beginning of period(2)</cell><cell>$123.69</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Increase (decrease) from operations:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Total revenue</cell><cell>0.16</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total expenses</cell><cell>(0.06)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Realized gains (losses)</cell><cell>(0.16)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Unrealized gains (losses)</cell><cell>(1.44)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total increase (decrease) from operations(2)</cell><cell>$(1.50)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Distributions to unitholders:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>From net investment income (excluding dividends)</cell><cell>(0.11)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From dividends</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From capital gains</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Return of capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total annual distributions(2, 3)</cell><cell>$(0.11)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row><row><cell>Net assets per unit, end of period(2)</cell><cell>$122.12</cell><cell>$13.69</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell></row></table>"
                    , "M11a"
                    , "<table><row><cell>HSBC Canadian Bond Fund - Investor Series - Net Assets per Unit<sup>(1)</sup></cell><cell /><cell /><cell /><cell /><cell /><cell /></row><row><cell>Period ended June 30, 2022, and year(s) ended December 31</cell><cell /><cell /><cell /><cell /><cell /><cell /></row><row><cell /><cell>June 30,<br />2022</cell><cell>Dec. 31,<br/>2021</cell><cell>Dec. 31,<br/>2020</cell><cell>Dec. 31,<br/>2019</cell><cell>Dec. 31,<br/>2018</cell><cell /></row><row><cell>Net assets per unit, beginning of period(2)</cell><cell>$123.69</cell><cell>$–</cell><cell>$14.35</cell><cell>$13.73</cell><cell>$13.19</cell><cell>$13.51</cell></row><row><cell>Increase (decrease) from operations:</cell><cell /><cell /><cell /><cell /><cell /><cell /></row><row><cell>Total revenue</cell><cell>0.16</cell><cell>–</cell><cell>0.38</cell><cell>0.40</cell><cell>0.42</cell><cell>0.44</cell></row><row><cell>Total expenses</cell><cell>(0.06)</cell><cell>–</cell><cell>(0.16)</cell><cell>(0.16)</cell><cell>(0.16)</cell><cell>(0.15)</cell></row><row><cell>Realized gains (losses)</cell><cell>(0.16)</cell><cell>–</cell><cell>0.05</cell><cell>0.42</cell><cell>0.08</cell><cell>0.02</cell></row><row><cell>Unrealized gains (losses)</cell><cell>(1.44)</cell><cell>–</cell><cell>(0.74)</cell><cell>0.47</cell><cell>0.49</cell><cell>(0.34)</cell></row><row><cell>Total increase (decrease) from operations(2)</cell><cell>$(1.50)</cell><cell>$–</cell><cell>$(0.47)</cell><cell>$1.13</cell><cell>$0.83</cell><cell>$(0.03)</cell></row><row><cell>Distributions to unitholders:</cell><cell /><cell /><cell /><cell /><cell /><cell /></row><row><cell>From net investment income (excluding dividends)</cell><cell>(0.11)</cell><cell>–</cell><cell>(0.23)</cell><cell>(0.25)</cell><cell>(0.27)</cell><cell>(0.29)</cell></row><row><cell>From dividends</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>From capital gains</cell><cell>–</cell><cell>–</cell><cell>(0.02)</cell><cell>(0.27)</cell><cell>(0.01)</cell><cell>(0.03)</cell></row><row><cell>Return of capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Total annual distributions(2, 3)</cell><cell>$(0.11)</cell><cell>$–</cell><cell>$(0.25)</cell><cell>$(0.52)</cell><cell>$(0.28)</cell><cell>$(0.32)</cell></row><row><cell>Net assets per unit, end of period(2)</cell><cell>$122.12</cell><cell>$–</cell><cell>$13.69</cell><cell>$14.35</cell><cell>$13.73</cell><cell>$13.19</cell></row></table>"
                };
                yield return new object[] {

                    "<table SeriesLetter=\"X8\" AgeCalendarYears=\"0\" AsAtDate=\"9/30/2022\" PrimaryIndex=\"Broad-based Index\" InceptionDate=\"7/14/2022\" Cycle=\"Semi-Annual\"><row rowtype=\"test\"><cell>Sep. <sup>***</sup><br />2022</cell><cell /></row></table>" //  Rerun with only semi history - but no semi value
                    , "M15x8"
                    ,"<table><row rowtype=\"test\"><cell>Sep. <sup>***</sup><br />2022</cell><cell /></row></table>"
                };
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        }

        [Fact]
        public void CanConstruct()
        {
            var instance = new MergeTables(_checkRow, _checkCol, _desiredRows, _desiredColumns, _interimIndicator, _interimOffset);
            Assert.NotNull(instance);
            instance = new MergeTables(_mergeSettings);
            Assert.NotNull(instance);
        }

        [Fact]
        public void CannotConstructWithNullMergeSettings()
        {
            Assert.Throws<ArgumentNullException>(() => new MergeTables(default(MergeTablesSettings)));
        }

        //[Fact]
        //public void CanCallLoadHistoricFieldTable()
        //{
        //    var fieldName = "TestValue1433266800";
        //    var rowIdentity = new RowIdentity();
        //    var conObject = new object();
        //    var result = _testClass.LoadHistoricFieldTable(fieldName, rowIdentity, conObject);
        //    Assert.True(false, "Create or modify test");
        //}

        [Fact]
        public void CannotCallLoadHistoricFieldTableWithNullRowIdentity()
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.LoadHistoricalData("TestValue951125034", default(RowIdentity), new DBConnection("")));
        }

        [Fact]
        public void CannotCallLoadHistoricFieldTableWithNullConObject()
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.LoadHistoricalData("TestValue1620980527", new RowIdentity(), null));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallLoadHistoricFieldTableWithInvalidFieldName(string value)
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.LoadHistoricalData(value, new RowIdentity(), new DBConnection("")));
        }

        //[Fact]
        //public void CanCallGetHistoricFieldTableWithFieldNameAndRowIdentityAndDbPub()
        //{
        //    var fieldName = "TestValue1758537763";
        //    var rowIdentity = new RowIdentity();
        //    var dbPub = new DBConnection(new object());
        //    var result = _testClass.GetHistoricFieldTable(fieldName, rowIdentity, dbPub);
        //    Assert.True(false, "Create or modify test");
        //}

        [Fact]
        public void CannotCallGetHistoricFieldTableWithFieldNameAndRowIdentityAndDbPubWithNullRowIdentity()
        {
            Assert.Throws<ArgumentException>(() => _testClass.GetHistoricFieldTable("TestValue986352881", default(RowIdentity), new DBConnection(new object())));
        }

        [Fact]
        public void CannotCallGetHistoricFieldTableWithFieldNameAndRowIdentityAndDbPubWithNullDbPub()
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.GetHistoricFieldTable("TestValue744509215", new RowIdentity(), default(DBConnection)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallGetHistoricFieldTableWithFieldNameAndRowIdentityAndDbPubWithInvalidFieldName(string value)
        {
            Assert.Throws<ArgumentException>(() => _testClass.GetHistoricFieldTable(value, new RowIdentity(), new DBConnection(new object())));
        }

        //[Fact]
        //public void CanCallGetHistoricFieldTableWithFieldName()
        //{
        //    var fieldName = "TestValue1326570425";
        //    var result = _testClass.GetHistoricFieldTable(fieldName);
        //    Assert.True(false, "Create or modify test");
        //}

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallGetHistoricFieldTableWithFieldNameWithInvalidFieldName(string value)
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.GetHistoricFieldTable(value));
        }

        [Theory]
        [InlineData("Dummy22")]
        public void GetHistoricFieldTableTests(string fieldName)
        {
            _testClass.HistoricTableData = SetupTable();
            Assert.Null(_testClass.GetHistoricFieldTable(fieldName));

        }

        [Theory]
        [ClassData(typeof(MergeTablesTestsData))]
        public void CanCallMergeTableDataWithCurrentDataTableAndFieldName(string currentDataTable, string fieldName, string expected)
        {
            MergeTables mt = new MergeTables();
            mt.HistoricTableData = SetupTable();
            mt.MergeTableSettings = SetupTableSettings();
            var result = mt.MergeTableData(currentDataTable, fieldName);


            //DataTable currentPDataTable = Transform.GetDataTableFromXmlTable(currentDataTable);
            //var historicalDataString = mt.GetHistoricFieldString(fieldName);
            //DataTable historicalDataTable = Transform.GetDataTableFromXmlTable(historicalDataString);

            //if (fieldName.Contains("M11"))
            //    Transform.MergeM11Data(historicalDataTable, currentPDataTable);

            //if (fieldName.Contains("M12"))
            //    Transform.MergeM12Data(currentPDataTable, historicalDataTable);

            //string english = Transform.BuildPdiTableXmlFromDataTable(currentPDataTable).Replace("<cell></cell>", "<cell />");
            //if (fieldName.Contains("M11") || fieldName.Contains("M12"))
            //    Assert.Equal(english, result);

            Assert.Equal(expected, result) ;
            
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallMergeTableDataWithCurrentDataTableAndFieldNameWithInvalidCurrentDataTable(string value)
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.MergeTableData(value, "TestValue279732066"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallMergeTableDataWithCurrentDataTableAndFieldNameWithInvalidFieldName(string value)
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.MergeTableData("TestValue527218813", value));
        }

        //[Fact]
        //public void CanCallMergeTableDataWithCurrentDataTableAndHistoricalDataTable()
        //{
        //    var currentDataTable = new CustomDataTable();
        //    var historicalDataTable = new CustomDataTable();
        //    _testClass.MergeTableData(currentDataTable, historicalDataTable);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public void CanCallIsRerun()
        //{
        //    var historicalDataTable = new CustomDataTable();
        //    var currentDataTable = new CustomDataTable();
        //    var result = _testClass.IsRerun(historicalDataTable, currentDataTable);
        //    Assert.True(false, "Create or modify test");
        //}

        [Fact]
        public void CannotCallIsRerunWithNullHistoricalDataTable()
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.IsRerun(default(PDI_DataTable), new PDI_DataTable()));
        }

        [Fact]
        public void CannotCallIsRerunWithNullCurrentDataTable()
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.IsRerun(new PDI_DataTable(), default(PDI_DataTable)));
        }

        [Fact]
        public void MergeSettingsIsInitializedCorrectly()
        {
            _testClass = new MergeTables(_mergeSettings);
            Assert.Equal(_mergeSettings, _testClass.MergeSettings);
        }

        [Theory]
        [InlineData("M11", true)]
        [InlineData("C1", false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        public void GetMergeFieldNamePrefixTests(string fieldName, bool exists )
        {
            _testClass.MergeTableSettings = SetupTableSettings();
            Assert.Equal(exists, _testClass.GetMergeFieldNamePrefix.Any(fieldName.Contains));
        }

        public static DataTable SetupTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Field_Name", Type.GetType("System.String"));
            dt.Columns.Add("Content", Type.GetType("System.String"));
            dt.Columns.Add("Header_Field_Name", Type.GetType("System.String"));
            dt.Columns.Add("Header_Content", Type.GetType("System.String"));
            dt.Columns.Add("DOCUMENT_NUMBER", Type.GetType("System.String"));
            dt.Columns.Add("FEED_COMPANY_ID", Type.GetType("System.Int32"));
            
            return TestHelpers.LoadXMLTable(Resources.PUB_Data_Integration_DEV.pub_Historic_Field_Data, dt); ;
        }

        public static DataTable SetupTableSettings()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Field_Name_Prefix", Type.GetType("System.String"));
            dt.Columns.Add("Check_Field_Row", Type.GetType("System.Int32"));
            dt.Columns.Add("Check_Field_Column", Type.GetType("System.Int32"));
            dt.Columns.Add("Desired_Rows", Type.GetType("System.Int32"));
            dt.Columns.Add("Desired_Columns", Type.GetType("System.Int32"));
            dt.Columns.Add("Interim_Indicator", Type.GetType("System.String"));
            dt.Columns.Add("Interim_Offset", Type.GetType("System.Int32"));

            return TestHelpers.LoadXMLTable(Resources.PUB_Data_Integration_DEV.pdi_Data_Type_Merge_Table, dt);
        }

    }
}