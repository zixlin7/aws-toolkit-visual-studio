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
    public partial class MetricDatum
    {
        private long? _epochTimestamp;
        private List<MetadataEntry> _metadata = new List<MetadataEntry>();
        private string _metricName;
        private Unit _unit;
        private double? _value;

        /// <summary>
        /// Gets and sets the property EpochTimestamp.
        /// </summary>
        [AWSProperty(Min=0)]
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
        /// Gets and sets the property MetricName.
        /// </summary>
        public string MetricName
        {
            get { return this._metricName; }
            set { this._metricName = value; }
        }

        // Check to see if MetricName property is set
        internal bool IsSetMetricName()
        {
            return this._metricName != null;
        }

        /// <summary>
        /// Gets and sets the property Unit.
        /// </summary>
        public Unit Unit
        {
            get { return this._unit; }
            set { this._unit = value; }
        }

        // Check to see if Unit property is set
        internal bool IsSetUnit()
        {
            return this._unit != null;
        }

        /// <summary>
        /// Gets and sets the property Value.
        /// </summary>
        [AWSProperty(Min=0)]
        public double Value
        {
            get { return this._value.GetValueOrDefault(); }
            set { this._value = value; }
        }

        // Check to see if Value property is set
        internal bool IsSetValue()
        {
            return this._value.HasValue; 
        }

    }
}