using Publisher_Data_Operations;
using Publisher_Data_Operations.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Xml.Serialization;
using Xunit;
using Publisher_Data_Operations_Tests.Resources;
using System.Xml;

namespace Publisher_Data_Operations_Tests
{
    public class GenericTests
    {
        Generic genericTests;
        RowIdentity row;

        public GenericTests()
        {
            genericTests = new Generic("", null);

            row = new RowIdentity(13, 1003, 4, "53453");

            genericTests.ClientID = row.ClientID;
            genericTests.DocTypeID = row.DocumentTypeID;
            genericTests.LobID = row.LOBID;
                
            genericTests.ClientTranslation = TestHelpers.LoadTestData(PUB_Data_Integration_DEV.pdi_Client_Translation_Language, "pdi_Client_Translation_Language");
  
            genericTests.MissingFrench = new DataTable("MissingFrench");
            genericTests.MissingFrench.Columns.Add("Missing_ID", typeof(Guid));
            genericTests.MissingFrench.Columns.Add("Client_ID", typeof(int));
            genericTests.MissingFrench.Columns.Add("LOB_ID", typeof(int));
            genericTests.MissingFrench.Columns.Add("Document_Type_ID", typeof(int));
            genericTests.MissingFrench.Columns.Add("en-CA", typeof(string));

            var t = Guid.NewGuid();
            genericTests.MissingFrench.Rows.Add(t, row.ClientID, row.LOBID, row.DocumentTypeID, "Dummy Value");

            genericTests.MissingFrenchDetails = new DataTable("MissingFrenchDetails");
            genericTests.MissingFrenchDetails.Columns.Add("Missing_ID", typeof(Guid));
            genericTests.MissingFrenchDetails.Columns.Add("Job_ID", typeof(int));
            genericTests.MissingFrenchDetails.Columns.Add("Document_Number", typeof(string));
            genericTests.MissingFrenchDetails.Columns.Add("Field_Name", typeof(string));

            genericTests.MissingFrenchDetails.Rows.Add(t, 888, "tdoc", "tfield");


            genericTests.GlobalTextLanguage = TestHelpers.LoadTestData(PUB_Data_Integration_DEV.pdi_Global_Text_Language, "pdi_Global_Text_Language");
        }

        [Theory]
        [InlineData("01/10/2019", "October 1, 2019", "1<sup>er</sup> octobre 2019")]
        [InlineData("08/07/2020", "July 8, 2020", "8 juillet 2020")]
        [InlineData("8/07/2020", "July 8, 2020", "8 juillet 2020")]
        [InlineData("08/7/2020", "Invalid Date Format", "Format de date non valide")]
        [InlineData("33/07/2020", "Invalid Date Format", "Format de date non valide")]
        public void isLongDateFormat(string date, string expectedEnglish, string expectedFrench)
        {
            string[] output = genericTests.longFormDate(date);
            //string test = System.DateTime.Parse("2021-04-20").ToString("MMM yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR"));
            //string test2 = System.DateTime.Parse("2021-08-25").ToString("MMM yyyy", System.Globalization.CultureInfo.CreateSpecificCulture("fr-FR"));
            Assert.Equal(expectedEnglish, output[0]);
            Assert.Equal(expectedFrench, output[1]);
        }
 
        [Theory]
        [InlineData("OTHER ASSETS LESS LIABILITIES (-2.15%)", "AUTRES ACTIFS, MOINS LES PASSIFS (-2,15\u00A0%)")] // remember to put non breaking spaces in the French percent
        [InlineData("ASSET-BACKED SECURITIES (14.28%)", "MISSING FRENCH: ASSET-BACKED SECURITIES (14,28\u00A0%)")]
        [InlineData("July 8, 2020", "8 juillet 2020")]
        [InlineData("Information Technology –  0.73%", "TECHNOLOGIES DE L’INFORMATION –  0,73\u00A0%")]
        [InlineData("Information Technology –  $0.73", "TECHNOLOGIES DE L’INFORMATION –  0,73\u00A0$")]
        [InlineData("SHORT-TERM INVESTMENTS (1,01 %)", "MISSING FRENCH: SHORT-TERM INVESTMENTS (ERROR\u00A0%)")]
        public void searchFrenchTests(string en, string expectedFrench)
        {
            Tuple<string, string> fr = genericTests.SearchFrench(row, en, 888, "fieldname");
            Assert.Equal(expectedFrench, fr.Item2);
        }

        
    }
}
