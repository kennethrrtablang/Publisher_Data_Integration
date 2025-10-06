using Moq;
using Newtonsoft.Json;
using Publisher_Data_Operations;
using Publisher_Data_Operations.Extensions;
using Publisher_Data_Operations.Helper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Xunit;

namespace Publisher_Data_Operations_Tests.Helper
{
    public class ExcelHelperTests
    {
        private MockRepository mockRepository;
        private Mock<PDIStream> mockPDIStream;
        private Mock<Logger> mockLogger;

        public ExcelHelperTests()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);
            this.mockPDIStream = this.mockRepository.Create<PDIStream>();
            this.mockLogger = this.mockRepository.Create<Logger>();
        }

        private ExcelHelper CreateExcelHelper()
        {
            return new ExcelHelper(
                this.mockPDIStream.Object,
                this.mockLogger.Object);
        }

        [Theory]
        [ClassData(typeof(ExcelHelperTestData))]
        public void isBrandNewFundTests(DataTable dt, string filingID, bool expected)
        {
            Assert.Equal(expected, ExcelHelper.IsBrandNewFund(dt, filingID));
        }

        public class ExcelHelperTestData : IEnumerable<object[]>
        {
            DataTable dt = SetupTable();
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] {

                    dt.Select("Document_Number = '31635FSEM' AND FundCode = '31635'").CopyToDataTable()
                    , "ETFLAUNCH2021JUNE1"
                    , false 
                
                };
                yield return new object[] {

                    dt.Select("Document_Number = '31636FS' AND FundCode = '31636'").CopyToDataTable()
                    , "ETFLAUNCH2021JUNE2"
                    , true

                };
                yield return new object[] {

                    dt.Select("Document_Number = '31637FSC' AND FundCode = '31637'").CopyToDataTable()
                    , "ETFLAUNCH2021JUNE3"
                    , true

                };
                yield return new object[] {

                    dt.Select("Document_Number = '31638FSB' AND FundCode = '31638'").CopyToDataTable()
                    , "ETFLAUNCH2021JUNE4"
                    , true

                };
                yield return new object[] {

                    dt.Select("Document_Number = '31639SB' AND FundCode = '31639'").CopyToDataTable()
                    , "ETFLAUNCH2021JUNE5"
                    , false

                };
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        }

        public static DataTable SetupTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Document_Number", Type.GetType("System.String"));
            dt.Columns.Add("FundCode", Type.GetType("System.String"));

            dt.Columns.Add("DocFilingReferenceID", Type.GetType("System.String"));
            dt.Columns.Add("DocFFDocAgeStatusID", typeof(FFDocAge));
            dt.Columns.Add("FilingReferenceID", Type.GetType("System.String"));
            dt.Columns.Add("FFDocAgeStatusID", typeof(FFDocAge));

            dt.Rows.Add("31635FSEM", "31635", null, null, "ETFLAUNCH2021JUNE1", FFDocAge.BrandNewSeries);
            dt.Rows.Add("31636FS", "31636", null, null, null, null);
            dt.Rows.Add("31637FSC", "31637", "ETFLAUNCH2021JUNE3", FFDocAge.BrandNewFund, "ETFLAUNCH2021JUNE3", FFDocAge.BrandNewFund);
            dt.Rows.Add("31638FSB", "31638", null, null, "ETFLAUNCH2021JUNE4", FFDocAge.BrandNewFund);
            dt.Rows.Add("31639SB", "31639", "SomethingElsee", FFDocAge.BrandNewFund, "SomethingElsee", FFDocAge.NewSeries);
            return dt;
        }
    }
}
