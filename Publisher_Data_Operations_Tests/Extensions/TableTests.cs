using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Publisher_Data_Operations.Extensions;

namespace Publisher_Data_Operations_Tests.Extensions
{

    public class TableTests
    {

        // basic 10 year table types
        [Theory]
        [InlineData(2020, new string[] { "", "", "", "", "", "2.5", "6.2", "3.2", "-0.5", "-6.5" }, "<table><row><cell>2015</cell><cell>2.5%</cell></row><row><cell>2016</cell><cell>6.2%</cell></row><row><cell>2017</cell><cell>3.2%</cell></row><row><cell>2018</cell><cell>-0.5%</cell></row><row><cell>2019</cell><cell>-6.5%</cell></row></table>", 5, 2)]
        [InlineData(2020, new string[] { "2.1", "2.5", "-1.6", "6.5", "3.7", "2.9", "-4.21", "5.7", "7.2", "3.5" }, "<table><row><cell>2010</cell><cell>2.10%</cell></row><row><cell>2011</cell><cell>2.50%</cell></row><row><cell>2012</cell><cell>-1.60%</cell></row><row><cell>2013</cell><cell>6.50%</cell></row><row><cell>2014</cell><cell>3.70%</cell></row><row><cell>2015</cell><cell>2.90%</cell></row><row><cell>2016</cell><cell>-4.21%</cell></row><row><cell>2017</cell><cell>5.70%</cell></row><row><cell>2018</cell><cell>7.20%</cell></row><row><cell>2019</cell><cell>3.50%</cell></row></table>", 10, 2)]
        [InlineData(2020, new string[] { "", "", "", "", "", "", "", "", "", "" }, "<table></table>", 0, 0)]
        public void assembleTenYearTableTests(int filingYear, string[] yearData, string expectedString, int expectedYears, int expectedNegative)
        {

            //create the necessary TableList from the string data
            TableList tl = new TableList(filingYear);
            foreach (string s in yearData)
                tl.AddValidation(s);


            Assert.Equal(expectedString, tl.GetTableString(out int calcCalYears, out int calcNegYears));
            Assert.Equal(expectedYears, calcCalYears);
            Assert.Equal(expectedNegative, calcNegYears);
        }

        // tab 16 table type
        [Theory]
        [InlineData(new string[] { "Power Financial Corp. ", "Bank of Nova Scotia ", "Loblaw Companies Ltd. ", "Canadian Imperial Bank of Commerce ", "AltaGas Ltd. " },
            new string[] { "Corporation Financière Power ", "Banque de Nouvelle-Écosse ", "Les Compagnies Loblaw ltée ", "Banque Canadienne Impériale de Commerce ", "AltaGas Ltd. " },
            new string[] { "3.5", "3.2", "3.17", "3.14", "3.04" },
            "<table><row><cell>Power Financial Corp.</cell><cell>3.50%</cell></row><row><cell>Bank of Nova Scotia</cell><cell>3.20%</cell></row><row><cell>Loblaw Companies Ltd.</cell><cell>3.17%</cell></row><row><cell>Canadian Imperial Bank of Commerce</cell><cell>3.14%</cell></row><row><cell>AltaGas Ltd.</cell><cell>3.04%</cell></row></table>",
            "<table><row><cell>Corporation Financière Power</cell><cell>3,50\u00A0%</cell></row><row><cell>Banque de Nouvelle-Écosse</cell><cell>3,20\u00A0%</cell></row><row><cell>Les Compagnies Loblaw ltée</cell><cell>3,17\u00A0%</cell></row><row><cell>Banque Canadienne Impériale de Commerce</cell><cell>3,14\u00A0%</cell></row><row><cell>AltaGas Ltd.</cell><cell>3,04\u00A0%</cell></row></table>")]
        public void assemble16TableTests(string[] enData, string[] frData, string[] valData, string expectedEnglishString, string expectedFrenchString)
        {

            //create the necessary TableList from the string data
            TableList tl = new TableList();
            for (int i = 0; i < enData.Length; i++)
                tl.AddValidation(valData[i], enData[i], frData[i]);

            Assert.Equal(expectedEnglishString, tl.GetTableString());
            Assert.Equal(expectedFrenchString, tl.GetTableStringFrench());
        }

