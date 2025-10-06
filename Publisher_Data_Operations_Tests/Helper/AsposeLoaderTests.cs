using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Publisher_Data_Operations.Helper;

namespace Publisher_Data_Operations_Tests.Helper
{
    public class AsposeLoaderTests
    {
        AsposeLoader asposeLoader;

        public AsposeLoaderTests()
        {
            asposeLoader = new AsposeLoader("");
        }

        [Theory]
        [InlineData("Trailing Commission Table\n(Header)\nRow 1_E30b", "TrailingCommissionTable(Header)Row01_E30b")]
        [InlineData("Trailing Commission Table\n(Header)\nRow 10_E30b", "TrailingCommissionTable(Header)Row10_E30b")]
        [InlineData("Custom Text French\nField 9_E209c_FR", "CustomTextFrenchField09_E209c_FR")]
        [InlineData("Current year\nminus 8 years\nreturn_FP9", "CurrentYearMinus08YearsReturn_FP9")]
        [InlineData("FundCode_<FundCode>", "FundCode")]
        
        public void getColumnNameTests(string value, string expected)
        {
            Assert.Equal(expected, AsposeLoader.GetColumnName(value));
        }

   
        [Theory]
        [InlineData("Trailing Commission Table\n(Header)\nRow 1_E30b", 1)]
        [InlineData("Trailing Commission Table\n(Header)\nRow 10_E30b", 10)]
        [InlineData("Custom Text French\nField 9_E209c_FR", 9)]
        [InlineData("Current year\nminus 8 years\nreturn_FP9", 8)]
        [InlineData("FundCode_<FundCode>", 0)]

        public void getRowNumberTests(string value, int expected)
        {
            Assert.Equal(expected, AsposeLoader.GetNumberFromName(value));
        }

        [Theory]
        [InlineData("A3", true, 2, 0)]
        [InlineData("I7", true, 6, 8)]
        [InlineData("7I", false, -1, -1)]
        [InlineData("A3:I7", false, -1, -1)]
        [InlineData("A1C1", false, -1, -1)]
        public void convertAddressTests(string address, bool success, int row, int col)
        {
            bool res = asposeLoader.ConvertAddress(address, out int rowOut, out int colOut);
            Assert.Equal(success, res);
            Assert.Equal(row, rowOut);
            Assert.Equal(col, colOut);
        }

        [Theory]
        [InlineData(2, 0, "A3")]
        [InlineData(-1, 0, "A")]
        [InlineData(1, -1, "2")]
        [InlineData(-1, -1, null)]
        public void convertIndextoAddress(int row, int col, string address)
        {
            Assert.Equal(address, asposeLoader.ConvertIndexToAddress(row, col));
        }
    }
}
