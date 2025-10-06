namespace Publisher_Data_Operations_Tests.Helper
{
    using Publisher_Data_Operations.Extensions;
    using Publisher_Data_Operations.Helper;
    using System;
    using Xunit;

    public class LoggerTests
    {
        private Logger _testClass;
        private DBConnection _con;
        private int? _fileID;
        private int? _batchID;
        private string _runID;
        private PDIFile _pdiFile;

        public LoggerTests()
        {
            _con = new DBConnection("");
            _fileID = 1028292100;
            _batchID = 928728921;
            _runID = "TestValue687636836";
            _pdiFile = new PDIFile("CIBCM_IAF_NLOB_BAU_FF_20211201_134100_1.xlsx");
            _testClass = new Logger(_con, _fileID, _batchID, _runID);
        }

        [Fact]
        public void CanConstruct()
        {
            var instance = new Logger(_con, _fileID, _batchID, _runID);
            Assert.NotNull(instance);
            instance = new Logger(_con, _pdiFile);
            Assert.NotNull(instance);
        }

        //[Fact]
        //public void CannotConstructWithNullCon()
        //{
        //    Assert.Throws<ArgumentNullException>(() => new Logger(default(object), 940261484, 781346412, "TestValue1564318409"));
        //    Assert.Throws<ArgumentNullException>(() => new Logger(default(object), new PDIFile("TestValue171825132")));
        //}

        //[Fact]
        //public void CannotConstructWithNullPdiFile()
        //{
        //    Assert.Throws<ArgumentNullException>(() => new Logger(new object(), default(PDIFile)));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public void CannotConstructWithInvalidRunID(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => new Logger(new object(), 1160003731, 1289686655, value));
        //}

        //[Fact]
        //public void CanCallUpdateParams()
        //{
        //    var pdiFile = new PDIFile("TestValue1212961976");
        //    _testClass.UpdateParams(pdiFile);
        //    Assert.True(false, "Create or modify test");
        //}

        [Fact]
        public void CannotCallUpdateParamsWithNullPdiFile()
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.UpdateParams(default(PDIFile)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallAddErrorWithErrorMessageWithInvalidErrorMessage(string value)
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.AddError(value));
        }

        //[Fact]
        //public void CanCallAddErrorWithLogAndErrorMessage()
        //{
        //    var log = new Logger(_con, new PDIFile("TestValue676764862"));
        //    var errorMessage = "TestValue1246496606";
        //    Logger.AddError(log, errorMessage);
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public void CannotCallAddErrorWithLogAndErrorMessageWithNullLog()
        //{
        //    Assert.Throws<ArgumentNullException>(() => Logger.AddError(default(Logger), "TestValue1507270340"));
        //}

        //[Theory]
        //[InlineData(null)]
        //[InlineData("")]
        //[InlineData("   ")]
        //public void CannotCallAddErrorWithLogAndErrorMessageWithInvalidErrorMessage(string value)
        //{
        //    Assert.Throws<ArgumentNullException>(() => Logger.AddError(new Logger(new object(), new PDIFile("TestValue1094292828")), value));
        //}

        //[Fact]
        //public void CanCallWriteErrorsToDB()
        //{
        //    var result = _testClass.WriteErrorsToDB();
        //    Assert.True(false, "Create or modify test");
        //}

        //[Fact]
        //public void CanCallDispose()
        //{
        //    _testClass.Dispose();
        //    Assert.True(false, "Create or modify test");
        //}

        [Fact]
        public void FileIDIsInitializedCorrectly()
        {
            Assert.Equal(_fileID, _testClass.FileID);
        }

        [Fact]
        public void CanSetAndGetFileID()
        {
            var testValue = 1286230994;
            _testClass.FileID = testValue;
            Assert.Equal(testValue, _testClass.FileID);
        }

        [Fact]
        public void BatchIDIsInitializedCorrectly()
        {
            Assert.Equal(_batchID, _testClass.BatchID);
        }

        [Fact]
        public void CanSetAndGetBatchID()
        {
            var testValue = 275977358;
            _testClass.BatchID = testValue;
            Assert.Equal(testValue, _testClass.BatchID);
        }

        [Fact]
        public void RunIDIsInitializedCorrectly()
        {
            Assert.Equal(_runID, _testClass.RunID);
        }

        [Fact]
        public void CanSetAndGetRunID()
        {
            var testValue = "TestValue699462706";
            _testClass.RunID = testValue;
            Assert.Equal(testValue, _testClass.RunID);
        }
    }
}