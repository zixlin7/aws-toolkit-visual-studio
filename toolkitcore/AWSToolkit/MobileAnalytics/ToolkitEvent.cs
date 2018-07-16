using Amazon.Runtime.Internal.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.MobileAnalytics;
using Amazon.MobileAnalytics.Model;


namespace Amazon.AWSToolkit.MobileAnalytics
{
    public enum AttributeKeys
    {
        OpenViewFullIdentifier,
        ServiceName,
        ViewName,
        ReadableTimestamp,
        NavigationReason,
        DeploymentNetCoreTargetFramework,
        DeploymentSuccessType,
        DeploymentErrorType,

        CloudFormationNewProject,
        LambdaNodeJsNewProject,
        LambdaNETCoreNewProject,
        LambdaNETCoreNewProjectType,
        LambdaNETCoreNewProjectLanguage,

        LambdaFunctionTargetFramework,
        LambdaFunctionMemorySize,

        LambdaFunctionDeploymentSuccess,
        LambdaFunctionDeploymentError,
        LambdaFunctionDeploymentErrorDetail,

        LambdaFunctionIAMRoleCleanup,

        LambdaFunctionUsesAspNetCore,
        LambdaFunctionUsesRazorPages,
        LambdaFunctionUsesXRay,
        LambdaDeployedFunctionLanguage,

        LambdaTestInvoke,
        LambdaEventSourceSetupSuccess,
        LambdaEventSourceSetupError,

        VisualStudioIdentifier,

        CodeCommitCloneStatus,
        CodeCommitCreateStatus,
        CodeCommitConnectStatus,
        CodeCommitSetupCredentials,

        FirstExperienceDisplayStatus,
        FirstExperienceSaveCredentialsStatus,
        FirstExperienceImport,
        FirstExperienceLinkClick,

        ECSPublishContainerWizardStart,
        ECSPublishContainerWizardFinish,
        ECSPublishContainerDockerBase,
        ECSDeployService,
        ECSDeployScheduleTask,
        ECSDeployTask,
        ECSPushImage,
        ECSLaunchType,

        ECSConfiguredELB,
        ECSDeleteService,

        XRayEnabled,
        BeanstalkEnhancedHealth
    };

    public enum MetricKeys
    {
        NumberOfServicesUsed,
        TimeSpentInView,
        PageIndex,
        GroupIndex,
        DeploymentBundleSize,
        LambdaDeploymentBundleSize,
        FunctionInvokeTime
    };

    public class ToolkitEvent
    {
        public const string COMMON_STATUS_SUCCESS = "Success";
        public const string COMMON_STATUS_FAILURE = "Failure";

        private const string SERVICE_NAME_IDENTIFIER = "Amazon.AWSToolkit.";
        private const string VIEW_NAME_IDENTIFIER = ".View.";

        private Dictionary<string, string> _attributes;
        private Dictionary<string, double> _metrics;
        private DateTime _eventTimestamp;
        private string _customEventType = AMAConstants.EventTypes.VisualStudioToolkitEvent;
        private Session _customStartSessionDetails;

        /// <summary>
        /// Constructs a new Toolkit Event that can collect properties
        /// which will be parsed into attributes and metrics.
        /// The toolkit event can then be parsed into a Mobile Analytics Event.
        /// 
        /// NOTE: If you pass in customStartSessionDetails AND mark the customEventType as
        /// AMAConstants.SessionEventNames.START_SESSION, this constructor will align the timestamps to be the same.
        /// </summary>
        public ToolkitEvent(string customEventType = AMAConstants.EventTypes.VisualStudioToolkitEvent, Session customStartSessionDetails = null)
        {
            _eventTimestamp = DateTime.UtcNow;
            _attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _metrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            /*
             * If given custom traits, save them for Mobile Analytics Event export
             */
            _customEventType = customEventType;

            if (customStartSessionDetails != null)
            {
                _customStartSessionDetails = customStartSessionDetails;
                
                //if given customStartSessionDetails and the user marks the custom event as a START_SESSION event
                //make sure the right timestamps match.
                if (customEventType.Equals(AMAConstants.SessionEventNames.START_SESSION))
                    _eventTimestamp = _customStartSessionDetails.StartTimestamp;

                //if given customStartSessionDetails and the user marks the custom event as a STOP_SESSION event
                //make sure the right timestamps match.
                else if (customEventType.Equals(AMAConstants.SessionEventNames.STOP_SESSION))
                    _eventTimestamp = _customStartSessionDetails.StopTimestamp;
            }

            this.AddProperty(AttributeKeys.ReadableTimestamp, _eventTimestamp.ToString());
        }

