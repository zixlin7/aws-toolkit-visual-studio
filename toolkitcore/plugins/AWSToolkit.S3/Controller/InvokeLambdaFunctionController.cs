using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Amazon.S3;
using Amazon.Lambda;
using Amazon.Lambda.Model;

using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class InvokeLambdaFunctionController
    {
        IAmazonS3 _s3Client;
        string _bucketName;
        IList<string> _objectKeys;

        InvokeLambdaFunctionModel _model;
 
        public InvokeLambdaFunctionController(IAmazonS3 s3Client, string bucketName, IList<string> objectKeys)
        {
            this._s3Client = s3Client;
            this._bucketName = bucketName;
            this._objectKeys = objectKeys;            
        }

        public bool Execute()
        {
            this.LoadModel();
            var control = new InvokeLambdaFunctionControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }

        public void LoadModel()
        {
            this._model = new InvokeLambdaFunctionModel();

            foreach(var region in RegionEndPointsManager.GetInstance().Regions)
            {
                if (region.GetEndpoint(RegionEndPointsManager.LAMBDA_SERVICE_NAME) == null)
                    continue;

                this._model.Regions.Add(region);
                if (ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints == region)
                    this._model.SelectedRegion = region;
            }

            if (this._model.SelectedRegion == null && this._model.Regions.Count > 0)
                this._model.SelectedRegion = this._model.Regions[0];

            this._model.SelectedEventType = EventType.ObjectCreatedPut;
            this._model.EventTypes.Add(EventType.ObjectCreatedCompleteMultipartUpload);
            this._model.EventTypes.Add(EventType.ObjectCreatedCopy);
            this._model.EventTypes.Add(EventType.ObjectCreatedPost);
            this._model.EventTypes.Add(EventType.ObjectCreatedPut);

            if (this._model.SelectedRegion != null)
                this.LoadFunctions();
        }

        public void LoadFunctions()
        {
            var client = CreateLambdaClient();
            var functionNames = new List<string>();
            var response = new ListFunctionsResponse();

            do
            {
                response = client.ListFunctions(new ListFunctionsRequest { Marker = response.NextMarker });
                foreach(var function in response.Functions)
                {
                    functionNames.Add(function.FunctionName);
                }


            } while (!string.IsNullOrEmpty(response.NextMarker));

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread(((Action)(() =>
            {
                this._model.Functions.Clear();
                this._model.SelectedFunction = null;

                foreach (var functionName in functionNames.OrderBy(x => x))
                {
                    this._model.Functions.Add(functionName);
                }

                if (this._model.Functions.Count > 0)
                    this._model.SelectedFunction = this._model.Functions[0];
                else
                    this._model.SelectedFunction = null;
            })));

        }

        private IAmazonLambda CreateLambdaClient()
        {
            var accountModel = ToolkitFactory.Instance.Navigator.SelectedAccount;
            var client = accountModel.CreateServiceClient<AmazonLambdaClient>(this.Model.SelectedRegion.GetEndpoint(RegionEndPointsManager.LAMBDA_SERVICE_NAME));
            return client;
        }

        public InvokeLambdaFunctionModel Model => this._model;

        public void InvokeFunction()
        {
            var client = CreateLambdaClient();
            ThreadPool.QueueUserWorkItem(this.InvokeFunctionAsync, client);
        }

        private string GetBucketRegion()
        {
            var location = this._s3Client.GetBucketLocation(this._bucketName).Location;
            if (string.IsNullOrEmpty(location))
                return "us-east-1";
            else if (string.Equals("EU", location))
                return "eu-west-1";

            return location;
        }

        public void InvokeFunctionAsync(object state)
        {
            var bucketRegion = GetBucketRegion();
            ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(string.Format("Determine S3 bucket was in region {0}", bucketRegion), true);
            var client = state as IAmazonLambda;
            var request = new InvokeRequest
            {
                FunctionName = this.Model.SelectedFunction,
                InvocationType = InvocationType.Event
            };

            if (this._model.GroupInvokes)
            {
                try
                {
                    request.Payload = BuildFunctionArgument(bucketRegion, this._objectKeys);
                    client.InvokeAsync(request);
                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(
                        string.Format("Invoked {0} for {1} objects", this.Model.SelectedFunction, this._objectKeys.Count),
                        true);
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(
                                string.Format("Error invoking {0}: {1}", this.Model.SelectedFunction, e.Message),
                                true);
                }
            }
            else
            {
                foreach (var key in this._objectKeys)
                {
                    try
                    {
                        request.Payload = BuildFunctionArgument(bucketRegion, new string[] { key });
                        client.Invoke(request);
                        ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(
                            string.Format("Invoked {0} for object {1}", this.Model.SelectedFunction, key),
                            true);
                    }
                    catch(Exception e)
                    {
                        ToolkitFactory.Instance.ShellProvider.OutputToHostConsole(
                                    string.Format("Error invoking {0} for object {1}: {2}", this.Model.SelectedFunction, key,e.Message),
                                    true);
                    }
                }
            }
            
        }

        private string BuildFunctionArgument(string bucketRegion, IEnumerable<string> keys)
        {
            JsonData rootDoc = new JsonData();
            var records = new JsonData();
            rootDoc["Records"] = records;

            foreach (var s3Key in keys)
            {
                var recordDoc = new JsonData();
                records.Add(recordDoc);

                recordDoc["eventVersion"] = "2.0";
                recordDoc["eventSource"] = "aws:s3";
                recordDoc["awsRegion"] = bucketRegion;
                recordDoc["eventTime"] = DateTime.UtcNow.ToString(Amazon.Util.AWSSDKUtils.ISO8601DateFormat);
                recordDoc["eventName"] = this.Model.SelectedEventType;

                recordDoc["userIdentity"] = new JsonData();
                recordDoc["userIdentity"]["principalId"] = "AIDAJDPLRKLG7UEXAMPLE";

                recordDoc["requestParameters"] = new JsonData();
                recordDoc["requestParameters"]["sourceIPAddress"] = "127.0.0.1";

                recordDoc["responseElements"] = new JsonData();
                recordDoc["responseElements"]["x-amz-request-id"] = "C3D13FE58DE4C810";
                recordDoc["responseElements"]["x-amz-id-2"] = "FMyUVURIY8/IgAtTv8xRjskZQpcIZ9KG4V5Wp6S7S/JRWeUWerMUE5JgHvANOjpD";

                var s3Doc = new JsonData();
                recordDoc["s3"] = s3Doc;
                s3Doc["s3SchemaVersion"] = "1.0";
                s3Doc["configurationId"] = "testConfigRule";

                var bucket = new JsonData();
                s3Doc["bucket"] = bucket;
                bucket["name"] = this._bucketName;
                bucket["ownerIdentity"] = new JsonData();
                bucket["ownerIdentity"]["principalId"] = "A3NL1KOZZKExample";
                bucket["arn"] = string.Format("arn:aws:s3:::{0}", this._bucketName);

                var s3Object = new JsonData();
                s3Doc["object"] = s3Object;
                s3Object["key"] = s3Key;

                if (this.Model.GetLatestProperties)
                {
                    var headResponse = this._s3Client.GetObjectMetadata(this._bucketName, s3Key);
                    s3Object["size"] = headResponse.ContentLength;
                    s3Object["etag"] = headResponse.ETag;
                }
            }

            return rootDoc.ToJson();
        }
    }
}
