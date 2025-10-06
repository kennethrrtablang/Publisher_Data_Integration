using System.ComponentModel;

namespace Publisher_Data_Operations.Extensions
{
    public class RowIdentity : IChangeTracking
    {
        // Setting a new blank identity doesn't trip IsChanged
        public RowIdentity()
        {
            _clientID = -1;
            _lobID = -1;
            _docTypeID = -1;
            _documentCode = "";
            AcceptChanges();
        }

        // This can trigger IsChanged
        public RowIdentity(int docTypeID, int clientID, int lobID, string documentCode)
        {
            ClientID = clientID;
            LOBID = lobID;
            DocumentTypeID = docTypeID;
            DocumentCode = documentCode;
        }

        public void Update(int docTypeID, int clientID, int lobID, string documentCode)
        {
            ClientID = clientID;
            LOBID = lobID;
            DocumentTypeID = docTypeID;
            DocumentCode = documentCode;
        }

        public int _clientID;
        public int ClientID { get=> _clientID;
            set
            {
                if (_clientID != value)
                {
                    _clientID = value;
                    IsChanged = true;
                }
            }
        }
      
        public int _lobID;
        public int LOBID
        {
            get => _lobID;
            set
            {
                if (_lobID != value)
                {
                    _lobID = value;
                    IsChanged = true;
                }
            }
        }

        public int _docTypeID;
        public int DocumentTypeID
        {
            get => _docTypeID;
            set
            {
                if (_docTypeID != value)
                {
                    _docTypeID = value;
                    IsChanged = true;
                }
            }
        }
        public string _documentCode;
        public string DocumentCode
        {
            get => _documentCode;
            set
            {
                if (_documentCode != value)
                {
                    _documentCode = value;
                    IsChanged = true;
                }
            }
        }
        
        public bool IsChanged { get; private set; }
        public void AcceptChanges() => IsChanged = false;

        /// <summary>
        /// Allow cloning the object
        /// </summary>
        /// <returns>the cloned object</returns>
        public RowIdentity Clone()
        {
            return (RowIdentity)this.MemberwiseClone();
        }
    }
}
