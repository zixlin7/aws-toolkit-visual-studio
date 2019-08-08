using System;
using ThirdParty.Json.LitJson;
using Amazon.CloudFormation.Model;
using Amazon.EC2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using AWSDeploymentCryptoUtility;
using AWSDeploymentHostManagerClient;

namespace Amazon.AWSToolkit.Util
{
    public static class CloudFormationUtil
    {
        public const string BucketOutputParam = "Bucket";
        public const string ConfigFileOutputParam = "ConfigFile";

        /// <summary>
        /// Fetches and decrypts the current deployment config file for a given regional
        /// deployment and application, returning the parsed json data on completion.
        /// </summary>
        public static JsonData GetConfig(IAmazonS3 s3client, Stack runningStack, Reservation stackReservation)
        {
            return GetConfig(
                s3client, runningStack, stackReservation,
                string.Empty, string.Empty);
        }

        /// <summary>
        /// Fetches and decrypts the current deployment config file for a given regional
        /// deployment and application, returning the parsed json data on completion.
        /// </summary>
        public static JsonData GetConfig(
            IAmazonS3 s3client,
            Stack runningStack,
            Reservation stackReservation,
            string defaultBucketName, string defaultConfigFileKey)
        {
            JsonData configData = null;

            if (stackReservation == null)
                throw new Exception("Failed to obtain reservation for running instance, cannot proceed with reading prior configuration");

            // to allow the toolkit to be able to see stacks deployed using the standalone tool,
            // determine the bucket name from the stack itself as that can be specified by the user
            // in the deployment config file. The config file key is predictable however we can also
            // get that from the running stack too, 'just in case'.
            string configFileKey;
            string bucketName;
            DeterminePriorBucketAndConfigNames(runningStack, out bucketName, out configFileKey);

            // if it wasn't a quality stack; fall back to toolkit defaults
            if (string.IsNullOrEmpty(bucketName))
            {
                if (string.IsNullOrEmpty(defaultBucketName))
                    throw new Exception("Unable to determine bucket name to retrieve config");
                bucketName = defaultBucketName;
            }

            if (string.IsNullOrEmpty(configFileKey))
            {
                if (string.IsNullOrEmpty(defaultConfigFileKey))
                    throw new Exception("Unable to determine config key to retrieve config from bucket " + bucketName);
                configFileKey = defaultConfigFileKey;
            }

            var response = s3client.GetObject(new GetObjectRequest() { BucketName = bucketName, Key = configFileKey });

            string responseBody;
            using (StreamReader sr = new StreamReader(response.ResponseStream))
            {
                responseBody = sr.ReadToEnd().Trim();
            }

            if (string.IsNullOrEmpty(responseBody))
                throw new Exception(string.Format("Failed to download deployment config file {0} from bucket {1}", configFileKey, bucketName));

            Byte[] key;
            Byte[] iv;
            FetchDecryptionKeys(stackReservation, out key, out iv);

            string config = CryptoUtil.DecryptFromBase64EncodedString(responseBody, key, iv);
            configData = JsonMapper.ToObject(config);

            return configData;
        }

        public static void DeterminePriorBucketAndConfigNames(Stack runningStack, out string bucketName, out string configFileKey)
        {
            bucketName = string.Empty;
            configFileKey = string.Empty;
            if (runningStack.Outputs != null)
            {
                foreach (Output output in runningStack.Outputs)
                {
                    // we don't have many outputs right now but in case we should, bomb out as soon as possible
                    if (!string.IsNullOrEmpty(bucketName) && !string.IsNullOrEmpty(configFileKey))
                        break;

                    if (string.IsNullOrEmpty(bucketName) && string.Compare(output.OutputKey, BucketOutputParam, true) == 0)
                    {
                        bucketName = output.OutputValue;
                        continue;
                    }

                    if (string.IsNullOrEmpty(configFileKey) && string.Compare(output.OutputKey, ConfigFileOutputParam, true) == 0)
                    {
                        configFileKey = output.OutputValue;
                        continue;
                    }
                }
            }
        }

        /// <summary>
        /// Contact a running instance to get the necessary decryption info to decrypt the 
        /// deployment config file
        /// </summary>
        private static void FetchDecryptionKeys(Reservation reservation, out Byte[] key, out Byte[] iv)
        {
            key = null;
            iv = null;

            HostManagerClientRequest client = new HostManagerClientRequest();
            client.InstanceId = reservation.Instances[0].InstanceId;
            client.ReservationId = reservation.ReservationId;
            client.Hostname = reservation.Instances[0].PublicIpAddress;

            client.TaskName = "SystemInfo";

            string responseStr = null;
            int wait = 0;

            while (responseStr == null)
            {
                try
                {
                    responseStr = client.SendRequest();
                }
                catch (Exception)
                {
                    int delay = (int)Math.Pow(4, wait) * 100;
                    System.Threading.Thread.Sleep(delay);
                    wait++;
                    if (wait == 5)
                    {
                        throw;
                    }
                }
            }

            JsonData response = JsonMapper.ToObject(responseStr);
            JsonData payload = JsonMapper.ToObject((string)response["payload"]);

            key = Convert.FromBase64String((string)payload["key"]);
            iv = Convert.FromBase64String((string)payload["iv"]);
        }
    }
}
