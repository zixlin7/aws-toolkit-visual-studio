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
    /// Container for the parameters to the PostErrorReport operation.
    /// 
    /// </summary>
    public partial class PostErrorReportRequest : AmazonToolkitTelemetryRequest
    {
        private AWSProduct _awsProduct;
        private string _awsProductVersion;
        private ErrorDetails _errorDetails;
        private List<MetadataEntry> _metadata = new List<MetadataEntry>();
        private Userdata _userdata;

        /// <summary>
        /// Gets and sets the property AWSProduct.
        /// </summary>
        [AWSProperty(Required=true)]
        public AWSProduct AWSProduct
        {
            get { return this._awsProduct; }
            set { this._awsProduct = value; }
        }

        // Check to see if AWSProduct property is set
        internal bool IsSetAWSProduct()
        {
            return this._awsProduct != null;
        }

        /// <summary>
        /// Gets and sets the property AWSProductVersion.
        /// </summary>
        [AWSProperty(Required=true)]
        public string AWSProductVersion
        {
            get { return this._awsProductVersion; }
            set { this._awsProductVersion = value; }
        }

        // Check to see if AWSProductVersion property is set
        internal bool IsSetAWSProductVersion()
        {
            return this._awsProductVersion != null;
        }

        /// <summary>
        /// Gets and sets the property ErrorDetails.
        /// </summary>
        [AWSProperty(Required=true)]
        public ErrorDetails ErrorDetails
        {
            get { return this._errorDetails; }
            set { this._errorDetails = value; }
        }

        // Check to see if ErrorDetails property is set
        internal bool IsSetErrorDetails()
        {
            return this._errorDetails != null;
        }

        /// <summary>
        /// Gets and sets the property Metadata.
        /// </summary>
        public List<MetadataEntry> Metadata
        {
            get { return this._metadata; }
            set { this._metadata = value; }
        }

        // Check to see if Metadata property is set
        internal bool IsSetMetadata()
        {
            return this._metadata != null && this._metadata.Count > 0; 
        }

        /// <summary>
        /// Gets and sets the property Userdata.
        /// </summary>
        public Userdata Userdata
        {
            get { return this._userdata; }
            set { this._userdata = value; }
        }

        // Check to see if Userdata property is set
        internal bool IsSetUserdata()
        {
            return this._userdata != null;
        }

    }
}