        // tab 17 table type
        [Theory]
        [InlineData(new string[] { "Money Market Securities", "Corporations", "Municipalities and Semi-Public Institutions", "Provincial Governments and Crown Corporations", "Canadian Bonds" },
            new string[] { "Titres de marché monétaire", "Sociétés", "Municipalités et institutions parapubliques", "Gouvernements et sociétés publiques des provinces", "Obligations canadiennes" },
            new string[] { "82.2", "74", "4.4", "3.8", "17.8" },
            new string[] { "1", "2", "2", "2", "1" },
            "<table><row><cell>Money Market Securities</cell><cell>82.2%</cell><cell /></row><row><cell>Corporations</cell><cell /><cell>74.0%</cell></row><row><cell>Municipalities and Semi-Public Institutions</cell><cell /><cell>4.4%</cell></row><row><cell>Provincial Governments and Crown Corporations</cell><cell /><cell>3.8%</cell></row><row><cell>Canadian Bonds</cell><cell>17.8%</cell><cell /></row></table>",
            "<table><row><cell>Titres de marché monétaire</cell><cell>82,2\u00A0%</cell><cell /></row><row><cell>Sociétés</cell><cell /><cell>74,0\u00A0%</cell></row><row><cell>Municipalités et institutions parapubliques</cell><cell /><cell>4,4\u00A0%</cell></row><row><cell>Gouvernements et sociétés publiques des provinces</cell><cell /><cell>3,8\u00A0%</cell></row><row><cell>Obligations canadiennes</cell><cell>17,8\u00A0%</cell><cell /></row></table>")]
        [InlineData(new string[] { }, new string[] { }, new string[] { }, new string[] { }, "<table></table>", "<table></table>")]
        public void assemble17TableTests(string[] enData, string[] frData, string[] valData, string[] levelData, string expectedEnglishString, string expectedFrenchString)
        {

            //create the necessary TableList from the string data
            TableList tl = new TableList();
            for (int i = 0; i < enData.Length; i++)
                tl.AddValidation(valData[i], enData[i], frData[i], null, levelData[i]);

            Assert.Equal(expectedEnglishString, tl.GetTableString());
            Assert.Equal(expectedFrenchString, tl.GetTableStringFrench());
        }

        // tab Number of Investments
        [Theory]
        [InlineData(new string[] { "Fixed Income", "Equity Assets", "Total Number of Investments" },
            new string[] { "Revenu fixe", "Actions", "Nombre total de placements" },
            new string[] { "38", "41", "79" },
            new string[] { "2", "2", "1" },
            "<table><row><cell>Total Number of Investments</cell><cell>79</cell></row><row><cell>Equity Assets</cell><cell>41</cell></row><row><cell>Fixed Income</cell><cell>38</cell></row></table>",
            "<table><row><cell>Nombre total de placements</cell><cell>79</cell></row><row><cell>Actions</cell><cell>41</cell></row><row><cell>Revenu fixe</cell><cell>38</cell></row></table>")]
        [InlineData(new string[] { }, new string[] { }, new string[] { }, new string[] { }, "<table></table>", "<table></table>")]
        public void assembleNumberOfInvestmentsTableTests(string[] enData, string[] frData, string[] valData, string[] levelData, string expectedEnglishString, string expectedFrenchString)
        {
            //create the necessary TableList from the string data
            TableList tl = new TableList(TableTypes.Number);
            for (int i = 0; i < enData.Length; i++)
                tl.AddValidation(valData[i], enData[i], frData[i], null, levelData[i]);
            string tableString = tl.GetTableString();
            Assert.Equal(expectedEnglishString, tableString);
            tableString = tl.GetTableStringFrench();
            Assert.Equal(expectedFrenchString, tableString);
        }

        // tab Distributions

