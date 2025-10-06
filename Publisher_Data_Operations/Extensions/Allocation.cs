using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Publisher_Data_Operations.Helper;

namespace Publisher_Data_Operations.Extensions
{
    public class AllocationItem : IEquatable<AllocationItem>, IComparable<AllocationItem>
    {
        public string Allocation { get; set; }
        public string EnglishText { get; set; }
        public string FrenchText { get; set; }
        public string FieldName { get; set; }
        public string EnglishHeader { get; set; }
        public string FrenchHeader { get; set; }
       
        public string MatchText { get; private set; }

        public AllocationItem(string allocation, string englishText, string frenchText, string fieldName = null, string englishHead = null, string frenchHead = null)
        {
            Allocation = allocation;
            FieldName = fieldName;
            EnglishHeader = englishHead;
            FrenchHeader = frenchHead;
            EnglishText = englishText;
            FrenchText = frenchText;

            MatchText = allocation.StripTagsExceptAlpha();
        }

        /// <summary>
        /// Return the numeric portion of the FieldName
        /// </summary>
        /// <returns>int if found in FieldName or -1</returns>
        public int FieldNumber()
        {
            if (FieldName == string.Empty)
                return -1;
            else if (int.TryParse(new string(FieldName.Where(Char.IsDigit).ToArray()), out int tempInt))
                return tempInt;
            else
                return -1;
        }

        /// <summary>
        /// Use the FieldName for comparisons to see if the Allocation item is equal
        /// </summary>
        /// <param name="other"></param>
        /// <returns>True if equal</returns>
        public bool Equals(AllocationItem other)
        {
            if (other == null) return false;
            return this.FieldName == other.FieldName;
        }

        /// <summary>
        /// Used to sort the AllocationItem List - null and empty strings are sorted to the end 
        /// </summary>
        /// <param name="other"></param>
        /// <returns>1 if other is less than this, 0 if equal, and -1 if other is greater than this</returns>
        public int CompareTo(AllocationItem other)
        {
            // A null or blank value is greater (sort after any specific values)
            if (this.Equals(other))
                return 0;
            else if (other == null || other.FieldName == string.Empty)
                return -1;
            else if (string.IsNullOrEmpty(this.FieldName))
                return 1;
            else
                return this.FieldName.CompareTo(other.FieldName);
        }
    }

    public class AllocationTable
    {
        public string FundCode { get; set; }
        public List<AllocationItem> Allocation { get; set; }

        public AllocationTable(string fundCode, string allocation, string englishText, string frenchText, string fieldName = null, string englishHead = null, string frenchHead = null)
        {
            FundCode = fundCode;
            Allocation = new List<AllocationItem>() { new AllocationItem(allocation, englishText, frenchText, fieldName, englishHead, frenchHead) };
        }

        /// <summary>
        /// Sorts the AllocationItem List using it's built in CompareTo
        /// </summary>
        public void ExistingOrder() => Allocation.Sort((x, y) => x.CompareTo(y));

        public Queue<AllocationItem> MatchedQueue()
        {
            Queue<AllocationItem> matchQueue = new Queue<AllocationItem>();
            foreach (AllocationItem ai in this.Allocation)
                if (ai.FieldName != null && ai.FieldName.Length > 0)
                    matchQueue.Enqueue(ai);

            return matchQueue;
        }

        public Queue<AllocationItem> UnMatchedQueue()
        {
            Queue<AllocationItem> unMatchedQueue = new Queue<AllocationItem>();
            foreach (AllocationItem ai in this.Allocation)
                if (ai.FieldName == null || ai.FieldName.Length <= 0)
                    unMatchedQueue.Enqueue(ai);

            return unMatchedQueue;
        }
    }

    /// <summary>
    /// An extension for a List of AllocationTable
    /// </summary>
    public class AllocationList : List<AllocationTable>
    {
        private Generic _gen = null;
        private PDIFile _fileName = null;

        private int _clientID = -1;
        private int _lobID = -1;
        private int _docTypeID = -1;
        private int _jobID = -1;

        private Logger _log = null;

