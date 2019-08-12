using System;
using System.IO;
using System.Net;

using log4net;

namespace Amazon.AWSToolkit.Util
{
    public static class IPAddressUtil
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(IPAddressUtil));

        const string EXTENAL_URL = "http://checkip.amazonaws.com/";
        /// <summary>
        /// Returns the IP address as seen from external source.  If the IP address fails to be determine then null is returned.
        /// </summary>
        /// <returns></returns>
        public static string DetermineIPFromExternalSource()
        {
            try
            {
                HttpWebRequest httpRequest = WebRequest.Create(EXTENAL_URL) as HttpWebRequest;
                using (HttpWebResponse response = httpRequest.GetResponse() as HttpWebResponse)
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        var ipaddress = reader.ReadToEnd();
                        if (ipaddress != null)
                            ipaddress = ipaddress.Trim();

                        if (ipaddress.Length > 5 && ipaddress.Length < 17 && 4 == ipaddress.Split('.').Length)
                            return ipaddress;

                        return null;
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to determine external IP address", e);
                return null;
            }
        }
    }
}