        [Theory]
        [InlineData(new string[] { "English Header", "October 2020", "November 2020", "December 2020", "English Header", "October 2020", "November 2020", "December 2020" },
                new string[] { "French Header", "october 2020", "november 2020", "december 2020", "French Header", "october 2020", "november 2020", "december 2020" },
                new string[] { "F", "N/A", "0.249", "N/A", "F5", "0.033", "0.033", "0.033" },
                new string[] { "-1", "2", "1", "0", "-1", "2", "1", "0" },
                "<table><row><cell>English Header</cell><cell>F</cell><cell>F5</cell></row><row><cell>October 2020</cell><cell>-</cell><cell>0.033</cell></row><row><cell>November 2020</cell><cell>0.249</cell><cell>0.033</cell></row><row><cell>December 2020</cell><cell>-</cell><cell>0.033</cell></row></table>",
                "<table><row><cell>French Header</cell><cell>F</cell><cell>F5</cell></row><row><cell>october 2020</cell><cell>-</cell><cell>0.033</cell></row><row><cell>november 2020</cell><cell>0.249</cell><cell>0.033</cell></row><row><cell>december 2020</cell><cell>-</cell><cell>0.033</cell></row></table>")]
        [InlineData(new string[] { }, new string[] { }, new string[] { }, new string[] { }, "<table></table>", "<table></table>")]
        public void assembleDistributionsTableTests(string[] enData, string[] frData, string[] valData, string[] rowData, string expectedEnglishString, string expectedFrenchString)
        {
            //create the necessary TableList from the string data
            TableList tl = new TableList("-");
            for (int i = 0; i < enData.Length; i++)
                tl.AddValidation(valData[i], enData[i], frData[i], rowData[i]);
            string tableString = tl.GetTableString();
            Assert.Equal(expectedEnglishString, tableString);
            tableString = tl.GetTableStringFrench();
            Assert.Equal(expectedFrenchString, tableString);
        }

        // tab 10K Value - uses shortMonths - updated to not use short months

