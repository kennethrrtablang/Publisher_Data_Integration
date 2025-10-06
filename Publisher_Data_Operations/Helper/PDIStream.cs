using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Publisher_Data_Operations.Helper
{
    public class PDIStream : IDisposable
    {
        private bool disposedValue;
        private Stream _sourceStream;

        public Stream SourceStream
        {
            get
            {
                if (_sourceStream != null && _sourceStream.CanSeek && _sourceStream.Position != 0)
                    _sourceStream.Position = 0;

                return _sourceStream;
            }
            set
            {

                if (_sourceStream is null)
                    _sourceStream = new MemoryStream();


                if (value is null)
                    return;

                if (value.CanSeek && value.Position != 0)
                    value.Position = 0;

                value.CopyTo(_sourceStream);
            }
        }

        public bool IsValidStream => _sourceStream != null && _sourceStream.Length > 0;
        public PDIFile PdiFile { get; set; }

        public PDIStream(string filePath, PDIFile pdiFileName = null)
        {
            if (File.Exists(filePath))
            {
                //SourceStream = new MemoryStream();
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    SourceStream = fs;

                if (pdiFileName is null)
                    PdiFile = new PDIFile(filePath);
                else
                    PdiFile = pdiFileName;
            }
        }

        public PDIStream(PDIFile pdiFileName, Stream stream)
        {
            SourceStream = stream;
            PdiFile = pdiFileName;
        }

        public PDIStream(PDIFile pdiFileName)
        {
            PdiFile = pdiFileName;
            if (File.Exists(PdiFile.FullPath))
            {
                //_sourceStream = new MemoryStream();
                using (FileStream fs = new FileStream(PdiFile.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    SourceStream = fs; //fs.CopyTo(_sourceStream);
            }
        }

        public PDIStream(Stream stream, string fileName, object con = null)
        {
            SourceStream = stream;
            if (con != null)
                PdiFile = new PDIFile(fileName, con, true);
            else
                PdiFile = new PDIFile(fileName);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _sourceStream.Dispose();
                }
                PdiFile = null;
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~PDIStream()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public static class StreamExtensions
    {
        public static string ToBase64String(this Stream stream)
        {
            if (stream != null && stream.CanSeek && stream.Position != 0)
                stream.Position = 0;

            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
