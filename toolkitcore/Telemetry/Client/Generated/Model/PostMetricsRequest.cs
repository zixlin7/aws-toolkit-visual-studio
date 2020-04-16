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
    /// Container for the parameters to the PostMetrics operation.
    /// 
    /// </summary>
    public partial class PostMetricsRequest : AmazonToolkitTelemetryRequest
    {
        private AWSProduct _awsProduct;
        private string _awsProductVersion;
        private string _clientID;
        private List<MetricDatum> _metricData = new List<MetricDatum>();
        private string _os;
        private string _osVersion;
        private string _parentProduct;
        private string _parentProductVersion;

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
        /// Gets and sets the property ClientID.
        /// </summary>
        [AWSProperty(Required=true)]
        public string ClientID
        {
            get { return this._clientID; }
            set { this._clientID = value; }
        }

        // Check to see if ClientID property is set
        internal bool IsSetClientID()
        {
            return this._clientID != null;
        }

        /// <summary>
        /// Gets and sets the property MetricData.
        /// </summary>
        [AWSProperty(Required=true, Min=1, Max=20)]
        public List<MetricDatum> MetricData
        {
            get { return this._metricData; }
            set { this._metricData = value; }
        }

        // Check to see if MetricData property is set
        internal bool IsSetMetricData()
        {
            return this._metricData != null && this._metricData.Count > 0; 
        }

        /// <summary>
        /// Gets and sets the property OS.
        /// </summary>
        [AWSProperty(Min=1, Max=200)]
        public string OS
        {
            get { return this._os; }
            set { this._os = value; }
        }

        // Check to see if OS property is set
        internal bool IsSetOS()
        {
            return this._os != null;
        }

        /// <summary>
        /// Gets and sets the property OSVersion.
        /// </summary>
        [AWSProperty(Min=1, Max=200)]
        public string OSVersion
        {
            get { return this._osVersion; }
            set { this._osVersion = value; }
        }

        // Check to see if OSVersion property is set
        internal bool IsSetOSVersion()
        {
            return this._osVersion != null;
        }

        /// <summary>
        /// Gets and sets the property ParentProduct.
        /// </summary>
        [AWSProperty(Min=1, Max=200)]
        public string ParentProduct
        {
            get { return this._parentProduct; }
            set { this._parentProduct = value; }
        }

        // Check to see if ParentProduct property is set
        internal bool IsSetParentProduct()
        {
            return this._parentProduct != null;
        }

        /// <summary>
        /// Gets and sets the property ParentProductVersion.
        /// </summary>
        [AWSProperty(Min=1, Max=200)]
        public string ParentProductVersion
        {
            get { return this._parentProductVersion; }
            set { this._parentProductVersion = value; }
        }

        // Check to see if ParentProductVersion property is set
        internal bool IsSetParentProductVersion()
        {
            return this._parentProductVersion != null;
        }

    }
}