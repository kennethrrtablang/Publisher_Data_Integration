using Publisher_Data_Operations;
using System.Collections.Generic;
using Xunit;
using Moq;
using Publisher_Data_Operations.Extensions;
using Publisher_Data_Operations_Tests.Resources;
using System.IO;
using System.Xml.Serialization;
using System.Collections.ObjectModel;
using System.Linq;
using System.Data;
using System.Xml;
using Xunit.Abstractions;
using Newtonsoft.Json;

namespace Publisher_Data_Operations_Tests
{
    public class DocumentProcessingTests
    {

        //protected Mock<Pub_Data_Integration_DEVEntities> MockContext;
        //protected Mock<DbSet<pdi_Publisher_Documents>> MockSet;
        //protected Mock<DbSet<pdi_Mutual_Fund_FF_BAU_Data_Staging>> MockSetFF;


        private readonly DocumentProcessing _documentProcessing;
        private DataTable dt;

        //private Mock<pdi_Mutual_Fund_FF_BAU_Data_Staging> _mockedFF_Staging;
        //private Mock<pdi_Publisher_Documents> _mockedPublisher_Documents;

        //private IEnumerable<pdi_Publisher_Documents> _publisherDocuments { get; set; }

        //private IEnumerable<pdi_Publisher_Documents> _publisherDocumentsResults { get; set; }

        //private IEnumerable<pdi_Mutual_Fund_FF_BAU_Data_Staging> _FFStaging { get; set; }



        //private Mock<Pub_Data_Integration_DEVEntities> _mockedContext;

        public DocumentProcessingTests()
        {
            _documentProcessing = new DocumentProcessing("", null);
            dt = SetupTable(); 

            
            //_mockedFF_Staging = new Mock<pdi_Mutual_Fund_FF_BAU_Data_Staging>();
            //_mockedPublisher_Documents = new Mock<pdi_Publisher_Documents>();

            //_mockedContext = new Mock<Pub_Data_Integration_DEVEntities>();

            //using (StreamReader reader = new StreamReader(PUB_Data_Integration_DEV.pdi_Publisher_Documents.ToStream()))
            //{
            //    XmlSerializer serializer = new XmlSerializer(typeof(Collection<pdi_Publisher_Documents>), new XmlRootAttribute("ArrayOfpdi_Publisher_Documents"));
            //    _publisherDocuments = (Collection<pdi_Publisher_Documents>)serializer.Deserialize(reader);
            //}

            //var publisherDocumentsDbSet = _publisherDocuments.GetQueryableMockDbSet();
            //foreach (var document in _publisherDocuments)
            //    publisherDocumentsDbSet.Add(document);

            //_mockedContext.Setup(o => o.pdi_Publisher_Documents).Returns(publisherDocumentsDbSet);

            ////_mockedPublisher_Documents.Setup(o => _publisherDocuments);

            //using (StreamReader reader = new StreamReader(PUB_Data_Integration_DEV.pdi_Fund_Fact_Source_Data_Staging.ToStream()))
            //{
            //    XmlSerializer serializer = new XmlSerializer(typeof(Collection<pdi_Mutual_Fund_FF_BAU_Data_Staging>), new XmlRootAttribute("ArrayOfpdi_Fund_Fact_Source_Data_Staging"));
            //    //List<pdi_Fund_Fact_Source_Data_Staging> FFList = (List<pdi_Fund_Fact_Source_Data_Staging>)serializer.Deserialize(reader);
            //    _FFStaging = (Collection<pdi_Mutual_Fund_FF_BAU_Data_Staging>)serializer.Deserialize(reader);
            //}

            //var FFStagingDbSet = _FFStaging.GetQueryableMockDbSet();
            //foreach (var staging in _FFStaging)
            //    FFStagingDbSet.Add(staging);

            //_mockedContext.Setup(o => o.pdi_Mutual_Fund_FF_BAU_Data_Staging).Returns(FFStagingDbSet);


        }

