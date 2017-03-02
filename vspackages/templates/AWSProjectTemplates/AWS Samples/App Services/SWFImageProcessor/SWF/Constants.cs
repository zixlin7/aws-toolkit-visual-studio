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

namespace $safeprojectname$.SWF
{
    class Constants
    {
        public const string ImageProcessingDomain = "ImageProcessing";
        public const string ActivityIdPrefix = "ImageActivity";

        public const string ImageProcessingTaskList = "ImageProcessingTaskList";

        public const string ImageProcessingActivityName = "Image Processing";
        public const string ImageProcessingActivityVersion = "1.0";
        public const string ImageProcessingActivityTaskList = "ImageProcessingTaskList";
        public const string ExecutionStartToCloseTimeout = "300";
        public const string TaskStartToCloseTimeout = "30";

        public const string ImageProcessingWorkflow = "ImageProcessingWorkflow";
        public const string ImageProcessingWorkflowVersion = "1.0";

        public const string ScheduleToCloseTimeout = "30";
        public const string ScheduleToStartTimeout = "10";
        public const string StartToCloseTimeout = "60";
        public const string HeartbeatTimeout = "NONE";

    }
}
