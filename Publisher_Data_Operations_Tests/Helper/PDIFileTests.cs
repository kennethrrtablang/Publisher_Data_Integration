namespace Publisher_Data_Operations_Tests.Helper
{
    using Publisher_Data_Operations.Helper;
    using System;
    using Xunit;
    using System.Collections.Generic;

    public class FileDetailsObjectTests
    {
        private FileDetailsObject _testClass;
        private PDIFile _pdiFile;

        public FileDetailsObjectTests()
        {
            _pdiFile = new PDIFile("TestValue1025509932");
            _testClass = new FileDetailsObject(_pdiFile);
        }

        [Fact]
        public void CanConstruct()
        {
            var instance = new FileDetailsObject();
            Assert.NotNull(instance);
            instance = new FileDetailsObject(_pdiFile);
            Assert.NotNull(instance);
        }

        [Fact]
        public void CannotConstructWithNullPdiFile()
        {
            Assert.Throws<ArgumentNullException>(() => new FileDetailsObject(default(PDIFile)));
        }

        [Fact]
        public void CanCallSetValues()
        {
            var pdiFile = new PDIFile("TestValue760762784");
            _testClass.SetValues(pdiFile);
            Assert.False(_testClass.FileNameIsValid);
        }

        [Fact]
        public void CannotCallSetValuesWithNullPdiFile()
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.SetValues(default(PDIFile)));
        }

        [Fact]
        public void CanSetAndGetDataCustodian()
        {
            var testValue = "TestValue234453511";
            _testClass.DataCustodian = testValue;
            Assert.Equal(testValue, _testClass.DataCustodian);
        }

        [Fact]
        public void CanSetAndGetDataCustodianID()
        {
            var testValue = "TestValue830335970";
            _testClass.DataCustodianID = testValue;
            Assert.Equal(testValue, _testClass.DataCustodianID);
        }

        [Fact]
        public void CanSetAndGetCompanyName()
        {
            var testValue = "TestValue1783600347";
            _testClass.CompanyName = testValue;
            Assert.Equal(testValue, _testClass.CompanyName);
        }

        [Fact]
        public void CanSetAndGetCompanyID()
        {
            var testValue = "TestValue395149836";
            _testClass.CompanyID = testValue;
            Assert.Equal(testValue, _testClass.CompanyID);
        }

        [Fact]
        public void CanSetAndGetDocumentType()
        {
            var testValue = "TestValue2143193782";
            _testClass.DocumentType = testValue;
            Assert.Equal(testValue, _testClass.DocumentType);
        }

        [Fact]
        public void CanSetAndGetDocumentTypeID()
        {
            var testValue = "TestValue1287887852";
            _testClass.DocumentTypeID = testValue;
            Assert.Equal(testValue, _testClass.DocumentTypeID);
        }

        [Fact]
        public void CanSetAndGetDataType()
        {
            var testValue = "TestValue1706535899";
            _testClass.DataType = testValue;
            Assert.Equal(testValue, _testClass.DataType);
        }

        [Fact]
        public void CanSetAndGetDataTypeID()
        {
            var testValue = "TestValue339505786";
            _testClass.DataTypeID = testValue;
            Assert.Equal(testValue, _testClass.DataTypeID);
        }

        [Fact]
        public void CanSetAndGetCreationDateTime()
        {
            var testValue = new DateTime(2101113424);
            _testClass.CreationDateTime = testValue;
            Assert.Equal(testValue, _testClass.CreationDateTime);
        }

        [Fact]
        public void CanSetAndGetVersion()
        {
            var testValue = "TestValue1676485159";
            _testClass.Version = testValue;
            Assert.Equal(testValue, _testClass.Version);
        }

        [Fact]
        public void CanSetAndGetCode()
        {
            var testValue = "TestValue1954068125";
            _testClass.Code = testValue;
            Assert.Equal(testValue, _testClass.Code);
        }

        [Fact]
        public void CanSetAndGetNote()
        {
            var testValue = "TestValue543311541";
            _testClass.Note = testValue;
            Assert.Equal(testValue, _testClass.Note);
        }

        [Fact]
        public void CanSetAndGetFileNameIsValid()
        {
            var testValue = true;
            _testClass.FileNameIsValid = testValue;
            Assert.Equal(testValue, _testClass.FileNameIsValid);
        }

        [Fact]
        public void CanSetAndGetFileIsValid()
        {
            var testValue = false;
            _testClass.FileIsValid = testValue;
            Assert.Equal(testValue, _testClass.FileIsValid);
        }
    }

    public class PDIFileTests
    {
        private PDIFile _testClass;
        private string _fileName;
        private object _conn;
        private bool _loadOnly;
        private int _fileID;
        private Logger _log;

        public PDIFileTests()
        {
            _fileName = "TestValue1047791792";
            _conn = "";
            _loadOnly = true;
            _fileID = 1430178951;
            _log = new Logger(_conn);
            _testClass = new PDIFile(_fileName, _conn, _loadOnly, _fileID, _log);
        }

        [Theory]
        [ClassData(typeof(PDIFileTestData))]
        public void PdiFileNameTests(string fileName, bool isValid, string code, string note)
        {
            var pdiFile = new PDIFile(fileName);
            Assert.Equal(isValid, pdiFile.IsValid);
            Assert.Equal(code, pdiFile.Code);
            Assert.Equal(note, pdiFile.Note);
        }

        public class PDIFileTestData : IEnumerable<object[]>
        {
            
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] {
                    "CIBCM_IAF_NLOB_STATIC_FF_20220225_111401_1.xlsx"
                    , true
                    , null
                    , null
                };
                yield return new object[] {
                    "CIBCM_IAF_NLOB_TestCode_FSMRFP_FS_20220225_111401_1.xlsx"
                    , true
                    , "TESTCODE"
                    , null
                };
                yield return new object[] {
                    "CIBCM_IAF_NLOB_TestCode_FSMRFP_FS_20220225_111401_1_TestNote.xlsx"
                    , true
                    , "TESTCODE"
                    , "TESTNOTE"
                };
                yield return new object[] {
                    "CIBCM_IAF_NLOB_TestCode_FSMRFP_FS_20220225_111401_A_TestNote.xlsx"
                    , false
                    , "TESTCODE"
                    , "TESTNOTE"
                };
                yield return new object[] {
                    "CIBCM_IAF_NLOB_FSMRFP_FS_20220225_111401_1_TestNote.xlsx" //now this one fails properly since it's missing the code
                    , false
                    , null
                    , "TESTNOTE"
                };
                yield return new object[] {
                    "CIBCM_IAF_NLOB_FSMRFP_20220225_111401_1_TestNote.xlsx"
                    , false
                    , null
                    , null
                };
                yield return new object[] {
                    "TEMPLATE_FSMRFP_FS.xlsx"
                    , true
                    , null
                    , null
                };
                yield return new object[] {
                    "SomeOtherRandomFileName2342 234.xlsx"
                    , false
                    , null
                    , null
                };
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();

        }
        /*[Fact]
        public void CanConstruct()
        {
            var instance = new PDIFile(_fileName, _conn, _loadOnly, _fileID, _log);
            Assert.NotNull(instance);
            instance = new PDIFile(_fileID, _conn, _processPath, _loadOnly, _log);
            Assert.NotNull(instance);
            instance = new PDIFile(_fileName);
            Assert.NotNull(instance);
        }

        [Fact]
        public void CannotConstructWithNullConn()
        {
            Assert.Throws<ArgumentNullException>(() => new PDIFile("TestValue1924289771", default(object), false, 1908045314, new Logger(new object(), new PDIFile("TestValue1662716091"))));
            Assert.Throws<ArgumentNullException>(() => new PDIFile(1816677103, default(object), "TestValue30509011", true, new Logger(new object(), new PDIFile("TestValue1122418172"))));
        }

        [Fact]
        public void CannotConstructWithNullLog()
        {
            Assert.Throws<ArgumentNullException>(() => new PDIFile("TestValue17446652", new object(), true, 1355526793, default(Logger)));
            Assert.Throws<ArgumentNullException>(() => new PDIFile(1313710127, new object(), "TestValue386493639", false, default(Logger)));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotConstructWithInvalidFileName(string value)
        {
            Assert.Throws<ArgumentNullException>(() => new PDIFile(value, new object(), false, 1839283302, new Logger(new object(), new PDIFile("TestValue5383344"))));
            Assert.Throws<ArgumentNullException>(() => new PDIFile(value));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotConstructWithInvalidProcessPath(string value)
        {
            Assert.Throws<ArgumentNullException>(() => new PDIFile(821918940, new object(), value, false, new Logger(new object(), new PDIFile("TestValue513476344"))));
        }

        [Fact]
        public void CanCallGetAllParameters()
        {
            var jobID = 605240222;
            var result = _testClass.GetAllParameters(jobID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanCallIsValidParameters()
        {
            var jobID = 576899518;
            var result = _testClass.IsValidParameters(jobID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanCallProcessAfterLoadOnly()
        {
            var result = _testClass.ProcessAfterLoadOnly();
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanCallIDValues()
        {
            var columnName = "TestValue845813689";
            var result = _testClass.IDValues(columnName);
            Assert.True(false, "Create or modify test");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void CannotCallIDValuesWithInvalidColumnName(string value)
        {
            Assert.Throws<ArgumentNullException>(() => _testClass.IDValues(value));
        }

        [Fact]
        public void CanCallSetBatchFileID()
        {
            var result = _testClass.SetBatchFileID();
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanCallInsertReceiptLog()
        {
            var result = _testClass.InsertReceiptLog();
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanCallGetFileName()
        {
            var result = _testClass.GetFileName();
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanCallLoadParametersWithInt()
        {
            var jobID = 30609568;
            _testClass.LoadParameters(jobID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanCallLoadParametersWithJobIDAndDbInternal()
        {
            var jobID = 392914019;
            var dbInternal = new DBConnection(new object());
            var result = PDIFile.LoadParameters(jobID, dbInternal);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CannotCallLoadParametersWithJobIDAndDbInternalWithNullDbInternal()
        {
            Assert.Throws<ArgumentNullException>(() => PDIFile.LoadParameters(1385110252, default(DBConnection)));
        }

        [Fact]
        public void CanCallGetValidationErrorsCSV()
        {
            var fileID = 1405956148;
            var result = _testClass.GetValidationErrorsCSV(fileID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanCallGetDefaultTemplateName()
        {
            var result = _testClass.GetDefaultTemplateName();
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetGetFileDetails()
        {
            Assert.IsType<FileDetailsObject>(_testClass.GetFileDetails);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetDataID()
        {
            Assert.IsType<int?>(_testClass.DataID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanSetAndGetJobID()
        {
            var testValue = 2065789806;
            _testClass.JobID = testValue;
            Assert.Equal(testValue, _testClass.JobID);
        }

        [Fact]
        public void CanSetAndGetBatchID()
        {
            var testValue = 2036979355;
            _testClass.BatchID = testValue;
            Assert.Equal(testValue, _testClass.BatchID);
        }

        [Fact]
        public void CanSetAndGetFileRunID()
        {
            var testValue = "TestValue1975498922";
            _testClass.FileRunID = testValue;
            Assert.Equal(testValue, _testClass.FileRunID);
        }

        [Fact]
        public void FileIDIsInitializedCorrectly()
        {
            _testClass = new PDIFile(_fileName, _conn, _loadOnly, _fileID, _log);
            Assert.Equal(_fileID, _testClass.FileID);
            _testClass = new PDIFile(_fileID, _conn, _processPath, _loadOnly, _log);
            Assert.Equal(_fileID, _testClass.FileID);
        }

        [Fact]
        public void CanGetDataCustodian()
        {
            Assert.IsType<string>(_testClass.DataCustodian);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetDataCustodianID()
        {
            Assert.IsType<int?>(_testClass.DataCustodianID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetClientName()
        {
            Assert.IsType<string>(_testClass.ClientName);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetClientID()
        {
            Assert.IsType<int?>(_testClass.ClientID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetCompanyID()
        {
            Assert.IsType<int?>(_testClass.CompanyID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetLOB()
        {
            Assert.IsType<string>(_testClass.LOB);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetCode()
        {
            Assert.IsType<string>(_testClass.Code);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetLOBID()
        {
            Assert.IsType<int?>(_testClass.LOBID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetDataType()
        {
            Assert.IsType<string>(_testClass.DataType);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetDataTypeID()
        {
            Assert.IsType<int?>(_testClass.DataTypeID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetDocumentType()
        {
            Assert.IsType<string>(_testClass.DocumentType);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetDocumentTypeID()
        {
            Assert.IsType<int?>(_testClass.DocumentTypeID);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetCreationDate()
        {
            Assert.IsType<string>(_testClass.CreationDate);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetCreationTime()
        {
            Assert.IsType<string>(_testClass.CreationTime);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetVersion()
        {
            Assert.IsType<string>(_testClass.Version);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetNote()
        {
            Assert.IsType<string>(_testClass.Note);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetCreationDateTime()
        {
            Assert.IsType<DateTime?>(_testClass.CreationDateTime);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetExtension()
        {
            Assert.IsType<string>(_testClass.Extension);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetIsValid()
        {
            Assert.IsType<bool>(_testClass.IsValid);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetIsValidFileName()
        {
            Assert.IsType<bool>(_testClass.IsValidFileName);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetErrorMessage()
        {
            Assert.IsType<string>(_testClass.ErrorMessage);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetFullPath()
        {
            Assert.IsType<string>(_testClass.FullPath);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetFileNameWithoutExtension()
        {
            Assert.IsType<string>(_testClass.FileNameWithoutExtension);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanSetAndGetOnlyFileName()
        {
            var testValue = "TestValue2100823065";
            _testClass.OnlyFileName = testValue;
            Assert.Equal(testValue, _testClass.OnlyFileName);
        }

        [Fact]
        public void CanGetGetDataType()
        {
            Assert.IsType<DataTypeID?>(_testClass.GetDataType);
            Assert.True(false, "Create or modify test");
        }

        [Fact]
        public void CanGetGetDocumentType()
        {
            Assert.IsType<DocumentTypeID?>(_testClass.GetDocumentType);
            Assert.True(false, "Create or modify test");
        } */
    }
}