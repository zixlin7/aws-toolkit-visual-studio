using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;

using ThirdParty.Json.LitJson;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.S3;
using Amazon.S3.Model;

using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.Navigator.Node;

using AWSDeploymentCryptoUtility;

namespace Amazon.AWSToolkit.CloudFormation.Util
{
    internal class CloudFormationUtil
    {
        private IAmazonCloudFormation _cfClient;
        private IAmazonEC2 _ec2Client;
        private CloudFormationRootViewModel _viewModel;

        public static CryptoUtil.EncryptionKeyTimestampIntegrator ConstructIntegrator(string instanceID, string reservationID)
        {
            return delegate(string ts)
            {
                return string.Format("{0}{1}{2}", instanceID, reservationID, ts);
            };
        }

        public CloudFormationUtil(CloudFormationRootViewModel model)
        {
            _viewModel = model;
            
            var region = this._viewModel.CurrentEndPoint.RegionSystemName;
            var endPoints = RegionEndPointsManager.Instance.GetRegion(region);

            this._cfClient = this._viewModel.CloudFormationClient;

            var ec2Config = new AmazonEC2Config {ServiceURL = endPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME).Url};
            this._ec2Client = new AmazonEC2Client(this._viewModel.AccountViewModel.Credentials, ec2Config);
        }
        public CloudFormationUtil(IAmazonCloudFormation cfClient, IAmazonEC2 ec2Client)
        {
            _cfClient = cfClient;
            _ec2Client = ec2Client;
        }

        public Dictionary<string, string> GetDeploymentLogs(string stackName)
        {
            var instanceIDs = new List<string>();
            var reservationIDs = new List<string>();
            var ips = new List<string>();
            var deploymentLogs = new Dictionary<string, string>();

            GetCodingData(stackName, instanceIDs, reservationIDs, ips);

            for (int i = 0; i < instanceIDs.Count; i++)
            {
                string deploymentLog = GetDeploymentLog(instanceIDs[i], reservationIDs[i], ips[i]);
                deploymentLogs.Add(instanceIDs[i], deploymentLog);
            }

            return deploymentLogs;
        }

        public string GetDeploymentLog(string instance)
        {
            string reservation = null;
            string ip = null;

            DescribeInstancesResponse response2 = _ec2Client.DescribeInstances(new DescribeInstancesRequest() { InstanceIds = new List<string>() { instance } });

            reservation = response2.Reservations[0].ReservationId;

            foreach (var ri in response2.Reservations[0].Instances)
            {
                if (ri.InstanceId.Equals(instance))
                {
                    ip = ri.PublicIpAddress;
                    break;
                }
            }

            return GetDeploymentLog(instance, reservation, ip);
        }

        public static string GetDeploymentLog(RunningInstanceWrapper wrapper)
        {
            return GetDeploymentLog(wrapper.InstanceId, wrapper.ReservationId, wrapper.IpAddress);
        }

        private static string GetDeploymentLog(string instance, string reservation, string ip)
        {
            return GetLog(instance, reservation, ip, "Deployment.log", -3);
        }

        private static string GetLog(string instance, string reservation, string ip, string log, int lines)
        {
            CryptoUtil.EncryptionKeyTimestampIntegrator keymatter = ConstructIntegrator(instance, reservation);

            String url = String.Format("http://{0}:80/_hostmanager/tasks", ip);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";

            Byte[] iv = Aes.Create().IV;
            string requestBody = HttpUtility.UrlEncode(CryptoUtil.EncryptResponse(
                String.Format("{{\"name\":\"Tail\",\"parameters\":{{\"lines\":\"{0}\",\"log\":\"{1}\"}}}}", lines, log),
                iv, 
                CryptoUtil.Timestamp(), 
                keymatter));

            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = requestBody.Length;

            StreamWriter requestStream = new StreamWriter(request.GetRequestStream());
            requestStream.Write(requestBody);
            requestStream.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());

            string responseBody = sr.ReadToEnd().Trim();

