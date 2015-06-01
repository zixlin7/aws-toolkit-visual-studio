using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Web;
using System.Security.Cryptography;
using AWSDeploymentCryptoUtility;
using ThirdParty.Json.LitJson;

namespace AWSDeploymentHostManagerClient
{
    public class HostManagerClientRequest
    {
        private string hostname = "localhost";
        private string instance_id = "i-00000000";
        private string reservation_id = "r-00000000";
        private string taskName = "Status";
        private IDictionary<string, string> parameters = new Dictionary<string, string>();
        private CryptoUtil.EncryptionKeyTimestampIntegrator ec2MetaDataIntegrator;
        private bool verbose = false;

        public string Hostname
        {
            get { return hostname; }
            set { hostname = value; }
        }

        public string InstanceId
        {
            get { return instance_id; }
            set { instance_id = value; }
        }

        public string ReservationId
        {
            get { return reservation_id; }
            set { reservation_id = value; }
        }

        public string TaskName
        {
            get { return taskName; }
            set { taskName = value; }
        }

        public IDictionary<string, string> Parameters
        {
            get { return parameters; }
        }

        public bool Verbose
        {
            get { return verbose; }
            set { verbose = value; }
        }

        public string SendRequest()
        {
            ec2MetaDataIntegrator = delegate(string ts)
            {
                return String.Format("{0}{1}{2}", instance_id, reservation_id, ts);
            };

            string requestUrl = String.Format("http://{0}/_hostmanager/tasks", hostname);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUrl);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            StringBuilder sb = new StringBuilder();
            JsonWriter json = new JsonWriter(sb);

            json.WriteObjectStart();

            json.WritePropertyName("name");
            json.Write(taskName);

            if (parameters.Count > 0)
            {
                json.WritePropertyName("parameters");
                json.WriteObjectStart();

                foreach (KeyValuePair<string, string> kv in parameters)
                {
                    json.WritePropertyName(kv.Key);
                    json.Write(kv.Value);
                }

                json.WriteObjectEnd();
            }

            json.WriteObjectEnd();

            if (verbose)
                Console.WriteLine("Inner Request: {0}", sb.ToString());

            string requestBody = HttpUtility.UrlEncode(EncryptRequest(sb.ToString(), ec2MetaDataIntegrator));
            request.ContentLength = requestBody.Length;
            StreamWriter sw = new StreamWriter(request.GetRequestStream());

            if (verbose)
                Console.WriteLine("Outer Request: {0}", requestBody);

 
            sw.Write(requestBody);
            sw.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            StreamReader sr = new StreamReader(response.GetResponseStream());

            string responseBody = sr.ReadToEnd().Trim();
            if (string.IsNullOrEmpty(responseBody))
            {
                return "No Body Returned";
            }

            JsonData jData = DecryptResponse(responseBody, ec2MetaDataIntegrator);

            return JsonMapper.ToJson(jData);
        }

        private static string EncryptRequest(string json, CryptoUtil.EncryptionKeyTimestampIntegrator integrator)
        {
            byte[] iv = Aes.Create().IV;
            string timestamp = CryptoUtil.Timestamp();

            return CryptoUtil.EncryptResponse(json, iv, timestamp, integrator);
        }

        private static JsonData DecryptResponse(string json, CryptoUtil.EncryptionKeyTimestampIntegrator integrator)
        {
            return CryptoUtil.DecryptRequest(json, integrator);
        }

    }
}