        [Theory]
        [ClassData(typeof(FFDocAgeStatusIDTestData))]
        public void isValidFFDocAgeStatusID(Dictionary<string, string> staging, DataRow matchedRow, FFDocAge? expected)
        {
            Assert.Equal(expected, _documentProcessing.SetFFDocAge(staging, matchedRow, dt));
        }

        public class FFDocAgeStatusIDTestData : IEnumerable<object[]>
        {
            DataTable dt = SetupTable(); // the table is used in the setup so it needs to be available locally
            public IEnumerator<object[]> GetEnumerator()
            {
                string testData =
                "Client_ID:1002|Document_Number:F014_ISF6|InceptionDate:01/01/2022|FilingReferenceID:FR-032022|NoSecuritiesIssued:1|DataAsAtDate:31/03/2022|FundCode:F014|Expected:NewSeries,Client_ID:1002|Document_Number:F013_ISFA|InceptionDate:28/07/2003|FilingReferenceID:FR-032022|NoSecuritiesIssued:0|DataAsAtDate:31/03/2022|FundCode:F013|Expected:TwelveConsecutiveMonths,Client_ID:1002|Document_Number:F013_ISFB|InceptionDate:28/04/2021|FilingReferenceID:FR-032022|NoSecuritiesIssued:1|DataAsAtDate:31/03/2022|FundCode:F013|Expected:NewFund,Client_ID:1002|Document_Number:F014_ISF6|InceptionDate:01/04/2022|FilingReferenceID:FR-032022|NoSecuritiesIssued:0|DataAsAtDate:31/03/2022|FundCode:F014|Expected:BrandNewSeries,Client_ID:1002|Document_Number:F014_ISF8|InceptionDate:12/10/2021|FilingReferenceID:FR-032022|NoSecuritiesIssued:1|DataAsAtDate:31/03/2022|FundCode:F014|Expected:NewSeries,Client_ID:1002|Document_Number:F015_ISFA|InceptionDate:28/04/2022|FilingReferenceID:FR-032022|NoSecuritiesIssued:0|DataAsAtDate:31/03/2022|FundCode:F015|Expected:BrandNewFund,Client_ID:1002|Document_Number:T099_CISFLC|InceptionDate:01/04/2022|FilingReferenceID:IGWM-Amend-02-2022|NoSecuritiesIssued:0|DataAsAtDate:31/03/2022|FundCode:T099|Expected:BrandNewFund,Client_ID:1002|Document_Number:T098_CISFLC|InceptionDate:31/01/2022|FilingReferenceID:IGWM-Amend-02-2022|NoSecuritiesIssued:0|DataAsAtDate:31/03/2022|FundCode:T098|Expected:NewFund,Client_ID:1002|Document_Number:T099_CISFLA|InceptionDate:31/03/2021|FilingReferenceID:IGWM-Amend-02-2022|NoSecuritiesIssued:0|DataAsAtDate:31/03/2022|FundCode:T099|Expected:BrandNewFund,Client_ID:1002|Document_Number:T097_CISFL|InceptionDate:30/03/2021|FilingReferenceID:NewFilingRefID|NoSecuritiesIssued:0|DataAsAtDate:31/03/2022|FundCode:T097|Expected:TwelveConsecutiveMonths,Client_ID:1002|Document_Number:T097_CISFL|InceptionDate:30/01/2022|FilingReferenceID:NewFilingRefID|NoSecuritiesIssued:0|DataAsAtDate:31/03/2022|FundCode:T097|Expected:NewFund";

                var testSplit = testData.Split(',');
                foreach(string cur in testSplit)
                {
                    Dictionary<string, string> curDict = cur.Split('|').ToDictionary(i => i.Split(':')[0], i => i.Split(':')[1]);
                    yield return new object[]
                    {
                        curDict,
                        dt.Select($"Client_ID = {curDict["Client_ID"]} AND Document_Number = '{curDict["Document_Number"]}'").SingleOrDefault(),
                        (FFDocAge)FFDocAge.Parse(typeof(FFDocAge), curDict["Expected"])
                    };
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
 
        }

        // TODO: Broken by changes to enum FFDocAgeStatusID
        //[Fact]
        //public void ConfirmDbContextUsesSDummyData()
        //{
        //    var dbContext = _mockedContext.Object;
        //    var publisherDocuments = dbContext.pdi_Publisher_Documents;

        //    Assert.True(publisherDocuments.Count() == 24);

        //    var ffStaging = dbContext.pdi_Fund_Fact_Source_Data_Staging;

        //    Assert.True(ffStaging.Count() == 32);



        //}

        //[Fact]
        //public void CompareDocumentProcessingResults()
        //{

        //    Pub_Data_Integration_DEVEntities dbContext = _mockedContext.Object;
        //    _documentProcessing.processFFStaging(208, dbContext);

        //    using (StreamReader reader = new StreamReader(PUB_Data_Integration_DEV.pdi_Publisher_Documents_Results.ToStream()))
        //    {
        //        XmlSerializer serializer = new XmlSerializer(typeof(Collection<pdi_Publisher_Documents>), new XmlRootAttribute("ArrayOfpdi_Publisher_Documents"));
        //        _publisherDocuments = (Collection<pdi_Publisher_Documents>)serializer.Deserialize(reader);
        //    }
        //    //var publisherDocumentsResultsDbSet = _publisherDocuments.GetQueryableMockDbSet();
        //    //foreach (var document in _publisherDocuments)
        //    //    publisherDocumentsResultsDbSet.Add(document);

        //    var updated = dbContext.pdi_Publisher_Documents.ToList();

        //    IEnumerable<string> changes = _publisherDocuments.ToList().EnumeratePropertyDifferences(updated);

        //}

        //    [Fact]
        //    public void CompareTest()
        //    {

        //        List< pdi_Publisher_Documents> _PubDocs;

        //        using (StreamReader reader = new StreamReader(PUB_Data_Integration_DEV.pdi_Publisher_Documents.ToStream()))
        //        {
        //            XmlSerializer serializer = new XmlSerializer(typeof(Collection<pdi_Publisher_Documents>), new XmlRootAttribute("ArrayOfpdi_Publisher_Documents"));
        //            _PubDocs = ((Collection<pdi_Publisher_Documents>)serializer.Deserialize(reader)).ToList();
        //        }


        //        var queryPubDocs = _PubDocs.AsQueryable();

        //        MockSet = new Mock<DbSet<pdi_Publisher_Documents>>();
        //        MockContext = new Mock<Pub_Data_Integration_DEVEntities>();

        //        MockSet.As<IQueryable<pdi_Publisher_Documents>>().Setup(m => m.Expression).Returns(queryPubDocs.Expression);
        //        MockSet.As<IQueryable<pdi_Publisher_Documents>>().Setup(m => m.ElementType).Returns(queryPubDocs.ElementType);
        //        MockSet.As<IQueryable<pdi_Publisher_Documents>>().Setup(m => m.GetEnumerator()).Returns(queryPubDocs.GetEnumerator);

        //        MockSet.As<IQueryable<pdi_Publisher_Documents>>().Setup(m => m.Provider).Returns(queryPubDocs.Provider);

        //        //MockSet.As<IQueryable<pdi_Publisher_Documents>>().Setup(m => m.Provider).Returns(new AsyncQueryProvider<Person>(queryable.Provider));
        //        //MockSet.As<IDbAsyncEnumerable<Person>>().Setup(m => m.GetAsyncEnumerator()).Returns(new AsyncEnumerator<Person>(queryable.GetEnumerator()));

        //        MockSet.Setup(m => m.Add(It.IsAny<pdi_Publisher_Documents>())).Callback((pdi_Publisher_Documents pubdoc) => _PubDocs.Add(pubdoc));
        //        MockSet.Setup(m => m.Remove(It.IsAny<pdi_Publisher_Documents>())).Callback((pdi_Publisher_Documents pubdoc) => _PubDocs.Remove(pubdoc));
        //        //MockSet.Setup(Function(x) x.Local).Returns(New ObservableCollection(Of pdi_Publisher_Documents)(dataMyDbSet))

        //        //    MockSet.Setup(m => m.Local).Returns.

        //        MockContext.Setup(m => m.pdi_Publisher_Documents).Returns(MockSet.Object);
        //        MockContext.Setup(m => m.Set<pdi_Publisher_Documents>().Local).Returns(new ObservableCollection<pdi_Publisher_Documents>(queryPubDocs));

        //        List<pdi_Fund_Fact_Source_Data_Staging> _FFStaging;
        //        using (StreamReader reader = new StreamReader(PUB_Data_Integration_DEV.pdi_Fund_Fact_Source_Data_Staging.ToStream()))
        //        {
        //            XmlSerializer serializer = new XmlSerializer(typeof(Collection<pdi_Fund_Fact_Source_Data_Staging>), new XmlRootAttribute("ArrayOfpdi_Fund_Fact_Source_Data_Staging"));
        //            _FFStaging = ((Collection<pdi_Fund_Fact_Source_Data_Staging>)serializer.Deserialize(reader)).ToList();
        //        }


        //        var queryFFStaging = _FFStaging.AsQueryable();

        //        MockSetFF = new Mock<DbSet<pdi_Fund_Fact_Source_Data_Staging>>();

        //        MockSetFF.As<IQueryable<pdi_Fund_Fact_Source_Data_Staging>>().Setup(m => m.Provider).Returns(queryFFStaging.Provider);
        //        MockSetFF.As<IQueryable<pdi_Fund_Fact_Source_Data_Staging>>().Setup(m => m.Expression).Returns(queryFFStaging.Expression);
        //        MockSetFF.As<IQueryable<pdi_Fund_Fact_Source_Data_Staging>>().Setup(m => m.ElementType).Returns(queryFFStaging.ElementType);
        //        MockSetFF.As<IQueryable<pdi_Fund_Fact_Source_Data_Staging>>().Setup(m => m.GetEnumerator()).Returns(queryFFStaging.GetEnumerator);


        //        //MockSet.As<IQueryable<pdi_Publisher_Documents>>().Setup(m => m.Provider).Returns(new AsyncQueryProvider<Person>(queryable.Provider));
        //        //MockSet.As<IDbAsyncEnumerable<Person>>().Setup(m => m.GetAsyncEnumerator()).Returns(new AsyncEnumerator<Person>(queryable.GetEnumerator()));

        //        MockSetFF.Setup(m => m.Add(It.IsAny<pdi_Fund_Fact_Source_Data_Staging>())).Callback((pdi_Fund_Fact_Source_Data_Staging ffstage) => _FFStaging.Add(ffstage));
        //        MockSetFF.Setup(m => m.Remove(It.IsAny<pdi_Fund_Fact_Source_Data_Staging>())).Callback((pdi_Fund_Fact_Source_Data_Staging ffstage) => _FFStaging.Remove(ffstage));


        //        MockContext.Setup(m => m.pdi_Fund_Fact_Source_Data_Staging).Returns(MockSetFF.Object);
        //        MockContext.Setup(m => m.Set<pdi_Fund_Fact_Source_Data_Staging>().Local).Returns(new ObservableCollection<pdi_Fund_Fact_Source_Data_Staging>(queryFFStaging));

        //        _documentProcessing.processFFStaging(208, MockContext.Object);
        //    }

        public static DataTable SetupTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Client_ID", System.Type.GetType("System.Int32"));
            dt.Columns.Add("Document_Type_ID", System.Type.GetType("System.Int32"));
            dt.Columns.Add("LOB_ID", System.Type.GetType("System.Int32"));
            dt.Columns.Add("Document_Number", System.Type.GetType("System.String"));
            dt.Columns.Add("FundCode", System.Type.GetType("System.String"));
            dt.Columns.Add("SeriesLetter", System.Type.GetType("System.String"));
            dt.Columns.Add("InceptionDate", System.Type.GetType("System.String"));
            dt.Columns.Add("FFDocAgeStatusID", typeof(FFDocAge));
            dt.Columns.Add("IsActiveStatus", System.Type.GetType("System.Boolean"));
            dt.Columns.Add("Date_Created", System.Type.GetType("System.DateTime"));
            dt.Columns.Add("FilingReferenceID", System.Type.GetType("System.String"));
            dt.Columns.Add("Last_Filing_Date", System.Type.GetType("System.String"));
            dt.Columns.Add("Last_Updated", System.Type.GetType("System.DateTime"));
            dt.Columns.Add("NoSecuritiesIssued", System.Type.GetType("System.Boolean"));

            return TestHelpers.LoadXMLTable(Resources.PUB_Data_Integration_DEV.pdi_Publisher_Documents, dt);
        }

