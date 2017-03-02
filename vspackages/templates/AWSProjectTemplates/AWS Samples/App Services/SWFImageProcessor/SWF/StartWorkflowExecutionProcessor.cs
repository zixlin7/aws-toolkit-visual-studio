/*******************************************************************************
* Copyright 2009-2013 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

using Amazon.S3;
using Amazon.S3.Model;


namespace $safeprojectname$.SWF
{
    /// <summary>
    /// This class is used start workflow executions
    /// </summary>
    class StartWorkflowExecutionProcessor
    {
        IAmazonSimpleWorkflow _swfClient = new AmazonSimpleWorkflowClient();
        IAmazonS3 _s3Client = new AmazonS3Client();

        VirtualConsole _console;

        public StartWorkflowExecutionProcessor(VirtualConsole console)
        {
            this._console = console;
        }

        /// <summary>
        /// This method starts the workflow execution.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="filepath"></param>
        public void StartWorkflowExecution(string bucket, string filepath)
        {
            Task.Run(() =>
            {
                try
                {
                    IAmazonS3 s3Client = new AmazonS3Client();
                    IAmazonSimpleWorkflow swfClient = new AmazonSimpleWorkflowClient();

                    this._console.WriteLine("Create bucket if it doesn't exist");
                    // Make sure bucket exists
                    s3Client.PutBucket(new PutBucketRequest
                    {
                        BucketName = bucket,
                        UseClientRegion = true
                    });

                    this._console.WriteLine("Uploading image to S3");
                    // Upload the image to S3 before starting the execution
                    PutObjectRequest putRequest = new PutObjectRequest
                    {
                        BucketName = bucket,
                        FilePath = filepath,
                        Key = Path.GetFileName(filepath)
                    };

                    // Add upload progress callback to print every increment of 10 percent uploaded to the console.
                    int currentPercent = -1;
                    putRequest.StreamTransferProgress = new EventHandler<Amazon.Runtime.StreamTransferProgressArgs>((x, args) =>
                    {
                        if (args.PercentDone == currentPercent)
                            return;

                        currentPercent = args.PercentDone;
                        if (currentPercent % 10 == 0)
                        {
                            this._console.WriteLine(string.Format("... Uploaded {0} %", currentPercent));
                        }
                    });

                    s3Client.PutObject(putRequest);

                    // Setup the input for the workflow execution that tells the execution what bukcet and object to use.
                    WorkFlowExecutionInput input = new WorkFlowExecutionInput
                    {
                        Bucket = putRequest.BucketName,
                        SourceImageKey = putRequest.Key
                    };

                    this._console.WriteLine("Start workflow execution");
                    // Start the workflow execution
                    swfClient.StartWorkflowExecution(new StartWorkflowExecutionRequest()
                    {
                        //Serialize input to a string
                        Input = Utils.SerializeToJSON<WorkFlowExecutionInput>(input),
                        //Unique identifier for the execution
                        WorkflowId = DateTime.Now.Ticks.ToString(),
                        Domain = Constants.ImageProcessingDomain,
                        WorkflowType = new WorkflowType()
                        {
                            Name = Constants.ImageProcessingWorkflow,
                            Version = Constants.ImageProcessingWorkflowVersion
                        }
                    });
                }
                catch (Exception e)
                {
                    this._console.WriteLine("Error starting workflow execution: " + e.Message);
                }

            });
        }
    }
}
