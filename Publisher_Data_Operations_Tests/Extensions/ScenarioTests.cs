using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Publisher_Data_Operations.Extensions;
using Xunit;

namespace Publisher_Data_Operations_Tests.Extensions
{

    public class ScenarioTests
    {
        Flags flags = new Flags();
        Scenarios scenarios = new Scenarios();

        [Theory]
        [InlineData("default", "default")]
        [InlineData("fieldName{value1, value2 , value3} & fieldName2 { value3, value4 }", "fieldName{value1,value2,value3}&fieldName2{value3,value4}")]
        [InlineData("abc123   {   1    } ", "abc123{1}")]
        [InlineData("fn1{val1} & fn2 { val1, val2, val3, val4 } & fn3 {val1,val2} & fn4 {val1,val2,val3}", "fn1{val1}&fn2{val1,val2,val3,val4}&fn3{val1,val2}&fn4{val1,val2,val3}")]
        [InlineData("PortfolioCharacteristicsTemplate{Distinction & Inhance, template2} & fn4{4}", "PortfolioCharacteristicsTemplate{Distinction & Inhance,template2}&fn4{4}")]
        public void scenarioCreateTests(string value, string expected)
        {
            flags.Clear();
            flags.ParseScenario(value);
            Assert.Equal(flags.ToString(), expected);
            
        }


        [Theory]
       
        [InlineData("fieldName{value1, value2 , value3} & fieldName2 { value3, value4 }", "fieldName3 { value3, value4 }", "fieldName,fieldName2,fieldName3")]
        [InlineData("fn1{val1} & fn2 { val1, val2, val3, val4 } & fn3 {val1,val2} & fn4 {val1,val2,val3}", "fn1{val1} & fn2 { val1, val2, val3, val4}&fn3 { val1,val2}", "fn1,fn2,fn3,fn4")]
        public void distinctFieldNames(string value1, string value2, string expected)
        {
            scenarios.Add(new Scenario(value1, "", "", DateTime.MinValue));
            scenarios.Add(new Scenario(value2, "", "", DateTime.MinValue));
            string result = string.Join(",", scenarios.AllDistinctFieldNames());
            Assert.Equal(result, expected);

        }
    }
}
