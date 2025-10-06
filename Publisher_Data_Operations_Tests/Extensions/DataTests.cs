namespace Publisher_Data_Operations_Tests.Extensions
{
    using Publisher_Data_Operations.Extensions;
    using System;
    using Xunit;
    using System.Data;
    using System.Collections.Generic;
    using Publisher_Data_Operations.Helper;
    using Publisher_Data_Operations;

    public static class DataTests
    {
        //[Fact]
        //public static void CanCallGetPartialColumnStringValue()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var column = "TestValue2009884848";
        //    var result = dr.GetPartialColumnStringValue(column);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetPartialColumnStringValueWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetPartialColumnStringValue("TestValue802276318"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallGetPartialColumnStringValueWithInvalidColumn(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataRow(new DataRowBuilder()).GetPartialColumnStringValue(value));
        //}

        //[Fact]
        //public static void CanCallGetExactColumnStringValue()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var column = "TestValue2028163716";
        //    var result = dr.GetExactColumnStringValue(column);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetExactColumnStringValueWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetExactColumnStringValue("TestValue1095186367"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallGetExactColumnStringValueWithInvalidColumn(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataRow(new DataRowBuilder()).GetExactColumnStringValue(value));
        //}

        //[Fact]
        //public static void CanCallGetStringValue()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var col = 462429054;
        //    var result = dr.GetStringValue(col);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetStringValueWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetStringValue(844784566));
        //}

        //[Fact]
        //public static void CanCallGetPartialColumnIntValue()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var column = "TestValue1250497752";
        //    var result = dr.GetPartialColumnIntValue(column);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetPartialColumnIntValueWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetPartialColumnIntValue("TestValue68756321"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallGetPartialColumnIntValueWithInvalidColumn(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataRow(new DataRowBuilder()).GetPartialColumnIntValue(value));
        //}

        //[Fact]
        //public static void CanCallGetExactColumnIntValue()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var column = "TestValue502566238";
        //    var result = dr.GetExactColumnIntValue(column);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetExactColumnIntValueWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetExactColumnIntValue("TestValue496535202"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallGetExactColumnIntValueWithInvalidColumn(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataRow(new DataRowBuilder()).GetExactColumnIntValue(value));
        //}

        //[Fact]
        //public static void CanCallGetIntValue()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var col = 2126682745;
        //    var result = dr.GetIntValue(col);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetIntValueWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetIntValue(1866410141));
        //}

        //[Fact]
        //public static void CanCallGetExactColumnFFDocAge()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var column = "TestValue229309462";
        //    var result = dr.GetExactColumnFFDocAge(column);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetExactColumnFFDocAgeWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetExactColumnFFDocAge("TestValue178558278"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallGetExactColumnFFDocAgeWithInvalidColumn(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataRow(new DataRowBuilder()).GetExactColumnFFDocAge(value));
        //}

        //[Fact]
        //public static void CanCallGetPartialColumnBoolValue()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var column = "TestValue916166761";
        //    var result = dr.GetPartialColumnBoolValue(column);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetPartialColumnBoolValueWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetPartialColumnBoolValue("TestValue1421044834"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallGetPartialColumnBoolValueWithInvalidColumn(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataRow(new DataRowBuilder()).GetPartialColumnBoolValue(value));
        //}

        //[Fact]
        //public static void CanCallGetBoolValue()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var col = 1545054240;
        //    var result = dr.GetBoolValue(col);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetBoolValueWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetBoolValue(975239022));
        //}

        //[Fact]
        //public static void CanCallFindDataRowColumn()
        //{
        //    var row = new DataRow(new DataRowBuilder());
        //    var column = "TestValue685999446";
        //    var result = row.FindDataRowColumn(column);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallFindDataRowColumnWithNullRow()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).FindDataRowColumn("TestValue1093420528"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallFindDataRowColumnWithInvalidColumn(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataRow(new DataRowBuilder()).FindDataRowColumn(value));
        //}

        //[Fact]
        //public static void CanCallFindDataTableColumn()
        //{
        //    var tbl = new DataTable();
        //    var column = "TestValue1922677624";
        //    var result = tbl.FindDataTableColumn(column);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallFindDataTableColumnWithNullTbl()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataTable).FindDataTableColumn("TestValue1307079513"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallFindDataTableColumnWithInvalidColumn(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataTable().FindDataTableColumn(value));
        //}

        //[Fact]
        //public static void CanCallGetChangedColumnsWithDataRow()
        //{
        //    var row = new DataRow(new DataRowBuilder());
        //    var result = row.GetChangedColumns();
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetChangedColumnsWithDataRowWithNullRow()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetChangedColumns());
        //}

        //[Fact]
        //public static void CanCallGetChangedColumnsWithRows()
        //{
        //    var rows = new[] { new DataRow(new DataRowBuilder()), new DataRow(new DataRowBuilder()), new DataRow(new DataRowBuilder()) };
        //    var result = rows.GetChangedColumns();
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetChangedColumnsWithRowsWithNullRows()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(IEnumerable<DataRow>).GetChangedColumns());
        //}

        //[Fact]
        //public static void CanCallGetChangedColumnsWithTable()
        //{
        //    var table = new DataTable();
        //    var result = table.GetChangedColumns();
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetChangedColumnsWithTableWithNullTable()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataTable).GetChangedColumns());
        //}

        //[Fact]
        //public static void CanCallRowHasChanged()
        //{
        //    var row = new DataRow(new DataRowBuilder());
        //    var result = row.RowHasChanged();
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallRowHasChangedWithNullRow()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).RowHasChanged());
        //}

        //[Fact]
        //public static void CanCallRemoveBlankColumns()
        //{
        //    var dt = new DataTable();
        //    var minColumns = 1281675784;
        //    var result = dt.RemoveBlankColumns(minColumns);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallRemoveBlankColumnsWithNullDt()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataTable).RemoveBlankColumns(2064061909));
        //}

        //[Fact]
        //public static void CanCallRemoveBlankColumnsAtEnd()
        //{
        //    var dt = new DataTable();
        //    var sourceHasRowType = false;
        //    var formatColumnName = "TestValue77433239";
        //    var result = dt.RemoveBlankColumnsAtEnd(sourceHasRowType, formatColumnName);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallRemoveBlankColumnsAtEndWithNullDt()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataTable).RemoveBlankColumnsAtEnd(false, "TestValue1470820470"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallRemoveBlankColumnsAtEndWithInvalidFormatColumnName(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataTable().RemoveBlankColumnsAtEnd(true, value));
        //}

        //[Fact]
        //public static void CanCallRemoveBlankRows()
        //{
        //    var dt = new DataTable();
        //    var result = dt.RemoveBlankRows();
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallRemoveBlankRowsWithNullDt()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataTable).RemoveBlankRows());
        //}

        //[Fact]
        //public static void CanCallValidateXML()
        //{
        //    var dt = new DataTable();
        //    var log = new Logger(new object(), new PDIFile("TestValue833150983"));
        //    var result = dt.ValidateXML(log);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallValidateXMLWithNullDt()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataTable).ValidateXML(new Logger(new object(), new PDIFile("TestValue376129089"))));
        //}

        //[Fact]
        //public static void CannotCallValidateXMLWithNullLog()
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataTable().ValidateXML(default(Logger)));
        //}

        //[Fact]
        //public static void CanCallToCSV()
        //{
        //    var dtTable = new DataTable();
        //    var result = dtTable.ToCSV();
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallToCSVWithNullDtTable()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataTable).ToCSV());
        //}

        //[Fact]
        //public static void CanCallGetDataRowDictionary()
        //{
        //    var dr = new DataRow(new DataRowBuilder());
        //    var docFields = new Dictionary<string, string>();
        //    var result = dr.GetDataRowDictionary(docFields);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallGetDataRowDictionaryWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => default(DataRow).GetDataRowDictionary(new Dictionary<string, string>()));
        //}

        //[Fact]
        //public static void CannotCallGetDataRowDictionaryWithNullDocFields()
        //{
        //    Assert.Throws<ArgumentNullException>(() => new DataRow(new DataRowBuilder()).GetDataRowDictionary(default(Dictionary<string, string>)));
        //}

        //[Fact]
        //public static void CanCallReplaceByDataRow()
        //{
        //    var input = "TestValue247715286";
        //    var dr = new DataRow(new DataRowBuilder());
        //    var gen = new Generic(new object(), new Logger(new object(), new PDIFile("TestValue1790255365")));
        //    var isFrench = false;
        //    var naValue = "TestValue1420993834";
        //    var result = input.ReplaceByDataRow(dr, gen, isFrench, naValue);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public static void CannotCallReplaceByDataRowWithNullDr()
        //{
        //    Assert.Throws<ArgumentNullException>(() => "TestValue630470779".ReplaceByDataRow(default(DataRow), new Generic(new object(), new Logger(new object(), new PDIFile("TestValue1726492092"))), true, "TestValue581137787"));
        //}

        //[Fact]
        //public static void CannotCallReplaceByDataRowWithNullGen()
        //{
        //    Assert.Throws<ArgumentNullException>(() => "TestValue899886210".ReplaceByDataRow(new DataRow(new DataRowBuilder()), default(Generic), true, "TestValue1301662719"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallReplaceByDataRowWithInvalidInput(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => value.ReplaceByDataRow(new DataRow(new DataRowBuilder()), new Generic(new object(), new Logger(new object(), new PDIFile("TestValue786940629"))), false, "TestValue297075184"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public static void CannotCallReplaceByDataRowWithInvalidNaValue(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => "TestValue1457852132".ReplaceByDataRow(new DataRow(new DataRowBuilder()), new Generic(new object(), new Logger(new object(), new PDIFile("TestValue552473722"))), false, value));
        //}

        [Fact]
        public static void CanCallXMLtoDataTable()
        {
            string xmlString = "<table><row><cell>value</cell></row></table>";
            DataTable dt = xmlString.XMLtoDataTable();
            Assert.True(dt.Rows.Count > 0);
        }

        [Theory]
        [InlineData("<table testAttrib=\"Test\"><row testRowAttrib=\"TestRow\"><cell>Fund</cell><cell>Subsidiary</cell><cell>Ownership</cell><cell>Principal Place of Business</cell></row><row><cell>Infrastructure Fund</cell><cell>FCC Infrastructur US Inc.</cell><cell>100%</cell><cell>Delaware, United States</cell></row><row><cell>Infrastructure Fund</cell><cell>FCC Infrastructur US Inc.</cell><cell>100%</cell><cell /></row></table>")]
        [InlineData("<table><row><cell>Investor Series - Net Assets per Unit(1)</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Period ended June 30 2022, and the year(s) ended December 31</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>  </cell></row><row><cell></cell><cell>June 30,&lt;br /&gt;2022</cell><cell>Dec. 31,&lt;br /&gt;2021</cell><cell>Dec. 31,&lt;br /&gt;2020</cell><cell>Dec. 31,&lt;br /&gt;2019</cell><cell>Dec. 31,&lt;br /&gt;2018</cell><cell>Dec. 31,&lt;br /&gt;2017</cell><cell></cell></row><row><cell>Net assets per unit, beginning of period(2)</cell><cell>$10.79</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell></cell></row><row><cell>Increase (decrease) from operations:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Total revenue</cell><cell>0.20</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell></cell></row><row><cell>Total expenses</cell><cell>(0.10)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell></cell></row><row><cell>Realized gains (losses)</cell><cell>0.23</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell></cell></row><row><cell>Unrealized gains (losses)</cell><cell>(0.76)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell></cell></row><row><cell>Total increase (decrease) from operations(2)</cell><cell>$(0.43)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell></cell></row><row><cell>Distributions to unitholders:</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>From net investment income (excluding dividends)</cell><cell>(0.08)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell></cell></row><row><cell>From dividends</cell><cell>(0.01)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell></cell></row><row><cell>From capital gains</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell></cell></row><row><cell>Return of capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell></cell></row><row><cell>Total annual distributions(2),(3) </cell><cell>$(0.09)</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell></cell></row><row><cell>Net assets per unit, end of period(2)</cell><cell>$10.27</cell><cell>$10.79</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell>$–</cell><cell></cell></row></table>")]
        [InlineData("<table><row rowtype=\"Level1.Header\"><cell>Actif net par action du Fonds ($)&lt;sup&gt;1&lt;/sup&gt;</cell><cell>03/31</cell><cell>03/31</cell><cell>03/31</cell><cell>03/31</cell><cell>03/31</cell></row><row rowtype=\"Level1.Subheader\"><cell>MISSING FRENCH: Series 19</cell><cell>2022</cell><cell>2021</cell><cell>2020</cell><cell>2019</cell><cell>2018</cell></row><row><cell>MISSING FRENCH: Net Assets, beginning of period</cell><cell>21.84</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row rowtype=\"Level1.Subheader\"><cell>&lt;strong&gt;Augmentation (diminution) liée aux activités:&lt;/strong&gt;</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Total du revenu</cell><cell>0.69</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>MISSING FRENCH: Total expenses</cell><cell>(0.67)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Gains (pertes) réalisés pour la période</cell><cell>0.89</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Gains (pertes) non réalisés pour la période</cell><cell>2.16</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row rowtype=\"Level1.Total\"><cell>&lt;strong&gt;Augmentation (diminution) totale liée aux activités&lt;sup&gt;2&lt;/sup&gt;&lt;/strong&gt;</cell><cell>3.07</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row rowtype=\"Level2.Subheader\"><cell>&lt;strong&gt;Dividendes&#160;:&lt;/strong&gt;</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row><cell>Total des charges (excluant les distributions)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Dividendes&lt;sup&gt;4&lt;/sup&gt;</cell><cell>(0.72)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Gains en capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row><cell>Remboursement de capital</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row RowType=\"Level1.Total\"><cell>&lt;strong&gt;Total des dividendes&lt;sup&gt;3&lt;/sup&gt;&lt;/strong&gt;</cell><cell>(0.72)</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row><row rowtype=\"Level1.Total\"><cell>&lt;strong&gt;Actif net à la fin de la période&lt;/strong&gt;</cell><cell>24.08</cell><cell>–</cell><cell>–</cell><cell>–</cell><cell>–</cell></row></table>")]
        [InlineData("<table><row sourceDocument=\"50428219\" timeStamp=\"2022-07-29\"><cell>&lt;strong&gt;As at June 30, 2022&lt;/strong&gt;</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row><row rowtype=\"Level1.Header\" sourceDocument=\"50428219\" timeStamp=\"2022-07-29\"><cell>&lt;strong&gt;Fund&lt;/strong&gt;</cell><cell>&lt;strong&gt;Settlement &lt;br /&gt;Date&lt;/strong&gt;</cell><cell>&lt;strong&gt;Number of Contracts&lt;/strong&gt;</cell><cell></cell><cell>&lt;strong&gt;To Purchase ($)&lt;/strong&gt;</cell><cell></cell><cell>&lt;strong&gt;To Sell &lt;br /&gt;(S)&lt;/strong&gt;</cell><cell>&lt;strong&gt;Unrealized &lt;br /&gt;Appreciation (Depreciation) CAD $&lt;/strong&gt;</cell><cell>&lt;strong&gt;Credit &lt;br /&gt;Rating of Counterparty&lt;/strong&gt;</cell></row><row sourceDocument=\"50428941\" timeStamp=\"2022-07-29\"><cell>Global True Conviction</cell><cell>2022-07-05</cell><cell>1</cell><cell>USD</cell><cell>11</cell><cell>CAD</cell><cell>14</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428483\" timeStamp=\"2022-07-29\"><cell>International Disciplined Equity (iAIM)</cell><cell>2022-07-05</cell><cell>1</cell><cell>USD</cell><cell>48</cell><cell>CAD</cell><cell>61</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428483\" timeStamp=\"2022-07-29\"><cell>International Disciplined Equity (iAIM)</cell><cell>2022-07-05</cell><cell>1</cell><cell>GBP</cell><cell>19</cell><cell>CAD</cell><cell>30</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428483\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>2</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>–</cell><cell></cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell>International Equity</cell><cell>2022-07-05</cell><cell>1</cell><cell>USD</cell><cell>1,855</cell><cell>CAD</cell><cell>2,389</cell><cell>5</cell><cell>A</cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell>International Equity</cell><cell>2022-07-05</cell><cell>1</cell><cell>USD</cell><cell>111</cell><cell>CAD</cell><cell>143</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell>International Equity</cell><cell>2022-07-05</cell><cell>1</cell><cell>GBP</cell><cell>45</cell><cell>CAD</cell><cell>70</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>3</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>5</cell><cell></cell></row><row /><row sourceDocument=\"50428219\" timeStamp=\"2022-07-29\"><cell>Short Term Bond</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>155</cell><cell>USD</cell><cell>120</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"50428104\" timeStamp=\"2022-07-29\"><cell>Bond</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>1,959</cell><cell>USD</cell><cell>1,521</cell><cell>(3)</cell><cell>A</cell></row><row sourceDocument=\"50428104\" timeStamp=\"2022-07-29\"><cell>Bond</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>1,376</cell><cell>USD</cell><cell>1,069</cell><cell>(2)</cell><cell>A</cell></row><row sourceDocument=\"50428104\" timeStamp=\"2022-07-29\"><cell>Bond</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>979</cell><cell>USD</cell><cell>760</cell><cell>(1)</cell><cell>A</cell></row><row sourceDocument=\"50428104\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>3</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>(6)</cell><cell></cell></row><row sourceDocument=\"50428474\" timeStamp=\"2022-07-29\"><cell>Disciplined Bond (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>584</cell><cell>USD</cell><cell>454</cell><cell>(1)</cell><cell>A</cell></row><row sourceDocument=\"50428125\" timeStamp=\"2022-07-29\"><cell>Canadian Corporate Bond</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>114</cell><cell>USD</cell><cell>89</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"50428942\" timeStamp=\"2022-07-29\"><cell>Fixed Income Managed Portfolio</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>164</cell><cell>USD</cell><cell>127</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"50428249\" timeStamp=\"2022-07-29\"><cell>Diversified Security</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>36,995</cell><cell>USD</cell><cell>28,719</cell><cell>(53)</cell><cell>A</cell></row><row sourceDocument=\"50428249\" timeStamp=\"2022-07-29\"><cell>Diversified Security</cell><cell>2022-07-28</cell><cell>2</cell><cell>CAD</cell><cell>33,433</cell><cell>USD</cell><cell>25,950</cell><cell>(43)</cell><cell>A</cell></row><row sourceDocument=\"50428249\" timeStamp=\"2022-07-29\"><cell>Diversified Security</cell><cell>2022-07-28</cell><cell>2</cell><cell>CAD</cell><cell>12,645</cell><cell>USD</cell><cell>9,816</cell><cell>(18)</cell><cell>A</cell></row><row sourceDocument=\"50428249\" timeStamp=\"2022-07-29\"><cell>Diversified Security</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>157</cell><cell>USD</cell><cell>122</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"50428249\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>6</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>(114)</cell><cell></cell></row><row sourceDocument=\"50428149\" timeStamp=\"2022-07-29\"><cell>Diversified</cell><cell>2022-07-28</cell><cell>2</cell><cell>CAD</cell><cell>55,456</cell><cell>USD</cell><cell>43,061</cell><cell>(93)</cell><cell>A</cell></row><row sourceDocument=\"50428149\" timeStamp=\"2022-07-29\"><cell>Diversified</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>50,785</cell><cell>USD</cell><cell>39,425</cell><cell>(73)</cell><cell>A</cell></row><row sourceDocument=\"50428149\" timeStamp=\"2022-07-29\"><cell>Diversified</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>33,459</cell><cell>USD</cell><cell>25,974</cell><cell>(48)</cell><cell>A</cell></row><row sourceDocument=\"50428149\" timeStamp=\"2022-07-29\"><cell>Diversified</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>29,168</cell><cell>USD</cell><cell>22,640</cell><cell>(38)</cell><cell>A</cell></row><row sourceDocument=\"50428149\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>5</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>(252)</cell><cell></cell></row><row sourceDocument=\"50428252\" timeStamp=\"2022-07-29\"><cell>Diversified Opportunity</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>9,192</cell><cell>USD</cell><cell>7,136</cell><cell>(13)</cell><cell>A</cell></row><row sourceDocument=\"50428252\" timeStamp=\"2022-07-29\"><cell>Diversified Opportunity</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>9,020</cell><cell>USD</cell><cell>7,001</cell><cell>(12)</cell><cell>A</cell></row><row sourceDocument=\"50428252\" timeStamp=\"2022-07-29\"><cell>Diversified Opportunity</cell><cell>2022-07-28</cell><cell>2</cell><cell>CAD</cell><cell>3,709</cell><cell>USD</cell><cell>2,880</cell><cell>(6)</cell><cell>A</cell></row><row sourceDocument=\"50428252\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>4</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>(31)</cell><cell></cell></row><row sourceDocument=\"428300\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation Security (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>6,035</cell><cell>USD</cell><cell>4,685</cell><cell>(9)</cell><cell>A</cell></row><row sourceDocument=\"428300\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation Security (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>5,348</cell><cell>USD</cell><cell>4,151</cell><cell>(7)</cell><cell>A</cell></row><row sourceDocument=\"428300\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation Security (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>177</cell><cell>USD</cell><cell>137</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428300\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>3</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>(16)</cell><cell></cell></row><row sourceDocument=\"428309\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>5,526</cell><cell>USD</cell><cell>4,290</cell><cell>(8)</cell><cell>A</cell></row><row sourceDocument=\"428309\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>4,765</cell><cell>USD</cell><cell>3,698</cell><cell>(6)</cell><cell>A</cell></row><row sourceDocument=\"428309\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>2,806</cell><cell>USD</cell><cell>2,179</cell><cell>(4)</cell><cell>A</cell></row><row sourceDocument=\"428310\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation Opportunity (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>2,430</cell><cell>USD</cell><cell>1,886</cell><cell>(4)</cell><cell>A</cell></row><row sourceDocument=\"428310\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation Opportunity (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>793</cell><cell>USD</cell><cell>616</cell><cell>(1)</cell><cell>A</cell></row><row sourceDocument=\"428310\" timeStamp=\"2022-07-29\"><cell>Global Asset Allocation Opportunity (iAIM)</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>254</cell><cell>USD</cell><cell>198</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428314\" timeStamp=\"2022-07-29\"><cell>Canadian Equity Growth</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>9,073</cell><cell>USD</cell><cell>7,042</cell><cell>(12)</cell><cell>A</cell></row><row sourceDocument=\"50428939\" timeStamp=\"2022-07-29\"><cell>North American Equity</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>13,485</cell><cell>USD</cell><cell>10,468</cell><cell>(19)</cell><cell>A</cell></row><row sourceDocument=\"50428941\" timeStamp=\"2022-07-29\"><cell>Global True Conviction</cell><cell>2022-07-04</cell><cell>1</cell><cell>CAD</cell><cell>15</cell><cell>GBP</cell><cell>9</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"50428941\" timeStamp=\"2022-07-29\"><cell>Global True Conviction</cell><cell>2022-07-04</cell><cell>1</cell><cell>CAD</cell><cell>25</cell><cell>JPY</cell><cell>2,666</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"50428941\" timeStamp=\"2022-07-29\"><cell>Global True Conviction</cell><cell>2022-07-05</cell><cell>1</cell><cell>CAD</cell><cell>27</cell><cell>JPY</cell><cell>2,861</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"50428941\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>3</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>–</cell><cell></cell></row><row sourceDocument=\"428483\" timeStamp=\"2022-07-29\"><cell>International Disciplined Equity (iAIM)</cell><cell>2022-07-04</cell><cell>1</cell><cell>CAD</cell><cell>49</cell><cell>JPY</cell><cell>5,236</cell><cell>(1)</cell><cell>A</cell></row><row sourceDocument=\"428483\" timeStamp=\"2022-07-29\"><cell>International Disciplined Equity (iAIM)</cell><cell>2022-07-04</cell><cell>1</cell><cell>CAD</cell><cell>68</cell><cell>GBP</cell><cell>43</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428483\" timeStamp=\"2022-07-29\"><cell>International Disciplined Equity (iAIM)</cell><cell>2022-07-05</cell><cell>1</cell><cell>CAD</cell><cell>35</cell><cell>JPY</cell><cell>3,737</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428483\" timeStamp=\"2022-07-29\"><cell>International Disciplined Equity (iAIM)</cell><cell>2022-07-05</cell><cell>1</cell><cell>CAD</cell><cell>31</cell><cell>USD</cell><cell>24</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428483\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>4</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>(1)</cell><cell></cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell>International Equity</cell><cell>2022-07-04</cell><cell>1</cell><cell>CAD</cell><cell>109</cell><cell>JPY</cell><cell>11,588</cell><cell>(1)</cell><cell>A</cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell>International Equity</cell><cell>2022-07-04</cell><cell>1</cell><cell>CAD</cell><cell>157</cell><cell>GBP</cell><cell>100</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell>International Equity</cell><cell>2022-07-05</cell><cell>1</cell><cell>CAD</cell><cell>84</cell><cell>JPY</cell><cell>8,814</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell>International Equity</cell><cell>2022-07-05</cell><cell>1</cell><cell>CAD</cell><cell>72</cell><cell>USD</cell><cell>56</cell><cell>–</cell><cell>A</cell></row><row sourceDocument=\"428344\" timeStamp=\"2022-07-29\"><cell></cell><cell></cell><cell>4</cell><cell></cell><cell></cell><cell></cell><cell></cell><cell>(1)</cell><cell></cell></row><row sourceDocument=\"428216\" timeStamp=\"2022-07-29\"><cell>Dividend</cell><cell>2022-07-28</cell><cell>1</cell><cell>CAD</cell><cell>42,509</cell><cell>USD</cell><cell>33,000</cell><cell>(61)</cell><cell>A</cell></row></table>")]
        [InlineData("<table><row/></table>")]
        [InlineData("<table><row rowType=\"Level1.SubHeader\" /><row rowType=\"Level1.Header\"><cell/><cell> </cell></row></table>")]
        [InlineData("<table><row><cell>HSBC Canadian Bond Fund - Investor Series - Net Assets per Unit<sup>(1)</sup></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell><cell></cell></row></table>")]
        [InlineData("<table/>")]
        public static void XMLtoDataTableAndBackTests(string xmlString)
        {
            string xmlTestString = xmlString.ReplaceCI("<cell></cell>", "<cell />"); // the output code currently uses self closing cells so convert any completely empty cells to self closing for the comparison
            xmlTestString = xmlTestString.ReplaceCI("<row/>", "<row />").ReplaceCI("<cell/>", "<cell />");
            PDI_DataTable dt = xmlString.XMLtoDataTable();
            string xml = dt.DataTabletoXML();
            Assert.Equal(xmlTestString, xml);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public static void XMLtoDataTableWithInvalidXmlStringReturnsNull(string value)
        {
            Assert.Null(value.XMLtoDataTable());
        }
    }

    public class PDI_DataTableTests
    {
        private PDI_DataTable _testClass;
        private string _tableName;
        private string _tableNamespace;
        private DataTable _dt;

        public PDI_DataTableTests()
        {
            _tableName = "TestValue19918901";
            _tableNamespace = "TestValue1969199446";
            _dt = new DataTable();
            _testClass = new PDI_DataTable(_tableName, _tableNamespace);
        }

        [Fact]
        public void CanConstruct()
        {
            var instance = new PDI_DataTable();
            Assert.NotNull(instance);
            instance = new PDI_DataTable(_tableName);
            Assert.NotNull(instance);
            instance = new PDI_DataTable(_tableName, _tableNamespace);
            Assert.NotNull(instance);
            instance = new PDI_DataTable(_dt);
            Assert.NotNull(instance);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("TableName")]
        public void ConstructWithTableNameReturnsTableName(string value)
        {
            Assert.Equal(value, new PDI_DataTable(value).TableName);
            
        }

        [Fact]
        public void CanConvertDataTable()
        {
            for (int i = 0; i < 1000; i++)
            {
                DataTable dt = TestHelpers.RandomTable(i % 2 == 0);
                PDI_DataTable pd1 = new PDI_DataTable(dt);
                Assert.True(TestHelpers.TablesEqual(dt, pd1));
            }
        }
    }
}