namespace Publisher_Data_Operations_Tests.Helper
{
    using Publisher_Data_Operations.Helper;
    using System;
    using Xunit;
    using System.Data;

    public class ExtractTests
    {
        //private Extract _testClass;
        //private PDIStream _processStream;
        //private object _con;
        //private Logger _log;
        //private int _jobID;
        //private object _altCon;

        public ExtractTests()
        {
            //_processStream = new PDIStream(new PDIFile("TestValue1919811107"));
            //_con = new object();
            //_log = new Logger(742433430, new object());
            //_jobID = 1819428972;
            //_altCon = new object();
            //_testClass = new Extract(_processStream, _con, _log, _jobID, _altCon);
        }


        [Theory]
        [InlineData("M11{{i-e}}|SERIES{{-1}}|{{blank}},R70|{{nonBlank}}|{{rMax}}|Text", "M11i|SERIES{{-1}}|{{blank}},M11j|SERIES{{-1}}|{{blank}},M11k|SERIES{{-1}}|{{blank}},M11l|SERIES{{-1}}|{{blank}},M11m|SERIES{{-1}}|{{blank}},M11n|SERIES{{-1}}|{{blank}},M11o|SERIES{{-1}}|{{blank}},M11p|SERIES{{-1}}|{{blank}},M11q|SERIES{{-1}}|{{blank}},M11r|SERIES{{-1}}|{{blank}},M11s|SERIES{{-1}}|{{blank}},M11t|SERIES{{-1}}|{{blank}},M11u|SERIES{{-1}}|{{blank}},M11v|SERIES{{-1}}|{{blank}},M11w|SERIES{{-1}}|{{blank}},M11x|SERIES{{-1}}|{{blank}},M11y|SERIES{{-1}}|{{blank}},M11z|SERIES{{-1}}|{{blank}},M11a|SERIES{{-1}}|{{blank}},M11b|SERIES{{-1}}|{{blank}},M11c|SERIES{{-1}}|{{blank}},M11d|SERIES{{-1}}|{{blank}},M11e|SERIES{{-1}}|{{blank}},R70|{{nonBlank}}|{{rMax}}|Text")]
        [InlineData("M11~M12{{a-o}}|StartTable!{{+1}}|EndTable!{{-1}}", "M11a|StartTable!{{+1}}|EndTable!{{-1}},M12a|StartTable!{{+1}}|EndTable!{{-1}},M11b|StartTable!{{+1}}|EndTable!{{-1}},M12b|StartTable!{{+1}}|EndTable!{{-1}},M11c|StartTable!{{+1}}|EndTable!{{-1}},M12c|StartTable!{{+1}}|EndTable!{{-1}},M11d|StartTable!{{+1}}|EndTable!{{-1}},M12d|StartTable!{{+1}}|EndTable!{{-1}},M11e|StartTable!{{+1}}|EndTable!{{-1}},M12e|StartTable!{{+1}}|EndTable!{{-1}},M11g|StartTable!{{+1}}|EndTable!{{-1}},M12g|StartTable!{{+1}}|EndTable!{{-1}},M11i|StartTable!{{+1}}|EndTable!{{-1}},M12i|StartTable!{{+1}}|EndTable!{{-1}},M11j|StartTable!{{+1}}|EndTable!{{-1}},M12j|StartTable!{{+1}}|EndTable!{{-1}},M11k|StartTable!{{+1}}|EndTable!{{-1}},M12k|StartTable!{{+1}}|EndTable!{{-1}},M11l|StartTable!{{+1}}|EndTable!{{-1}},M12l|StartTable!{{+1}}|EndTable!{{-1}},M11m|StartTable!{{+1}}|EndTable!{{-1}},M12m|StartTable!{{+1}}|EndTable!{{-1}},M11n|StartTable!{{+1}}|EndTable!{{-1}},M12n|StartTable!{{+1}}|EndTable!{{-1}},M11o|StartTable!{{+1}}|EndTable!{{-1}},M12o|StartTable!{{+1}}|EndTable!{{-1}}")]
        public void ConvertRangeTests(string value, string expected)
        {
            string text = Extract.ConvertRange(value);
            Assert.Equal(expected, text);

        }

