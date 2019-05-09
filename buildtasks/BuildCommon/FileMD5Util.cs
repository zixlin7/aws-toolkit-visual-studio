using System;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using System.Net;

namespace BuildCommon
{
    public static class FileMD5Util
    {
        public static string GenerateMD5Hash(string resourceUri)
        {
            return HashContent(FetchResource(resourceUri));
        }

        public static int GenerateAndCompareMD5Hash(string resourceUri, string checkSum)
        {
            string hash = HashContent(FetchResource(resourceUri));
            return string.Compare(hash, checkSum, true);
        }

        // inspects the supplied uri for the file to checksum and downloads to a temp local
        // if necessary, returning the full path of a local resource
        static string FetchResource(string uri)
        {
            if (uri.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase))
            {
                string localTemp = Path.GetTempFileName();
                WebClient wc = new WebClient();
                wc.DownloadFile(uri, localTemp);
                return localTemp;
            }
            else
                return uri;
        }

        static string HashContent(string fileName)
        {
            byte[] retVal;
            using (FileStream file = new FileStream(fileName, FileMode.Open))
            {
                MD5 md5 = new MD5CryptoServiceProvider();
                retVal = md5.ComputeHash(file);
                file.Close();
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
