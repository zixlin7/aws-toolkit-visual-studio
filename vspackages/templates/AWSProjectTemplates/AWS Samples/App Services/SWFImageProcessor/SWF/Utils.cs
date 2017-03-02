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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Web.Script.Serialization;

using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

namespace $safeprojectname$.SWF
{
    class Utils
    {
        public static T DeserializeFromJSON<T>(string json)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            T inputs = serializer.Deserialize<T>(json);
            return inputs;
        }

        public static string SerializeToJSON<T>(T inputs)
        {
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            StringBuilder builder = new StringBuilder();
            serializer.Serialize(inputs, builder);
            return builder.ToString();
        }

        /// <summary>
        /// This utility method setups the simple workflow entities that are used for this workflow.
        /// </summary>
        public static void Setup()
        {
            IAmazonSimpleWorkflow swfClient = new AmazonSimpleWorkflowClient();

            // See if Domain exists and if not register it. The Domain is used as a global container for all the other entities created for the workflow.
            var listDomainRequest = new ListDomainsRequest()
            {
                RegistrationStatus = RegistrationStatus.REGISTERED
            };
            if (swfClient.ListDomains(listDomainRequest).DomainInfos.Infos.FirstOrDefault(x => x.Name == Constants.ImageProcessingDomain) == null)
            {
                RegisterDomainRequest request = new RegisterDomainRequest()
                {
                    Name = Constants.ImageProcessingDomain,
                    Description = "Sample Domain for processing images",
                    WorkflowExecutionRetentionPeriodInDays = "3"
                };

                swfClient.RegisterDomain(request);
            }

            // See if activity exists and if not register it. The activity defines a task list that 
            // the workflow's activity workflow will poll on for tasks to complete.
            var listActivityRequest = new ListActivityTypesRequest()
            {
                Name = Constants.ImageProcessingActivityName,
                Domain = Constants.ImageProcessingDomain,
                RegistrationStatus = RegistrationStatus.REGISTERED
            };
            if (swfClient.ListActivityTypes(listActivityRequest).ActivityTypeInfos.TypeInfos.Count == 0)
            {
                RegisterActivityTypeRequest request = new RegisterActivityTypeRequest()
                {
                    Name = Constants.ImageProcessingActivityName,
                    Domain = Constants.ImageProcessingDomain,
                    Description = "Activity to process images",
                    Version = Constants.ImageProcessingActivityVersion,
                    DefaultTaskList = new TaskList() { Name = Constants.ImageProcessingActivityTaskList },
                    DefaultTaskScheduleToCloseTimeout = Constants.ScheduleToCloseTimeout,
                    DefaultTaskScheduleToStartTimeout = Constants.ScheduleToStartTimeout,
                    DefaultTaskStartToCloseTimeout = Constants.StartToCloseTimeout,
                    DefaultTaskHeartbeatTimeout = Constants.HeartbeatTimeout,

                };

                swfClient.RegisterActivityType(request);
            }

            // See if workflow type exists and if not register it. The workflow type is used when starting a workflow execution and 
            // will direct tasks to the appropriate task list.
            var listWorkflowRequest = new ListWorkflowTypesRequest()
            {
                Name = Constants.ImageProcessingWorkflow,
                Domain = Constants.ImageProcessingDomain,
                RegistrationStatus = RegistrationStatus.REGISTERED
            };
            if (swfClient.ListWorkflowTypes(listWorkflowRequest).WorkflowTypeInfos.TypeInfos.Count == 0)
            {
                RegisterWorkflowTypeRequest request = new RegisterWorkflowTypeRequest()
                {
                    DefaultChildPolicy = ChildPolicy.TERMINATE,
                    DefaultExecutionStartToCloseTimeout = Constants.ExecutionStartToCloseTimeout,
                    DefaultTaskList = new TaskList()
                    {
                        Name = Constants.ImageProcessingActivityTaskList
                    },
                    DefaultTaskStartToCloseTimeout = Constants.TaskStartToCloseTimeout,
                    Domain = Constants.ImageProcessingDomain,
                    Name = Constants.ImageProcessingWorkflow,
                    Version = Constants.ImageProcessingWorkflowVersion
                };

                swfClient.RegisterWorkflowType(request);
            }
        }
    }
}
