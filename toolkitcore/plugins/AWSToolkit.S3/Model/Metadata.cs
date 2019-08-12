using System.Collections.Generic;
using System.Collections.Specialized;

namespace Amazon.AWSToolkit.S3.Model
{
    public class Metadata
    {
        public static HashSet<string> HEADER_NAMES = new HashSet<string>();

        static Metadata()
        {
            HEADER_NAMES.Add("Cache-Control");
            HEADER_NAMES.Add("Content-Disposition");
            HEADER_NAMES.Add("Content-Type");
            HEADER_NAMES.Add("Content-Language");
            HEADER_NAMES.Add("Expires");
            HEADER_NAMES.Add("Content-Encoding");
        }


        public Metadata()
        {
        }

        public Metadata(string key, string value)
        {
            this.Key = key;
            this.Value = value;
        }

        public string Key
        {
            get;
            set;
        }

        public string Value
        {
            get;
            set;
        }

        public static void GetMetadataAndHeaders(IList<Metadata> metadataEntries, 
            out NameValueCollection nvcMetadata, out NameValueCollection nvcHeader)
        {
            nvcMetadata = new NameValueCollection();
            nvcHeader = new NameValueCollection();
            foreach (Metadata entry in metadataEntries)
            {
                if (HEADER_NAMES.Contains(entry.Key))
                    nvcHeader[entry.Key] = entry.Value;
                else
                    nvcMetadata[entry.Key] = entry.Value;
            }
        }
    }
}
