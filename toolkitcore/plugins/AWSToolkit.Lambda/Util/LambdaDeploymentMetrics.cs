using Amazon.AWSToolkit.MobileAnalytics;
using System.Linq;

namespace Amazon.AWSToolkit.Lambda.Util
{
    /// <summary>
    /// Pushes out Success/Failure Metrics related to Publishing a Lambda Function in a consistent manner.
    /// 
    /// When a Publish succeeds or fails, we can determine from the metric how it was published, and runtime or framework was targeted.
    /// Examples: Publishing a Serverless application: Serverless - netcoreapp2.1, or Publishing a Lambda Function: NetCore - dotnetcore2.0
    /// 
    /// When a Publish fails, we can determine the classification of error that occurred.
    /// All error details are in a parseable format [publish method]:[runtime or framework]:[error classification]:[additional error info].
    /// </summary>
    public class LambdaDeploymentMetrics
    {
        public class LambdaDeploymentProperties
        {
            public string TargetFramework { get; set; }
            public string MemorySize { get; set; }
            public long? BundleSize { get; set; }
            public bool XRayEnabled { get; set; } = false;
        }

        public enum LambdaPublishMethod
        {
            // Publishing a Lambda Function
            NetCore,

            // Publishing a Serverless Application
            Serverless,

            // Directly creating a Lambda Function using the AWS Explorer
            Generic,
        }

        private readonly LambdaPublishMethod _lambdaPublishMethod;
        private readonly string _platform;

        private string MetricsTitle => $"{_lambdaPublishMethod.ToString()}:{_platform}";

        /// <param name="publishMethod">The way the Lambda function is being published by VS</param>
        /// <param name="platform">For Serverless, this is the framework (eg: netcoreapp2.1). For NetCore and Generic, this is the runtime (eg: dotnetcore2.0).</param>
        public LambdaDeploymentMetrics(LambdaPublishMethod publishMethod, string platform)
        {
            _lambdaPublishMethod = publishMethod;
            _platform = platform ?? "";
        }

        public void QueueDeploymentSuccess()
        {
            QueueDeploymentSuccess(new LambdaDeploymentProperties());
        }

        public void QueueDeploymentSuccess(LambdaDeploymentProperties lambdaDeploymentProperties)
        {
            ToolkitEvent evnt = new ToolkitEvent();
            evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentSuccess, MetricsTitle);

            if (!string.IsNullOrWhiteSpace(lambdaDeploymentProperties.TargetFramework))
            {
                evnt.AddProperty(AttributeKeys.LambdaFunctionTargetFramework,
                    lambdaDeploymentProperties.TargetFramework);
            }

            if (!string.IsNullOrWhiteSpace(lambdaDeploymentProperties.MemorySize))
            {
                evnt.AddProperty(AttributeKeys.LambdaFunctionMemorySize, lambdaDeploymentProperties.MemorySize);
            }

            if (lambdaDeploymentProperties.BundleSize.HasValue)
            {
                evnt.AddProperty(MetricKeys.LambdaDeploymentBundleSize, lambdaDeploymentProperties.BundleSize.Value);
            }

            if (lambdaDeploymentProperties.XRayEnabled)
            {
                evnt.AddProperty(AttributeKeys.XRayEnabled, "Lambda");
            }

            SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
        }

        public void QueueDeploymentFailure(string errorCode, string serviceCode)
        {
            // Example: Generic:dotnetcore2.1:LambdaCreateFunction:AccessDeniedException-Forbidden
            string errorDetail = string.Join(":", new string[]
            {
                MetricsTitle,
                errorCode ?? "",
                serviceCode ?? "",
            });

            ToolkitEvent evnt = new ToolkitEvent();
            evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentError, MetricsTitle);
            evnt.AddProperty(AttributeKeys.LambdaFunctionDeploymentErrorDetail, errorDetail);
            SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
        }

        public void QueueDeploymentFailure(string errorCode, string serviceErrorCode, string serviceStatusCode)
        {
            var serviceCode = string.Join("-", new string[]
            {
                serviceErrorCode,
                serviceStatusCode,
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            QueueDeploymentFailure(errorCode, serviceCode);
        }
    }
}