        //[Fact]
        //public void CanConstruct()
        //{
        //    var instance = new Extract(_processStream, _con, _log, _jobID, _altCon);
        //    Assert.NotNull(instance);
        //}

        //[Fact]
        //public void CannotConstructWithNullProcessStream()
        //{
        //    Assert.Throws<ArgumentNullException>(() => new Extract(default(PDIStream), new object(), new Logger(91894794, new object()), 1997295550, new object()));
        //}

        //[Fact]
        //public void CannotConstructWithNullCon()
        //{
        //    Assert.Throws<ArgumentNullException>(() => new Extract(new PDIStream(new PDIFile("TestValue2091527736")), default(object), new Logger(338839402, new object()), 45414041, new object()));
        //}

        //[Fact]
        //public void CannotConstructWithNullLog()
        //{
        //    Assert.Throws<ArgumentNullException>(() => new Extract(new PDIStream(new PDIFile("TestValue712026312")), new object(), default(Logger), 1668599634, new object()));
        //}

        //[Fact]
        //public void CannotConstructWithNullAltCon()
        //{
        //    Assert.Throws<ArgumentNullException>(() => new Extract(new PDIStream(new PDIFile("TestValue1622021532")), new object(), new Logger(2142291193, new object()), 922992937, default(object)));
        //}

        //[Fact]
        //public void CanCallRunExtract()
        //{
        //    var result = _testClass.RunExtract();
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public void CanCallConvertRange()
        //{
        //    var value = "TestValue2085620416";
        //    var result = Extract.ConvertRange(value);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public void CannotCallConvertRangeWithInvalidValue(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => Extract.ConvertRange(value));
        //}

        //[Fact]
        //public void CanCallReplaceAddress()
        //{
        //    var value = "TestValue1852368425";
        //    var rep = new object();
        //    var result = Extract.ReplaceAddress(value, rep);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public void CannotCallReplaceAddressWithNullRep()
        //{
        //    Assert.Throws<ArgumentNullException>(() => Extract.ReplaceAddress("TestValue1676768031", default(object)));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public void CannotCallReplaceAddressWithInvalidValue(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => Extract.ReplaceAddress(value, new object()));
        //}

        //[Fact]
        //public void CanCallPlusOffset()
        //{
        //    var value = "TestValue1837461068";
        //    var result = Extract.PlusOffset(value, out var offset);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public void CannotCallPlusOffsetWithInvalidValue(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => Extract.PlusOffset(value, out _));
        //}

        //[Fact]
        //public void CanCallConvertAddress()
        //{
        //    var value = "TestValue262248708";
        //    var al = new AsposeLoader("TestValue1581201264");
        //    var dt = new DataTable();
        //    var startRow = 195557933;
        //    var end = true;
        //    Extract.ConvertAddress(value, al, dt, out var row, out var col, startRow, end);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public void CannotCallConvertAddressWithNullAl()
        //{
        //    Assert.Throws<ArgumentNullException>(() => Extract.ConvertAddress("TestValue1310947122", default(AsposeLoader), new DataTable(), out _, out _, 879386458, true));
        //}

        //[Fact]
        //public void CannotCallConvertAddressWithNullDt()
        //{
        //    Assert.Throws<ArgumentNullException>(() => Extract.ConvertAddress("TestValue297562340", new AsposeLoader("TestValue509786517"), default(DataTable), out _, out _, 1212115554, true));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public void CannotCallConvertAddressWithInvalidValue(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => Extract.ConvertAddress(value, new AsposeLoader("TestValue1073145345"), new DataTable(), out _, out _, 240267821, false));
        //}

        //[Fact]
        //public void CanCallSplitText()
        //{
        //    var value = "TestValue353473364";
        //    var result = Extract.SplitText(value);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public void CannotCallSplitTextWithInvalidValue(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => Extract.SplitText(value));
        //}
    }
}