        public AllocationList(object con, PDIFile fileName, Logger log)
        {
            _gen = new Generic(con, log);
            _log = log;
            _fileName = fileName;
            _clientID = _fileName.ClientID.HasValue ? (int)_fileName.ClientID : -1;
            _lobID = _fileName.LOBID.HasValue ? (int)_fileName.LOBID : -1;
            _docTypeID = _fileName.DocumentTypeID.HasValue ? (int)_fileName.DocumentTypeID : -1;
            _jobID = _fileName.JobID.HasValue ? (int)_fileName.JobID : -1;
        }

        //public AllocationList(object con, int clientID, int lobID, int docTypeID, int jobID)
        //{
        //    _gen = new Generic(con);
        //    _clientID = clientID;
        //    _lobID = lobID;
        //    _docTypeID = docTypeID;
        //    _jobID = jobID;
        //}

        /// <summary>
        /// Add a new allocation by adding to existing Allocation if the fundCode is already present or creating a new AllocationTable if not
        /// </summary>
        /// <param name="fundCode"></param>
        /// <param name="allocation"></param>
        /// <param name="englishText"></param>
        /// <param name="frenchText"></param>
        /// <param name="fieldName"></param>
        /// <param name="englishHead"></param>
        /// <param name="frenchHead"></param>
        public void AddCode(string fundCode, string allocation, string englishText, string frenchText, string fieldName = null, string englishHead = null, string frenchHead = null, string altFieldName = null)
        {
            AllocationTable at;

            //lookup the French values from the English

            if (englishHead.IsNaOrBlank())
            {
                englishHead = allocation;
                frenchHead = _gen.verifyFrenchTableText(frenchHead, englishHead, new RowIdentity(_docTypeID, _clientID, _lobID, fundCode), _jobID, fieldName ?? altFieldName);
            }

            if (!englishText.IsNaOrBlank()) 
            {
                frenchText = _gen.verifyFrenchTableText(frenchText, englishText, new RowIdentity(_docTypeID, _clientID, _lobID, fundCode), _jobID, fieldName ?? altFieldName);
                frenchText = Generic.MakeTableString(frenchText);
                englishText = Generic.MakeTableString(englishText);
            }
            

            at = FindByCode(fundCode);
            AllocationItem tempItem = null;
            if (at != null)
            {
                // for new existing matching update the existing Allocation Item if it exists (on match) instead of using linq to match the tables
                tempItem = at.Allocation.Find(x => x.MatchText == allocation.StripTagsExceptAlpha());
                if (tempItem != null)
                {
                    tempItem.Allocation = englishHead;
                    tempItem.EnglishHeader = englishHead;
                    tempItem.FrenchHeader = frenchHead;
                    tempItem.FieldName = fieldName;
                }
                else
                    at.Allocation.Add(new AllocationItem(allocation, englishText, frenchText, fieldName, englishHead, frenchHead));
            }
            else
                this.Add(new AllocationTable(fundCode, allocation, englishText, frenchText, fieldName, englishHead, frenchHead));
        }
        
        /// <summary>
        /// Given a FundCode find a matching record in this class
        /// </summary>
        /// <param name="fundCode">The FundCode to search for</param>
        /// <returns>The AllocationTable if found or null</returns>
        private AllocationTable FindByCode(string fundCode)
        {
            foreach (AllocationTable at in this)
            {
                if (at.FundCode == fundCode)
                    return at;
            }
            return null;
        }

        /// <summary>
        /// Order this and all the AlloctionItems in the contained AllocationTables - the lists will then be ordered by FundCode and then Field_Name starting with non-empty Field_Names
        /// </summary>
        public void OrderAll()
        {
            foreach (AllocationTable at in this)
                at.ExistingOrder();
            this.Sort((x, y) => x.FundCode.CompareTo(y.FundCode));
        }

        /// <summary>
        /// Creates the first part of the Field_Name based on the current index, the offset, and the DataType
        /// </summary>
        /// <param name="index">The current index location</param>
        /// <param name="prepend">String to start the Field_Name - should be the Data Type</param>
        /// <param name="offset">Optional offset of where the Field_Name number sequence starts - defaults to 30</param>
        /// <returns>The resulting string</returns>
        public string GetFieldName(int index, string prepend, int offset = 30)
        {
            return prepend + (index + offset).ToString();
        }

