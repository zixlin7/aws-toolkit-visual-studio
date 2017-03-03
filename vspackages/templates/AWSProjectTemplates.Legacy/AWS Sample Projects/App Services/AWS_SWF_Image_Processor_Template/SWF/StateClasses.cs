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
    /// <summary>
    /// This class to store the inputs used when starting a workflow execution.
    /// It will be serialized into JSON and set as the input for the start.
    /// </summary>
    public class WorkFlowExecutionInput
    {
        public string Bucket
        {
            get;
            set;
        }

        public string SourceImageKey
        {
            get;
            set;
        }
    }

    /// <summary>
    /// This class is used to store the state passed into and out of the activity task to resize an image.
    /// </summary>
    public class ActivityState
    {
        public WorkFlowExecutionInput StartingInput
        {
            get;
            set;
        }

        public int ImageSize
        {
            get;
            set;
        }

        public string ResizedImageKey
        {
            get;
            set;
        }
    }
}