        /// <summary>
        /// Converts a toolkit event into an AMA Event
        /// </summary>
        /// <returns>AMA Event with properties of a toolkit event</returns>
        public Event ConvertToMobileAnalyticsEvent()
        {
            Event customEvent = new Event();
            customEvent.EventType = _customEventType;
            customEvent.Version = AMAConstants.MOBILE_ANALYTICS_EVENT_VERSION_NUMBER;
            customEvent.Timestamp = _eventTimestamp;

            //If a custom session was specified, use it, otherwise leave it null
            //all null sessions will be caught by SimpleMobileAnalytics and auto-populated
            if (_customStartSessionDetails != null)
            {
                customEvent.Session = new Session();
                customEvent.Session.Id = _customStartSessionDetails.Id;
                customEvent.Session.StartTimestamp = _customStartSessionDetails.StartTimestamp;
            }

            //Unload all of the attributes into the event
            foreach (KeyValuePair<string, string> attribute in _attributes)
            {
                customEvent.Attributes.Add(attribute.Key, attribute.Value);
            }

            //Unload all of the metrics into the event
            foreach (KeyValuePair<string, double> metric in _metrics)
            {
                customEvent.Metrics.Add(metric.Key, metric.Value);
            }

            return customEvent;
        }

        /// <summary>
        /// This method allows you to add a property to your Mobile Analytics Event.
        /// Valid event types are defined in the Attributes Enum.
        /// Every event is composed of one or more properties which are all converted into KeyValue pairs and attached to an outgoing event call.
        /// Example: attributeKey: Attributes.ServiceName, attributeValue: "EC2"
        /// </summary>
        /// <param name="attributeKey">Valid Attribute Keys. Choose one from the Attribues.* Enum</param>
        /// <param name="attributeValue">The string you'd like associated with the Attribute key.</param>
        public void AddProperty(AttributeKeys attributeKey, string attributeValue)
        {
            if (!string.IsNullOrWhiteSpace(attributeValue))
            {
                if (attributeValue.Contains(SERVICE_NAME_IDENTIFIER) && attributeValue.Contains(VIEW_NAME_IDENTIFIER))
                {
                    ParseServiceAndView(attributeValue);
                }

                //regardless of if the full name is parsed successfully, record the passed in value as long as it isn't null or empty
                _attributes.Add(attributeKey.ToString(), attributeValue);
            }
        }

        /// <summary>
        /// This method allows you to add a property to your Mobile Analytics Event.
        /// Valid event types are defined in the Metrics Enum.
        /// Every event is composed of one or more properties which are all converted into KeyValue pairs and attached to an outgoing event call.
        /// Example: metricKey: Metrics.TimeSpentInView, metricValue: "635" (seconds)
        /// </summary>
        /// <param name="metricKey">Valid Metric Keys. Choose one from the Metrics.* Enum</param>
        /// <param name="metricValue">The double value you'd like associated with the Metric key.</param>
        public void AddProperty(MetricKeys metricKey, double metricValue)
        {
            _metrics.Add(metricKey.ToString(), metricValue);
        }

        /// <summary>
        /// Internal method used to parse out specific information from a view identifier.
        /// Note: This method operates on the premise that a specific string is used to identify a view.
        /// Example string: "Amazon.AWSToolkit.SQS.View.CreateQueueControl"
        /// </summary>
        /// <param name="attributeString"></param>
        private void ParseServiceAndView(string attributeString)
        {
            //Grab the service name
            int startIndex = attributeString.LastIndexOf(SERVICE_NAME_IDENTIFIER) + SERVICE_NAME_IDENTIFIER.Length;
            int indexOfNextPeriod = attributeString.IndexOf(".", startIndex);
            string serviceName = attributeString.Substring(startIndex, indexOfNextPeriod - startIndex);
            AddProperty(AttributeKeys.ServiceName, serviceName);

            //check to see if the view name exists, then grab it if it does
            startIndex = attributeString.LastIndexOf(VIEW_NAME_IDENTIFIER) + VIEW_NAME_IDENTIFIER.Length;
            indexOfNextPeriod = attributeString.IndexOf(".", startIndex);
            //check to see if there's a '.' after the view name. If not, grab the rest of the string.
            if (indexOfNextPeriod == -1)
                indexOfNextPeriod = attributeString.Length;
            string viewName = attributeString.Substring(startIndex, indexOfNextPeriod - startIndex);
            AddProperty(AttributeKeys.ViewName, viewName);
        }

    }
}