        /// <summary>
        /// Add the resulting Allocation Tables data to the Data_Staging table by appending a row for each Field_Name and header (when required) The structure of the DataTable is assumed
        /// </summary>
        /// <param name="dt">The DataTable to append to</param>
        /// <param name="jobID">The current Job_ID</param>
        /// <param name="sheetName">The name of the source data sheet</param>
        /// <param name="dataType">The Data Type Code</param>
        /// <param name="offset">The optional offset for Field_Name - defaults to 30</param>
        public void AppendToDataTable(System.Data.DataTable dt, int jobID, string sheetName, string dataType, bool overrideHeaders = false, int offset = 30)
        {
            OrderAll(); // make sure the contents are sorted before generating the output

            foreach (AllocationTable at in this)
            {

                Queue<AllocationItem> matchedQueue = at.MatchedQueue();
                Queue<AllocationItem> unMatchedQueue = at.UnMatchedQueue();
                AllocationItem curItem = null;
                for (int i = 0; i <= 8; i++) //Loop through the 9 possible values
                {
                  
                    string genFieldName = GetFieldName(i, dataType, offset); // generate the field name for the current item
                    // if there is a problem in publisher resulting in multiple different matching options then the next matchedQueue item might be a duplicate and it will never pop - in that case toss any that are below the current count and offset
                    while (matchedQueue.Count > 0 && i + offset > matchedQueue.Peek().FieldNumber())
                    {
                        curItem = matchedQueue.Dequeue();
                        Logger.AddError(_log, $"Extra Matched value found for {curItem.FieldName} containing {curItem.EnglishHeader} in {at.FundCode}");
                    }
                    if (matchedQueue != null && matchedQueue.Count > 0 && genFieldName + "h" == matchedQueue.Peek().FieldName) // does the next matched item have the same field name?
                        curItem = matchedQueue.Dequeue();  
                    else if (matchedQueue != null && unMatchedQueue.Count > 0) // if we hae an unmatched item use it when we don't have a matched that matches the field
                        curItem = unMatchedQueue.Dequeue();
                    else
                        curItem = null; // null if we don't have an unmatched item

                    //while (matchedQueue != null && matchedQueue.Count > 0 && genFieldName + "h" == matchedQueue.Peek().FieldName) // this isn't supposed to happen - this means there were multiple results for a single field - get rid of the extras
                    //    matchedQueue.Dequeue();

                    if (curItem is null) // clear the field as we don't have a matched or unmatched to update
                    {
                        dt.Rows.Add(new object[] { jobID, at.FundCode, sheetName, genFieldName + "_EN", i, 0, Transform.EmptyTable }); //English
                        dt.Rows.Add(new object[] { jobID, at.FundCode, sheetName, genFieldName + "_FR", i, 0, Transform.EmptyTable }); //French

                        dt.Rows.Add(new object[] { jobID, at.FundCode, sheetName, genFieldName + "h_EN", i, 0, Transform.EmptyText }); //English Header
                        dt.Rows.Add(new object[] { jobID, at.FundCode, sheetName, genFieldName + "h_FR", i, 0, Transform.EmptyText }); //French Header
                    }
                    else // use the current item for the table content
                    {
                        if (!curItem.EnglishText.IsNaOrBlank()) // don't output the table data if it isn't provided in the BAU but we have a header from PROD (Target Click)
                        {
                            dt.Rows.Add(new object[] { jobID, at.FundCode, sheetName, genFieldName + "_EN", i, 0, curItem.EnglishText }); //English
                            dt.Rows.Add(new object[] { jobID, at.FundCode, sheetName, genFieldName + "_FR", i, 0, curItem.FrenchText }); //French
                        }
                        if (overrideHeaders || genFieldName + "h" != curItem.FieldName) // only output the header when the field name of the current item doesn't match the generated name (should only be when unmatched)
                        {
                            // at creation of the AllocationItem the English and French header text are created so always use the headers in the object when they don't match existing
                            dt.Rows.Add(new object[] { jobID, at.FundCode, sheetName, genFieldName + "h_EN", i, 0, curItem.EnglishHeader }); //English Header
                            dt.Rows.Add(new object[] { jobID, at.FundCode, sheetName, genFieldName + "h_FR", i, 0, curItem.FrenchHeader }); //French Header
                        }
                    }
                }
                if (matchedQueue.Count > 0 || unMatchedQueue.Count > 0)
                    Logger.AddError(_log, $"Queue Items remaining - Matched: {matchedQueue.Count} UnMatched: {unMatchedQueue.Count} in {at.FundCode}");
            }
        }
    }

}
