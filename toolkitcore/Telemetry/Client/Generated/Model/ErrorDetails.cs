/*
 * Copyright 2010-2014 Amazon.com, Inc. or its affiliates. All Rights Reserved.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License").
 * You may not use this file except in compliance with the License.
 * A copy of the License is located at
 * 
 *  http://aws.amazon.com/apache2.0
 * 
 * or in the "license" file accompanying this file. This file is distributed
 * on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either
 * express or implied. See the License for the specific language governing
 * permissions and limitations under the License.
 */

/*
 * Do not modify this file. This file is generated from the telemetry-2017-07-25.normal.json service model.
 */
using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;
using System.IO;

using Amazon.Runtime;
using Amazon.Runtime.Internal;

namespace Amazon.ToolkitTelemetry.Model
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ErrorDetails
    {
        private string _command;
        private long? _epochTimestamp;
        private string _message;
        private string _stackTrace;
        private string _type;

        /// <summary>
        /// Gets and sets the property Command.
        /// </summary>
        [AWSProperty(Required=true, Max=2000)]
        public string Command
        {
            get { return this._command; }
            set { this._command = value; }
        }

        // Check to see if Command property is set
        internal bool IsSetCommand()
        {
            return this._command != null;
        }

        /// <summary>
        /// Gets and sets the property EpochTimestamp.
        /// </summary>
        [AWSProperty(Required=true, Min=0)]
        public long EpochTimestamp
        {
            get { return this._epochTimestamp.GetValueOrDefault(); }
            set { this._epochTimestamp = value; }
        }

        // Check to see if EpochTimestamp property is set
        internal bool IsSetEpochTimestamp()
        {
            return this._epochTimestamp.HasValue; 
        }

        /// <summary>
        /// Gets and sets the property Message.
        /// </summary>
        [AWSProperty(Max=2048)]
        public string Message
        {
            get { return this._message; }
            set { this._message = value; }
        }

        // Check to see if Message property is set
        internal bool IsSetMessage()
        {
            return this._message != null;
        }

        /// <summary>
        /// Gets and sets the property StackTrace.
        /// </summary>
        [AWSProperty(Required=true, Max=16384)]
        public string StackTrace
        {
            get { return this._stackTrace; }
            set { this._stackTrace = value; }
        }

        // Check to see if StackTrace property is set
        internal bool IsSetStackTrace()
        {
            return this._stackTrace != null;
        }

        /// <summary>
        /// Gets and sets the property Type.
        /// </summary>
        [AWSProperty(Required=true, Max=1024)]
        public string Type
        {
            get { return this._type; }
            set { this._type = value; }
        }

        // Check to see if Type property is set
        internal bool IsSetType()
        {
            return this._type != null;
        }

    }
}