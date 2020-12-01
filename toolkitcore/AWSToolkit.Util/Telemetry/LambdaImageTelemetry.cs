using System;
using System.Collections.Generic;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.Telemetry
{

    /// Contains methods to record telemetry events
    public static partial class LambdaToolkitTelemetryEvent
    {
        /// Records Telemetry Event:
        /// Called when deploying a Lambda Function
        public static void RecordLambdaDeploy(this ITelemetryLogger telemetryLogger, LambdaDeploy payload)
        {
            try
            {
                var metrics = new Metrics();
                if (payload.CreatedOn.HasValue)
                {
                    metrics.CreatedOn = payload.CreatedOn.Value;
                }
                else
                {
                    metrics.CreatedOn = System.DateTime.Now;
                }
                metrics.Data = new List<MetricDatum>();

                var datum = new MetricDatum();
                datum.MetricName = "lambda_deploy";
                datum.Unit = Unit.None;
                datum.Passive = false;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }

                datum.AddMetadata("lambdaPackageType", payload.LambdaPackageType);

                datum.AddMetadata("result", payload.Result);

                datum.AddMetadata("regionId", payload.RegionId);

                datum.AddMetadata("initialDeploy", payload.InitialDeploy);

                if (payload.Runtime.HasValue)
                {
                    datum.AddMetadata("runtime", payload.Runtime.Value);
                }

                datum.AddMetadata("platform", payload.Platform);

                metrics.Data.Add(datum);
                telemetryLogger.Record(metrics);
            }
            catch (System.Exception e)
            {
                telemetryLogger.Logger.Error("Error recording telemetry event", e);
                System.Diagnostics.Debug.Assert(false, "Error Recording Telemetry");
            }
        }

        /// Records Telemetry Event:
        /// Called when invoking lambdas locally (with SAM in most toolkits)
        public static void RecordLambdaInvokeLocal(this ITelemetryLogger telemetryLogger, LambdaInvokeLocal payload)
        {
            try
            {
                var metrics = new Metrics();
                if (payload.CreatedOn.HasValue)
                {
                    metrics.CreatedOn = payload.CreatedOn.Value;
                }
                else
                {
                    metrics.CreatedOn = System.DateTime.Now;
                }
                metrics.Data = new List<MetricDatum>();

                var datum = new MetricDatum();
                datum.MetricName = "lambda_invokeLocal";
                datum.Unit = Unit.None;
                datum.Passive = false;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }

                if (payload.Runtime.HasValue)
                {
                    datum.AddMetadata("runtime", payload.Runtime.Value);
                }

                datum.AddMetadata("lambdaPackageType", payload.LambdaPackageType);

                datum.AddMetadata("result", payload.Result);

                datum.AddMetadata("debug", payload.Debug);

                metrics.Data.Add(datum);
                telemetryLogger.Record(metrics);
            }
            catch (System.Exception e)
            {
                telemetryLogger.Logger.Error("Error recording telemetry event", e);
                System.Diagnostics.Debug.Assert(false, "Error Recording Telemetry");
            }
        }
    }

    /// Metric field type
    /// The Lambda Package type of the function
    public struct LambdaPackageType
    {

        private string _value;

        /// Zip
        public static readonly LambdaPackageType Zip = new LambdaPackageType("Zip");

        /// Image
        public static readonly LambdaPackageType Image = new LambdaPackageType("Image");

        public LambdaPackageType(string value)
        {
            this._value = value;
        }

        public override string ToString()
        {
            return this._value;
        }
    }

    /// Metric field type
    /// The lambda runtime
    public struct Runtime
    {

        private string _value;

        /// dotnetcore3.1
        public static readonly Runtime Dotnetcore31 = new Runtime("dotnetcore3.1");

        /// dotnetcore2.1
        public static readonly Runtime Dotnetcore21 = new Runtime("dotnetcore2.1");

        /// dotnet5.0
        public static readonly Runtime Dotnet50 = new Runtime("dotnet5.0");

        /// nodejs12.x
        public static readonly Runtime Nodejs12x = new Runtime("nodejs12.x");

        /// nodejs10.x
        public static readonly Runtime Nodejs10x = new Runtime("nodejs10.x");

        /// nodejs8.10
        public static readonly Runtime Nodejs810 = new Runtime("nodejs8.10");

        /// ruby2.5
        public static readonly Runtime Ruby25 = new Runtime("ruby2.5");

        /// java8
        public static readonly Runtime Java8 = new Runtime("java8");

        /// java8.al2
        public static readonly Runtime Java8al2 = new Runtime("java8.al2");

        /// java11
        public static readonly Runtime Java11 = new Runtime("java11");

        /// go1.x
        public static readonly Runtime Go1x = new Runtime("go1.x");

        /// python3.8
        public static readonly Runtime Python38 = new Runtime("python3.8");

        /// python3.7
        public static readonly Runtime Python37 = new Runtime("python3.7");

        /// python3.6
        public static readonly Runtime Python36 = new Runtime("python3.6");

        /// python2.7
        public static readonly Runtime Python27 = new Runtime("python2.7");

        public Runtime(string value)
        {
            this._value = value;
        }

        public override string ToString()
        {
            return this._value;
        }
    }

    /// Called when deploying a Lambda Function
    public sealed class LambdaDeploy : BaseTelemetryEvent
    {

        /// The Lambda Package type of the function
        public LambdaPackageType LambdaPackageType;

        /// The result of the operation
        public Result Result;

        /// The ID of the region that was selected
        public string RegionId;

        /// Whether or not the deploy targets a new destination (true) or an existing destination (false)
        public bool InitialDeploy;

        /// Optional - The lambda runtime
        public AwsToolkit.Telemetry.Events.Generated.Runtime? Runtime;

        /// Optional - Language-specific identification. Examples: v4.6.1, netcoreapp3.1, nodejs12.x. Not AWS Lambda specific. Allows for additional details when other fields are opaque, such as the Lambda runtime value 'provided'.
        public string Platform;
    }

    /// Called when invoking lambdas locally (with SAM in most toolkits)
    public sealed class LambdaInvokeLocal : BaseTelemetryEvent
    {

        /// Optional - The lambda runtime
        public Runtime? Runtime;

        /// The Lambda Package type of the function
        public LambdaPackageType LambdaPackageType;

        /// The result of the operation
        public Result Result;

        /// If the action was run in debug mode or not
        public bool Debug;
    }
}
