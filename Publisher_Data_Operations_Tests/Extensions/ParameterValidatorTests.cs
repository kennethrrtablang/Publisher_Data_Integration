using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Publisher_Data_Operations.Extensions;

namespace Publisher_Data_Operations_Tests.Extensions
{
    public class ParameterValidatorTests
    {
        ParameterValidator xmlValidator;
        public ParameterValidatorTests()
        {
            Dictionary<string, string> validParams = new Dictionary<string, string>();
            validParams.Add("TestVal", string.Empty);
            validParams.Add("AnotherVal", string.Empty);
            validParams.Add("FinalVal", string.Empty);
            validParams.Add("IsMERAvailable", string.Empty);
            validParams.Add("FFDocAgeStatusID", string.Empty);
            validParams.Add("SeriesLetter", string.Empty);
            validParams.Add("PortfolioCharacteristicsTemplate", string.Empty);
            validParams.Add("FF31b", string.Empty);

            xmlValidator = new ParameterValidator(validParams);
        }
        
        [Fact]
        public void Dispose()
        {
            xmlValidator = null;
        }

        [Theory]
        [InlineData("<table><row><cell>[Replacement in Square]</cell><cell></cell></row><row><cell></cell><cell></cell></row></table>", true, true)]
        [InlineData("<table><row><cell></cell></row>", true, false)]
        [InlineData("<table><row><cell></cell></row><table>", true, false)]
        [InlineData("<table><row><cell><SeriesLetter></cell></row></table>", true, true)]
        [InlineData("This is a normal string", true, true)]
        [InlineData("This is a replace <AnotherVal> string with <TestVal> replacement strings", true, true)]
        [InlineData("<p>You don't pay these expenses directly. They affect you because they reduce the fund's returns.</p><p>The fund’s expenses are made up of the management fee, fixed administration fee, other fund costs and trading costs.The series’ annual management fee is <TestVal> of the series' value. The series' annual fixed administration fee is <AdminFeePercent> of the series' value.</p><p>Because this series is new, it's fund costs and trading costs are not yet available.</p>", true, false)]
        [InlineData("<p>You don't pay these expenses directly. They affect you because they reduce the fund's returns.</p><p>The fund’s expenses are made up of the management <br>fee, fixed administration fee, other fund costs and trading costs.The series’ annual management fee is <TestVal> of the series' value. The series' annual fixed administration fee is <IsMERAvailable> of the series' value.</p><p>Because this series is new, it's fund costs and trading costs are not yet available.</p>", true, true)]
        [InlineData("<table> <row> <cell></cell> <cell>Return</cell> <cell>3 months ending</cell> <cell>If you invested $1,000 at the beginning of the period</cell> </row> <row> <row><cell>Best return</cell> <cell>12.9%</cell> <cell>June 30, 2020</cell> <cell>Your investment would rise to $1,129</cell> </row> <row> <cell>Worst return</cell> <cell>-15.1%</cell> <cell>March 31, 2020</cell> <cell>Your investment would drop to $849</cell> </row> </table>", true, false)]
        [InlineData("<table>\n\t<row>\n\t\t<cell>This is a normal string with a broken non breaking space &nbsp here</cell>\n\t</row>\n</table>", false, false)]
        [InlineData("<p>Investors:</p></p>\r\n<ul><li>seeking a core (or focused) fund concentrated in developed market stocks outside the U.S.</li>\r\n<li>planning to hold their investment for the medium to long term</li></ul>", true, false)]
        [InlineData("<p>Investor's:</p>\r\n<ul><li>\"seeking\" a core (or focused) fund concentrated in developed market stocks outside the U.S.</li>\r\n<li>planning to hold their investment for the medium to long term</li></ul>", true, true)]
        public void isXMLValidXML(string value, bool cleanup, bool expected)
        {
            Assert.Equal(expected, xmlValidator.IsValidXML(value, cleanup));
        }

        [Theory]
        [InlineData("IsMERAvailable{0}&FFDocAgeStatusID{0,1,2,3}&SeriesLetter{A,L,HW,}", false)]
        [InlineData("IsMERAvailable{0}&FFDocAgeStatusID{0,1,2,3}&SeriesLetter{A,L,HW}", true)]
        [InlineData("FFDocAgeStatusID{1,3}", true)]
        [InlineData("FFDocAgeStatusID{1,3", false)]
        [InlineData("FFDocAgeStatusID{}", false)]
        [InlineData("FFDocAgeStatusID{1,3}FFDocAgeStatusID{2}", false)]
        [InlineData("Unknown{1,3}&AlsoUnknown{1}", false)]
        [InlineData("PortfolioCharacteristicsTemplate{Distinction & Inhance, template2} & FFDocAgeStatusID{4}", true)]
        [InlineData("FF31b{ABF╣F}", false)]
        [InlineData("FF31b{ABF_F}", true)]
        public void isScenarioValid(string value, bool expected)
        {
            Assert.Equal(expected, xmlValidator.IsValidScenario(value));
        }

    }
}