        public class MemberDataSerializer<T> : IXunitSerializable
        {
            public T Object { get; private set; }

            public MemberDataSerializer()
            {
            }

            public MemberDataSerializer(T objectToSerialize)
            {
                Object = objectToSerialize;
            }

            public void Deserialize(IXunitSerializationInfo info)
            {
                Object = JsonConvert.DeserializeObject<T>(info.GetValue<string>("objValue"));
            }

            public void Serialize(IXunitSerializationInfo info)
            {
                var json = JsonConvert.SerializeObject(Object);
                info.AddValue("objValue", json);
            }
        }
    }

    /// <summary>
    /// https://www.danylkoweb.com/Blog/the-fastest-way-to-mock-a-database-for-unit-testing-B6
    /// </summary>
    //public static class DbSetExtensions
    //{
    //    public static DbSet<T> GetQueryableMockDbSet<T>(this IEnumerable<T> sourceList) where T : class
    //    {
    //        var queryable = sourceList.AsQueryable();
    //        var asList = sourceList.ToList();
    //        //var localList = new ObservableCollection<T>(queryable);

    //        var dbSet = new Mock<DbSet<T>>();
    //        dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
    //        dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
    //        dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
    //        dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

    //        //dbSet.Setup(m => m.Set<T>().Local).Returns(new ObservableCollection<T>(queryable));

