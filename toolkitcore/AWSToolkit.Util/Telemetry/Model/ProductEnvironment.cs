﻿using System;
using Amazon.ToolkitTelemetry;

namespace Amazon.AWSToolkit.Telemetry.Model
{
    /// <summary>
    /// This is a proxy for some of the generated telemetry client request fields.
    /// It represents the environment the toolkit is running in.
    /// </summary>
    public class ProductEnvironment
    {
        public static readonly ProductEnvironment Default = new ProductEnvironment()
        {
            AwsProduct = AWSProduct.AWSToolkitForVisualStudio.Value,
            AwsProductVersion = "unknown",
            OperatingSystem = Environment.OSVersion.Platform.ToString(),
            OperatingSystemVersion = Environment.OSVersion.Version.ToString(), // eg: "10.0.18363.0"
            ParentProduct = "Visual Studio",
            ParentProductVersion = "unknown"
        };

        /// <summary>
        /// Identifies the product making telemetry calls (eg: VS Toolkit)
        /// </summary>
        public string AwsProduct { get; set; }
        public string AwsProductVersion { get; set; }

        public string OperatingSystem { get; set; }
        public string OperatingSystemVersion { get; set; }

        /// <summary>
        /// Identifies the IDE
        /// </summary>
        public string ParentProduct { get; set; }
        public string ParentProductVersion { get; set; }
    }
}