        [Theory]
        /*[InlineData(new string[] { "10112", "10005", "9987", "9948", "9906", "9887", "10012", "10138", "10266", "10395", "10526", "10658", "10792", "10928", "10926", "10924", "10922", "10920", "10917", "10915", "10913", "10911", "10909", "10907", "11014", "11121", "11230", "11341", "11452", "11564", "10138", "10266", "10395", "10526", "10658", "10792", "10928", "10926", "10924", "10922", "10920", "10917", "11028", "11025", "11136", "11133", "11245", "11242", "11355", "11352", "11466", "11463", "11579", "11489", "11346", "11094", "10000", "10125", "10168", "10132", "10149", "10226", "10204", "10167", "10157", "10112", "10005", "9987", "9948", "9906", "9887", "10012", "10138", "10266", "10395", "10526", "10658", "10792", "10928", "10926", "10924", "10922", "10920", "10917", "10915", "10913", "10911", "10909", "10907", "11014", "11121", "11230", "11341", "11452", "11564", "10138", "10266", "10395", "10526", "10658", "10792", "10928", "10926", "10924", "10922", "10920" },
         new string[] { "02/2010", "03/2010", "04/2010", "05/2010", "06/2010", "07/2010", "08/2010", "09/2010", "10/2010", "11/2010", "12/2010", "01/2011", "02/2011", "03/2011", "04/2011", "05/2011", "06/2011", "07/2011", "08/2011", "09/2011", "10/2011", "11/2011", "12/2011", "01/2012", "02/2012", "03/2012", "04/2012", "05/2012", "06/2012", "07/2012", "08/2012", "09/2012", "10/2012", "11/2012", "12/2012", "01/2013", "02/2013", "03/2013", "04/2013", "05/2013", "06/2013", "07/2013", "08/2013", "09/2013", "10/2013", "11/2013", "12/2013", "01/2014", "02/2014", "03/2014", "04/2014", "05/2014", "06/2014", "07/2014", "08/2014", "09/2014", "10/2014", "11/2014", "12/2014", "01/2015", "02/2015", "03/2015", "04/2015", "05/2015", "06/2015", "07/2015", "08/2015", "09/2015", "10/2015", "11/2015", "12/2015", "01/2016", "02/2016", "03/2016", "04/2016", "05/2016", "06/2016", "07/2016", "08/2016", "09/2016", "10/2016", "11/2016", "12/2016", "01/2017", "02/2017", "03/2017", "04/2017", "05/2017", "06/2017", "07/2017", "08/2017", "09/2017", "10/2017", "11/2017", "12/2017", "01/2018", "02/2018", "03/2018", "04/2018", "05/2018", "06/2018", "07/2018", "08/2018", "09/2018", "10/2018", "11/2018" },
         "<table><row><cell>Feb. 2010</cell><cell>$10,112</cell><cell>Y</cell></row><row><cell>Mar. 2010</cell><cell>$10,005</cell><cell></cell></row><row><cell>Apr. 2010</cell><cell>$9,987</cell><cell></cell></row><row><cell>May. 2010</cell><cell>$9,948</cell><cell></cell></row><row><cell>June. 2010</cell><cell>$9,906</cell><cell></cell></row><row><cell>July. 2010</cell><cell>$9,887</cell><cell></cell></row><row><cell>Aug. 2010</cell><cell>$10,012</cell><cell></cell></row><row><cell>Sept. 2010</cell><cell>$10,138</cell><cell></cell></row><row><cell>Oct. 2010</cell><cell>$10,266</cell><cell></cell></row><row><cell>Nov. 2010</cell><cell>$10,395</cell><cell></cell></row><row><cell>Dec. 2010</cell><cell>$10,526</cell><cell>Y</cell></row><row><cell>Jan. 2011</cell><cell>$10,658</cell><cell></cell></row><row><cell>Feb. 2011</cell><cell>$10,792</cell><cell></cell></row><row><cell>Mar. 2011</cell><cell>$10,928</cell><cell></cell></row><row><cell>Apr. 2011</cell><cell>$10,926</cell><cell></cell></row><row><cell>May. 2011</cell><cell>$10,924</cell><cell></cell></row><row><cell>June. 2011</cell><cell>$10,922</cell><cell></cell></row><row><cell>July. 2011</cell><cell>$10,920</cell><cell></cell></row><row><cell>Aug. 2011</cell><cell>$10,917</cell><cell></cell></row><row><cell>Sept. 2011</cell><cell>$10,915</cell><cell></cell></row><row><cell>Oct. 2011</cell><cell>$10,913</cell><cell></cell></row><row><cell>Nov. 2011</cell><cell>$10,911</cell><cell></cell></row><row><cell>Dec. 2011</cell><cell>$10,909</cell><cell>Y</cell></row><row><cell>Jan. 2012</cell><cell>$10,907</cell><cell></cell></row><row><cell>Feb. 2012</cell><cell>$11,014</cell><cell></cell></row><row><cell>Mar. 2012</cell><cell>$11,121</cell><cell></cell></row><row><cell>Apr. 2012</cell><cell>$11,230</cell><cell></cell></row><row><cell>May. 2012</cell><cell>$11,341</cell><cell></cell></row><row><cell>June. 2012</cell><cell>$11,452</cell><cell></cell></row><row><cell>July. 2012</cell><cell>$11,564</cell><cell></cell></row><row><cell>Aug. 2012</cell><cell>$10,138</cell><cell></cell></row><row><cell>Sept. 2012</cell><cell>$10,266</cell><cell></cell></row><row><cell>Oct. 2012</cell><cell>$10,395</cell><cell></cell></row><row><cell>Nov. 2012</cell><cell>$10,526</cell><cell></cell></row><row><cell>Dec. 2012</cell><cell>$10,658</cell><cell>Y</cell></row><row><cell>Jan. 2013</cell><cell>$10,792</cell><cell></cell></row><row><cell>Feb. 2013</cell><cell>$10,928</cell><cell></cell></row><row><cell>Mar. 2013</cell><cell>$10,926</cell><cell></cell></row><row><cell>Apr. 2013</cell><cell>$10,924</cell><cell></cell></row><row><cell>May. 2013</cell><cell>$10,922</cell><cell></cell></row><row><cell>June. 2013</cell><cell>$10,920</cell><cell></cell></row><row><cell>July. 2013</cell><cell>$10,917</cell><cell></cell></row><row><cell>Aug. 2013</cell><cell>$11,028</cell><cell></cell></row><row><cell>Sept. 2013</cell><cell>$11,025</cell><cell></cell></row><row><cell>Oct. 2013</cell><cell>$11,136</cell><cell></cell></row><row><cell>Nov. 2013</cell><cell>$11,133</cell><cell></cell></row><row><cell>Dec. 2013</cell><cell>$11,245</cell><cell>Y</cell></row><row><cell>Jan. 2014</cell><cell>$11,242</cell><cell></cell></row><row><cell>Feb. 2014</cell><cell>$11,355</cell><cell></cell></row><row><cell>Mar. 2014</cell><cell>$11,352</cell><cell></cell></row><row><cell>Apr. 2014</cell><cell>$11,466</cell><cell></cell></row><row><cell>May. 2014</cell><cell>$11,463</cell><cell></cell></row><row><cell>June. 2014</cell><cell>$11,579</cell><cell></cell></row><row><cell>July. 2014</cell><cell>$11,489</cell><cell></cell></row><row><cell>Aug. 2014</cell><cell>$11,346</cell><cell></cell></row><row><cell>Sept. 2014</cell><cell>$11,094</cell><cell></cell></row><row><cell>Oct. 2014</cell><cell>$10,000</cell><cell></cell></row><row><cell>Nov. 2014</cell><cell>$10,125</cell><cell></cell></row><row><cell>Dec. 2014</cell><cell>$10,168</cell><cell>Y</cell></row><row><cell>Jan. 2015</cell><cell>$10,132</cell><cell></cell></row><row><cell>Feb. 2015</cell><cell>$10,149</cell><cell></cell></row><row><cell>Mar. 2015</cell><cell>$10,226</cell><cell></cell></row><row><cell>Apr. 2015</cell><cell>$10,204</cell><cell></cell></row><row><cell>May. 2015</cell><cell>$10,167</cell><cell></cell></row><row><cell>June. 2015</cell><cell>$10,157</cell><cell></cell></row><row><cell>July. 2015</cell><cell>$10,112</cell><cell></cell></row><row><cell>Aug. 2015</cell><cell>$10,005</cell><cell></cell></row><row><cell>Sept. 2015</cell><cell>$9,987</cell><cell></cell></row><row><cell>Oct. 2015</cell><cell>$9,948</cell><cell></cell></row><row><cell>Nov. 2015</cell><cell>$9,906</cell><cell></cell></row><row><cell>Dec. 2015</cell><cell>$9,887</cell><cell>Y</cell></row><row><cell>Jan. 2016</cell><cell>$10,012</cell><cell></cell></row><row><cell>Feb. 2016</cell><cell>$10,138</cell><cell></cell></row><row><cell>Mar. 2016</cell><cell>$10,266</cell><cell></cell></row><row><cell>Apr. 2016</cell><cell>$10,395</cell><cell></cell></row><row><cell>May. 2016</cell><cell>$10,526</cell><cell></cell></row><row><cell>June. 2016</cell><cell>$10,658</cell><cell></cell></row><row><cell>July. 2016</cell><cell>$10,792</cell><cell></cell></row><row><cell>Aug. 2016</cell><cell>$10,928</cell><cell></cell></row><row><cell>Sept. 2016</cell><cell>$10,926</cell><cell></cell></row><row><cell>Oct. 2016</cell><cell>$10,924</cell><cell></cell></row><row><cell>Nov. 2016</cell><cell>$10,922</cell><cell></cell></row><row><cell>Dec. 2016</cell><cell>$10,920</cell><cell>Y</cell></row><row><cell>Jan. 2017</cell><cell>$10,917</cell><cell></cell></row><row><cell>Feb. 2017</cell><cell>$10,915</cell><cell></cell></row><row><cell>Mar. 2017</cell><cell>$10,913</cell><cell></cell></row><row><cell>Apr. 2017</cell><cell>$10,911</cell><cell></cell></row><row><cell>May. 2017</cell><cell>$10,909</cell><cell></cell></row><row><cell>June. 2017</cell><cell>$10,907</cell><cell></cell></row><row><cell>July. 2017</cell><cell>$11,014</cell><cell></cell></row><row><cell>Aug. 2017</cell><cell>$11,121</cell><cell></cell></row><row><cell>Sept. 2017</cell><cell>$11,230</cell><cell></cell></row><row><cell>Oct. 2017</cell><cell>$11,341</cell><cell></cell></row><row><cell>Nov. 2017</cell><cell>$11,452</cell><cell></cell></row><row><cell>Dec. 2017</cell><cell>$11,564</cell><cell>Y</cell></row><row><cell>Jan. 2018</cell><cell>$10,138</cell><cell></cell></row><row><cell>Feb. 2018</cell><cell>$10,266</cell><cell></cell></row><row><cell>Mar. 2018</cell><cell>$10,395</cell><cell></cell></row><row><cell>Apr. 2018</cell><cell>$10,526</cell><cell></cell></row><row><cell>May. 2018</cell><cell>$10,658</cell><cell></cell></row><row><cell>June. 2018</cell><cell>$10,792</cell><cell></cell></row><row><cell>July. 2018</cell><cell>$10,928</cell><cell></cell></row><row><cell>Aug. 2018</cell><cell>$10,926</cell><cell></cell></row><row><cell>Sept. 2018</cell><cell>$10,924</cell><cell></cell></row><row><cell>Oct. 2018</cell><cell>$10,922</cell><cell></cell></row><row><cell>Nov. 2018</cell><cell>$10,920</cell><cell>Y</cell></row></table>",
         "<table><row><cell>févr. 2010</cell><cell>$10,112</cell><cell>Y</cell></row><row><cell>mars 2010</cell><cell>$10,005</cell><cell></cell></row><row><cell>avr. 2010</cell><cell>$9,987</cell><cell></cell></row><row><cell>mai. 2010</cell><cell>$9,948</cell><cell></cell></row><row><cell>juin. 2010</cell><cell>$9,906</cell><cell></cell></row><row><cell>juill. 2010</cell><cell>$9,887</cell><cell></cell></row><row><cell>aout. 2010</cell><cell>$10,012</cell><cell></cell></row><row><cell>sept. 2010</cell><cell>$10,138</cell><cell></cell></row><row><cell>oct. 2010</cell><cell>$10,266</cell><cell></cell></row><row><cell>nov. 2010</cell><cell>$10,395</cell><cell></cell></row><row><cell>déc. 2010</cell><cell>$10,526</cell><cell>Y</cell></row><row><cell>janv. 2011</cell><cell>$10,658</cell><cell></cell></row><row><cell>févr. 2011</cell><cell>$10,792</cell><cell></cell></row><row><cell>mars 2011</cell><cell>$10,928</cell><cell></cell></row><row><cell>avr. 2011</cell><cell>$10,926</cell><cell></cell></row><row><cell>mai. 2011</cell><cell>$10,924</cell><cell></cell></row><row><cell>juin. 2011</cell><cell>$10,922</cell><cell></cell></row><row><cell>juill. 2011</cell><cell>$10,920</cell><cell></cell></row><row><cell>aout. 2011</cell><cell>$10,917</cell><cell></cell></row><row><cell>sept. 2011</cell><cell>$10,915</cell><cell></cell></row><row><cell>oct. 2011</cell><cell>$10,913</cell><cell></cell></row><row><cell>nov. 2011</cell><cell>$10,911</cell><cell></cell></row><row><cell>déc. 2011</cell><cell>$10,909</cell><cell>Y</cell></row><row><cell>janv. 2012</cell><cell>$10,907</cell><cell></cell></row><row><cell>févr. 2012</cell><cell>$11,014</cell><cell></cell></row><row><cell>mars 2012</cell><cell>$11,121</cell><cell></cell></row><row><cell>avr. 2012</cell><cell>$11,230</cell><cell></cell></row><row><cell>mai. 2012</cell><cell>$11,341</cell><cell></cell></row><row><cell>juin. 2012</cell><cell>$11,452</cell><cell></cell></row><row><cell>juill. 2012</cell><cell>$11,564</cell><cell></cell></row><row><cell>aout. 2012</cell><cell>$10,138</cell><cell></cell></row><row><cell>sept. 2012</cell><cell>$10,266</cell><cell></cell></row><row><cell>oct. 2012</cell><cell>$10,395</cell><cell></cell></row><row><cell>nov. 2012</cell><cell>$10,526</cell><cell></cell></row><row><cell>déc. 2012</cell><cell>$10,658</cell><cell>Y</cell></row><row><cell>janv. 2013</cell><cell>$10,792</cell><cell></cell></row><row><cell>févr. 2013</cell><cell>$10,928</cell><cell></cell></row><row><cell>mars 2013</cell><cell>$10,926</cell><cell></cell></row><row><cell>avr. 2013</cell><cell>$10,924</cell><cell></cell></row><row><cell>mai. 2013</cell><cell>$10,922</cell><cell></cell></row><row><cell>juin. 2013</cell><cell>$10,920</cell><cell></cell></row><row><cell>juill. 2013</cell><cell>$10,917</cell><cell></cell></row><row><cell>aout. 2013</cell><cell>$11,028</cell><cell></cell></row><row><cell>sept. 2013</cell><cell>$11,025</cell><cell></cell></row><row><cell>oct. 2013</cell><cell>$11,136</cell><cell></cell></row><row><cell>nov. 2013</cell><cell>$11,133</cell><cell></cell></row><row><cell>déc. 2013</cell><cell>$11,245</cell><cell>Y</cell></row><row><cell>janv. 2014</cell><cell>$11,242</cell><cell></cell></row><row><cell>févr. 2014</cell><cell>$11,355</cell><cell></cell></row><row><cell>mars 2014</cell><cell>$11,352</cell><cell></cell></row><row><cell>avr. 2014</cell><cell>$11,466</cell><cell></cell></row><row><cell>mai. 2014</cell><cell>$11,463</cell><cell></cell></row><row><cell>juin. 2014</cell><cell>$11,579</cell><cell></cell></row><row><cell>juill. 2014</cell><cell>$11,489</cell><cell></cell></row><row><cell>aout. 2014</cell><cell>$11,346</cell><cell></cell></row><row><cell>sept. 2014</cell><cell>$11,094</cell><cell></cell></row><row><cell>oct. 2014</cell><cell>$10,000</cell><cell></cell></row><row><cell>nov. 2014</cell><cell>$10,125</cell><cell></cell></row><row><cell>déc. 2014</cell><cell>$10,168</cell><cell>Y</cell></row><row><cell>janv. 2015</cell><cell>$10,132</cell><cell></cell></row><row><cell>févr. 2015</cell><cell>$10,149</cell><cell></cell></row><row><cell>mars 2015</cell><cell>$10,226</cell><cell></cell></row><row><cell>avr. 2015</cell><cell>$10,204</cell><cell></cell></row><row><cell>mai. 2015</cell><cell>$10,167</cell><cell></cell></row><row><cell>juin. 2015</cell><cell>$10,157</cell><cell></cell></row><row><cell>juill. 2015</cell><cell>$10,112</cell><cell></cell></row><row><cell>aout. 2015</cell><cell>$10,005</cell><cell></cell></row><row><cell>sept. 2015</cell><cell>$9,987</cell><cell></cell></row><row><cell>oct. 2015</cell><cell>$9,948</cell><cell></cell></row><row><cell>nov. 2015</cell><cell>$9,906</cell><cell></cell></row><row><cell>déc. 2015</cell><cell>$9,887</cell><cell>Y</cell></row><row><cell>janv. 2016</cell><cell>$10,012</cell><cell></cell></row><row><cell>févr. 2016</cell><cell>$10,138</cell><cell></cell></row><row><cell>mars 2016</cell><cell>$10,266</cell><cell></cell></row><row><cell>avr. 2016</cell><cell>$10,395</cell><cell></cell></row><row><cell>mai. 2016</cell><cell>$10,526</cell><cell></cell></row><row><cell>juin. 2016</cell><cell>$10,658</cell><cell></cell></row><row><cell>juill. 2016</cell><cell>$10,792</cell><cell></cell></row><row><cell>aout. 2016</cell><cell>$10,928</cell><cell></cell></row><row><cell>sept. 2016</cell><cell>$10,926</cell><cell></cell></row><row><cell>oct. 2016</cell><cell>$10,924</cell><cell></cell></row><row><cell>nov. 2016</cell><cell>$10,922</cell><cell></cell></row><row><cell>déc. 2016</cell><cell>$10,920</cell><cell>Y</cell></row><row><cell>janv. 2017</cell><cell>$10,917</cell><cell></cell></row><row><cell>févr. 2017</cell><cell>$10,915</cell><cell></cell></row><row><cell>mars 2017</cell><cell>$10,913</cell><cell></cell></row><row><cell>avr. 2017</cell><cell>$10,911</cell><cell></cell></row><row><cell>mai. 2017</cell><cell>$10,909</cell><cell></cell></row><row><cell>juin. 2017</cell><cell>$10,907</cell><cell></cell></row><row><cell>juill. 2017</cell><cell>$11,014</cell><cell></cell></row><row><cell>aout. 2017</cell><cell>$11,121</cell><cell></cell></row><row><cell>sept. 2017</cell><cell>$11,230</cell><cell></cell></row><row><cell>oct. 2017</cell><cell>$11,341</cell><cell></cell></row><row><cell>nov. 2017</cell><cell>$11,452</cell><cell></cell></row><row><cell>déc. 2017</cell><cell>$11,564</cell><cell>Y</cell></row><row><cell>janv. 2018</cell><cell>$10,138</cell><cell></cell></row><row><cell>févr. 2018</cell><cell>$10,266</cell><cell></cell></row><row><cell>mars 2018</cell><cell>$10,395</cell><cell></cell></row><row><cell>avr. 2018</cell><cell>$10,526</cell><cell></cell></row><row><cell>mai. 2018</cell><cell>$10,658</cell><cell></cell></row><row><cell>juin. 2018</cell><cell>$10,792</cell><cell></cell></row><row><cell>juill. 2018</cell><cell>$10,928</cell><cell></cell></row><row><cell>aout. 2018</cell><cell>$10,926</cell><cell></cell></row><row><cell>sept. 2018</cell><cell>$10,924</cell><cell></cell></row><row><cell>oct. 2018</cell><cell>$10,922</cell><cell></cell></row><row><cell>nov. 2018</cell><cell>$10,920</cell><cell>Y</cell></row></table>")] */
        [InlineData(new string[] { "10112", "10005", "9987", "9948", "9906", "9887", "10012", "10138", "10266", "10395", "10526", "10658", "10792" },
         new string[] { "02/2010", "03/2010", "04/2010", "05/2010", "06/2010", "07/2010", "08/2010", "09/2010", "10/2010", "11/2010", "12/2010", "01/2011", "02/2011" }
         , "<table><row><cell>Feb 2010</cell><cell>$10,112</cell><cell>Y</cell></row><row><cell>Mar 2010</cell><cell>$10,005</cell><cell /></row><row><cell>Apr 2010</cell><cell>$9,987</cell><cell /></row><row><cell>May 2010</cell><cell>$9,948</cell><cell /></row><row><cell>Jun 2010</cell><cell>$9,906</cell><cell /></row><row><cell>Jul 2010</cell><cell>$9,887</cell><cell /></row><row><cell>Aug 2010</cell><cell>$10,012</cell><cell /></row><row><cell>Sep 2010</cell><cell>$10,138</cell><cell /></row><row><cell>Oct 2010</cell><cell>$10,266</cell><cell /></row><row><cell>Nov 2010</cell><cell>$10,395</cell><cell /></row><row><cell>Dec 2010</cell><cell>$10,526</cell><cell>Y</cell></row><row><cell>Jan 2011</cell><cell>$10,658</cell><cell /></row><row><cell>Feb 2011</cell><cell>$10,792</cell><cell>Y</cell></row></table>"
         , "<table><row><cell>févr. 2010</cell><cell>$10,112</cell><cell>Y</cell></row><row><cell>mars 2010</cell><cell>$10,005</cell><cell /></row><row><cell>avr. 2010</cell><cell>$9,987</cell><cell /></row><row><cell>mai 2010</cell><cell>$9,948</cell><cell /></row><row><cell>juin 2010</cell><cell>$9,906</cell><cell /></row><row><cell>juill. 2010</cell><cell>$9,887</cell><cell /></row><row><cell>août 2010</cell><cell>$10,012</cell><cell /></row><row><cell>sept. 2010</cell><cell>$10,138</cell><cell /></row><row><cell>oct. 2010</cell><cell>$10,266</cell><cell /></row><row><cell>nov. 2010</cell><cell>$10,395</cell><cell /></row><row><cell>déc. 2010</cell><cell>$10,526</cell><cell>Y</cell></row><row><cell>janv. 2011</cell><cell>$10,658</cell><cell /></row><row><cell>févr. 2011</cell><cell>$10,792</cell><cell>Y</cell></row></table>")]
        [InlineData(new string[] { }, new string[] { }, "<table></table>", "<table></table>")]
        public void assemble10KTableTests(string[] valData, string[] dateData, string expectedEnglishString, string expectedFrenchString)
        {
            //create the necessary TableList from the string data
            TableList tl = new TableList(TableTypes.Currency);
            tl.ShortMonths = getShortMonths();

            for (int i = 0; i < valData.Length; i++)
                tl.AddValidation(valData[i], null, null, null, null, dateData[i]);
            string tableString = tl.GetTableString();
            Assert.Equal(expectedEnglishString, tableString);
            tableString = tl.GetTableStringFrench();
            Assert.Equal(expectedFrenchString, tableString);
        }

        public Dictionary<string, string[]> getShortMonths()
        {
            Dictionary<string, string[]> shortMonths = new Dictionary<string, string[]>();

            shortMonths.Add("SF01", new string[2] {"Jan", "janv." });
            shortMonths.Add("SF02", new string[2] { "Feb", "févr." });
            shortMonths.Add("SF03", new string[2] { "Mar", "mars" });
            shortMonths.Add("SF04", new string[2] { "Apr", "avr." });
            shortMonths.Add("SF05", new string[2] { "May", "mai" });
            shortMonths.Add("SF06", new string[2] { "Jun", "juin" });
            shortMonths.Add("SF07", new string[2] { "Jul", "juill." });
            shortMonths.Add("SF08", new string[2] { "Aug", "août" });
            shortMonths.Add("SF09", new string[2] { "Sep", "sept." });
            shortMonths.Add("SF10", new string[2] { "Oct", "oct." });
            shortMonths.Add("SF11", new string[2] { "Nov", "nov." });
            shortMonths.Add("SF12", new string[2] { "Dec", "déc." });

            return shortMonths;
        }
    }
}