    //        dbSet.Setup(m => m.Add(It.IsAny<T>())).Callback((T localVal) => asList.Add(localVal));
    //        dbSet.Setup(m => m.Remove(It.IsAny<T>())).Callback((T localVal) => asList.Remove(localVal));

    //        dbSet.Setup(m => m.Local).Returns(new ObservableCollection<T>(sourceList));

    //        return dbSet.Object;
    //    }

    //    public static DbSet<T> GetQueryableMockDbSet<T>(List<T> sourceList) where T : class
    //    {
    //        var queryable = sourceList.AsQueryable();
        
    //        var dbSet = new Mock<DbSet<T>>();
    //        dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
    //        dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
    //        dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
    //        dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());

    //        //dbSet.Setup(m => m.Set<T>().Local).Returns(new ObservableCollection<T>(sourceList));

    //        dbSet.Setup(m => m.Add(It.IsAny<T>())).Callback((T localVal) => sourceList.Add(localVal));
    //        dbSet.Setup(m => m.Remove(It.IsAny<T>())).Callback((T localVal) => sourceList.Remove(localVal));

    //        dbSet.Setup(m => m.Local).Returns(new ObservableCollection<T>(sourceList));

    //        return dbSet.Object;
    //    }

    //    //https://stackoverflow.com/questions/5811478/find-differences-between-two-entities-of-the-same-type
    //    public static IEnumerable<string> EnumeratePropertyDifferences<T>(this T obj1, T obj2)
    //    {
    //        PropertyInfo[] properties = typeof(T).GetProperties();
    //        List<string> changes = new List<string>();

    //        foreach (PropertyInfo pi in properties)
    //        {
    //            object value1 = typeof(T).GetProperty(pi.Name).GetValue(obj1, null);
    //            object value2 = typeof(T).GetProperty(pi.Name).GetValue(obj2, null);

    //            if (value1 != value2 && (value1 == null || !value1.Equals(value2)))
    //            {
    //                changes.Add(string.Format("Property {0} changed from {1} to {2}", pi.Name, value1, value2));
    //            }
    //        }
    //        return changes;
    //    }
    //}
}
