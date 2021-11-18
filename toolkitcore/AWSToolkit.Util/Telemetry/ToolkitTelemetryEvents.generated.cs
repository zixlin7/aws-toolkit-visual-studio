//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using Amazon.AwsToolkit.Telemetry.Events.Core;
using System;
using System.Collections.Generic;

/// --------------------------------------------------------------------------------
/// This file is generated from https://github.com/aws/aws-toolkit-common/tree/master/telemetry
/// --------------------------------------------------------------------------------

namespace Amazon.AwsToolkit.Telemetry.Events.Generated
{
    
    
    /// Contains methods to record telemetry events
    public static partial class ToolkitTelemetryEvent
    {
        
        /// Records Telemetry Event:
        /// Called when deploying a Serverless Application Project
        public static void RecordServerlessapplicationDeploy(this ITelemetryLogger telemetryLogger, ServerlessapplicationDeploy payload)
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
                datum.MetricName = "serverlessapplication_deploy";
                datum.Unit = Unit.None;
                datum.Passive = payload.Passive;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }
                datum.AddMetadata("awsAccount", payload.AwsAccount);
                datum.AddMetadata("awsRegion", payload.AwsRegion);

                datum.AddMetadata("result", payload.Result);

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
        /// Called when user selects a profile
        public static void RecordCodeartifactGetRepoUrl(this ITelemetryLogger telemetryLogger, CodeartifactGetRepoUrl payload)
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
                datum.MetricName = "codeartifact_getRepoUrl";
                datum.Unit = Unit.None;
                datum.Passive = payload.Passive;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }
                datum.AddMetadata("awsAccount", payload.AwsAccount);
                datum.AddMetadata("awsRegion", payload.AwsRegion);

                datum.AddMetadata("result", payload.Result);

                datum.AddMetadata("codeartifactPackageType", payload.CodeartifactPackageType);

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
        /// Called when user get an endpoint url
        public static void RecordCodeartifactSetRepoCredentialProfile(this ITelemetryLogger telemetryLogger, CodeartifactSetRepoCredentialProfile payload)
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
                datum.MetricName = "codeartifact_setRepoCredentialProfile";
                datum.Unit = Unit.None;
                datum.Passive = payload.Passive;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }
                datum.AddMetadata("awsAccount", payload.AwsAccount);
                datum.AddMetadata("awsRegion", payload.AwsRegion);

                datum.AddMetadata("result", payload.Result);

                datum.AddMetadata("codeartifactPackageType", payload.CodeartifactPackageType);

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
        /// Called when user starts the Publish to AWS workflow
        public static void RecordPublishStart(this ITelemetryLogger telemetryLogger, PublishStart payload)
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
                datum.MetricName = "publish_start";
                datum.Unit = Unit.None;
                datum.Passive = payload.Passive;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }
                datum.AddMetadata("awsAccount", payload.AwsAccount);
                datum.AddMetadata("awsRegion", payload.AwsRegion);

                datum.AddMetadata("result", payload.Result);

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
        /// Called when user publishes the project
        public static void RecordPublishDeploy(this ITelemetryLogger telemetryLogger, PublishDeploy payload)
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
                datum.MetricName = "publish_deploy";
                datum.Unit = Unit.None;
                datum.Passive = payload.Passive;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }
                datum.AddMetadata("awsAccount", payload.AwsAccount);
                datum.AddMetadata("awsRegion", payload.AwsRegion);

                datum.AddMetadata("result", payload.Result);

                datum.AddMetadata("framework", payload.Framework);

                datum.AddMetadata("duration", payload.Duration);

                datum.AddMetadata("applicationType", payload.ApplicationType);

                datum.AddMetadata("initialPublish", payload.InitialPublish);

                datum.AddMetadata("defaultConfiguration", payload.DefaultConfiguration);

                if (payload.RecommendedTarget.HasValue)
                {
                    datum.AddMetadata("recommendedTarget", payload.RecommendedTarget.Value);
                }

                datum.AddMetadata("recipeId", payload.RecipeId);

                datum.AddMetadata("errorCode", payload.ErrorCode);

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
        /// Called when user opts into Publish to AWS experience
        public static void RecordPublishOptIn(this ITelemetryLogger telemetryLogger, PublishOptIn payload)
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
                datum.MetricName = "publish_optIn";
                datum.Unit = Unit.None;
                datum.Passive = payload.Passive;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }
                datum.AddMetadata("awsAccount", payload.AwsAccount);
                datum.AddMetadata("awsRegion", payload.AwsRegion);

                datum.AddMetadata("result", payload.Result);

                datum.AddMetadata("serviceType", payload.ServiceType);

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
        /// Called when user opts out of Publish to AWS experience
        public static void RecordPublishOptOut(this ITelemetryLogger telemetryLogger, PublishOptOut payload)
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
                datum.MetricName = "publish_optOut";
                datum.Unit = Unit.None;
                datum.Passive = payload.Passive;
                if (payload.Value.HasValue)
                {
                    datum.Value = payload.Value.Value;
                }
                else
                {
                    datum.Value = 1;
                }
                datum.AddMetadata("awsAccount", payload.AwsAccount);
                datum.AddMetadata("awsRegion", payload.AwsRegion);

                datum.AddMetadata("result", payload.Result);

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
    
    /// Called when deploying a Serverless Application Project
    public sealed class ServerlessapplicationDeploy : BaseTelemetryEvent
    {
        
        /// The result of the operation
        public Result Result;
        
        public ServerlessapplicationDeploy()
        {
            this.Passive = false;
        }
    }
    
    /// Called when user selects a profile
    public sealed class CodeartifactGetRepoUrl : BaseTelemetryEvent
    {
        
        /// The result of the operation
        public Result Result;
        
        /// The CodeArtifact package type
        public string CodeartifactPackageType;
        
        public CodeartifactGetRepoUrl()
        {
            this.Passive = false;
        }
    }
    
    /// Called when user get an endpoint url
    public sealed class CodeartifactSetRepoCredentialProfile : BaseTelemetryEvent
    {
        
        /// The result of the operation
        public Result Result;
        
        /// The CodeArtifact package type
        public string CodeartifactPackageType;
        
        public CodeartifactSetRepoCredentialProfile()
        {
            this.Passive = false;
        }
    }
    
    /// Called when user starts the Publish to AWS workflow
    public sealed class PublishStart : BaseTelemetryEvent
    {
        
        /// The result of the operation
        public Result Result;
        
        public PublishStart()
        {
            this.Passive = false;
        }
    }
    
    /// Called when user publishes the project
    public sealed class PublishDeploy : BaseTelemetryEvent
    {
        
        /// The result of the operation
        public Result Result;
        
        /// Optional - Application framework being used
        public string Framework;
        
        /// The duration of the operation in milliseconds
        public double Duration;
        
        /// Optional - The type of application being published
        public string ApplicationType;
        
        /// Whether this was the initial publish or a republish
        public bool InitialPublish;
        
        /// Whether or not the default configuration values were used. False if at least one value was adjusted
        public bool DefaultConfiguration;
        
        /// Optional - Whether or not the recommended deployment target was used (initial publish only)
        public System.Boolean? RecommendedTarget;
        
        /// The recipe used for the deployment
        public string RecipeId;
        
        /// Optional - The error code associated with an operation
        public string ErrorCode;
        
        public PublishDeploy()
        {
            this.Passive = false;
        }
    }
    
    /// Called when user opts into Publish to AWS experience
    public sealed class PublishOptIn : BaseTelemetryEvent
    {
        
        /// The result of the operation
        public Result Result;
        
        /// The name of the AWS service acted on. These values come from the AWS SDK. To find them in the JAVA SDK search for SERVICE_NAME in each service client, or look for serviceId in metadata in the service2.json
        public string ServiceType;
        
        public PublishOptIn()
        {
            this.Passive = false;
        }
    }
    
    /// Called when user opts out of Publish to AWS experience
    public sealed class PublishOptOut : BaseTelemetryEvent
    {
        
        /// The result of the operation
        public Result Result;
        
        public PublishOptOut()
        {
            this.Passive = false;
        }
    }
}