            JsonData jData = CryptoUtil.DecryptRequest(responseBody, keymatter);
            JsonData jPayload = jData["payload"];
            JsonData jResponse = JsonMapper.ToObject(jPayload.ToString())["response"];
            string sResponse = jResponse.ToString();

            byte[] bytes = Convert.FromBase64String(sResponse);
            return Encoding.UTF8.GetString(bytes);
        }

        private void GetCodingData(string stackName, List<string> instanceIds, List<string> reservationIds, List<string> ips)
        {
            DescribeStackResourcesResponse response = _cfClient.DescribeStackResources(new DescribeStackResourcesRequest(){StackName = stackName});

            foreach (StackResource resource in response.StackResources)
            {
                if ("AWS::EC2::Instance".Equals(resource.ResourceType))
                {
                    instanceIds.Add(resource.PhysicalResourceId);

                    DescribeInstancesResponse response2 = _ec2Client.DescribeInstances(new DescribeInstancesRequest() { InstanceIds = new List<string>() { resource.PhysicalResourceId } });

                    reservationIds.Add(response2.Reservations[0].ReservationId);

                    foreach (Instance ri in response2.Reservations[0].Instances)
                    {
                        if (ri.InstanceId.Equals(resource.PhysicalResourceId))
                        {
                            ips.Add(ri.PublicIpAddress);
                            break;
                        }
                    }
                }
            }
        }

        internal static string UploadTemplateToS3(Account.AccountViewModel account, RegionEndPointsManager.RegionEndPoints region, string templateBody, string templateName, string stack)
        {
            var s3Client = account.CreateServiceClient<AmazonS3Client>(region);

            string uniqueIdentifier = string.IsNullOrEmpty(account.AccountNumber) ? account.CredentialKeys.AccessKey : account.AccountNumber;
            string bucketName = string.Format("cloudformation-{0}-{1}", region.SystemName, uniqueIdentifier).ToLower();
            string s3Key = string.Format("{0}/{1}", stack, templateName);

            try
            {
                s3Client.PutBucket(new PutBucketRequest()
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                });
            }
            catch (AmazonS3Exception) { }

            ToolkitFactory.Instance.ShellProvider.UpdateStatus(string.Format("Uploading template to S3 bucket {0}", bucketName));
            s3Client.PutObject(new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = s3Key,
                ContentBody = templateBody
            });

            string url = null;

            var s3Host = region.GetEndpoint(RegionEndPointsManager.S3_SERVICE_NAME);            
            url = String.Format("{0}{1}/{2}", s3Host.Url, bucketName, s3Key);
            
            return url;
        }

        internal static void OpenStack(Account.AccountViewModel account, RegionEndPointsManager.RegionEndPoints region, string stackName)
        {
            ToolkitFactory.Instance.ShellProvider.UpdateStatus("Displaying CloudFormation Stack View");
            if (ToolkitFactory.Instance.Navigator.SelectedAccount != account)
                ToolkitFactory.Instance.Navigator.UpdateAccountSelection(new Guid(account.SettingsUniqueKey), false);

            if (ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints != region)
                ToolkitFactory.Instance.Navigator.UpdateRegionSelection(region);

            var cloudFormationRootNode = ToolkitFactory.Instance.Navigator.SelectedAccount.FindSingleChild<Nodes.CloudFormationRootViewModel>(false);
            if (cloudFormationRootNode == null)
                return;

            var stackNode = cloudFormationRootNode.FindSingleChild<Nodes.CloudFormationStackViewModel>(false, x => string.Equals(x.StackName, stackName));
            if (stackNode == null)
            {
                cloudFormationRootNode.Refresh(false);
                stackNode = cloudFormationRootNode.FindSingleChild<Nodes.CloudFormationStackViewModel>(false, x => string.Equals(x.StackName, stackName));
            }

            if (stackNode != null)
            {
                ToolkitFactory.Instance.Navigator.SelectedNode = stackNode;
                stackNode.ExecuteDefaultAction();
            }
        }
    }
}
