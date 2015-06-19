using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.MobileAnalytics;
using Amazon.MobileAnalytics.Custom;
using Amazon.MobileAnalytics.Model;
using Amazon.CognitoIdentity;
using System.ComponentModel;
using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.MobileAnalytics
{
    public enum Attributes
    {
        OpenViewFullIdentifier,
        ServiceName,
        ViewName,
    };

    public enum Metrics
    {
        NumberOfServicesUsed,
        TimeSpentInView,
    };

    public class SimpleMobileAnalytics
    {
        private AmazonMobileAnalyticsClient amazonMobileAnalyticsClient;
        private AnalyticsCognitoAWSCredentials credentialsProvider;
        private ClientContext clientContext;

        private const string AWS_COGNITO_IDENTITY_POOL_ID = "us-east-1:af7e8a33-505c-4819-bfb4-3a034ac81664";
        private const string CLIENT_ID = "aws_dot_net_sdk_vs_tooling_analytics_tracking";
        private const string APP_TITLE = "aws_dot_net_sdk_tooling";
        private const string APP_ID = "18db203edefd489a82eb440f29ec2cd1";

        private const string START_SESSION = "_session.start";
        private const string STOP_SESSION = "_session.stop";
        private const string PAUSE_SESSION = "_session.pause";
        private const string RESUME_SESSION = "_session.resume";

        private const string SESSION_ID = "analytics_test_session";
        private Session _startSessionDetails;

        /*
         * Do NOT change MOBILE_ANALYTICS_EVENT_VERSION_NUMBER!!
         * See Version documentation here: http://docs.aws.amazon.com/mobileanalytics/latest/ug/PutEvents.html
         */
        private const string MOBILE_ANALYTICS_EVENT_VERSION_NUMBER = "v2.0";
        private const string GENERIC_EVENT_NAME = ".NET VS Toolkit Event";
        private const string SERVICE_NAME_IDENTIFIER = "Amazon.AWSToolkit.";
        private const string VIEW_NAME_IDENTIFIER = ".View.";

        private Dictionary<string, string> _attributes;
        private Dictionary<string, double> _metrics;

        public bool PermissionToCollectAnalytics
        {
            get
            {
                string analyticsPermission = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.AnalyticsPermission);
                return (analyticsPermission.Equals("true")) ? true : false;
            }
        }

        /*
         * Create a singleton instance of this class
         */
        private static volatile SimpleMobileAnalytics instance;
        private static object syncRoot = new Object();
        public static SimpleMobileAnalytics Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new SimpleMobileAnalytics();
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Private Constructor only to be instantiated by the singleton
        /// </summary>
        private SimpleMobileAnalytics()
        {
            /* Initialize the Amazon Cognito credentials provider.
             * Using the Analytics extended CognitoAWSCredentials will
             * ensure caching of the identityID for unauthticated session logging. 
             */
            credentialsProvider = new AnalyticsCognitoAWSCredentials(
                AWS_COGNITO_IDENTITY_POOL_ID,
                RegionEndpoint.USEast1
            );

            //construct the AWS Mobile Analytics Client
            amazonMobileAnalyticsClient = new AmazonMobileAnalyticsClient(credentialsProvider, RegionEndpoint.USEast1);

            //construct the AWS Mobile Analytics Client Context
            ClientContextConfig clientContextConfig = new ClientContextConfig(CLIENT_ID, APP_TITLE, APP_ID);
            clientContext = new ClientContext(clientContextConfig);

            //initialize attributes and metrics
            _attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _metrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            /*
             * Start the toolkit session - it will be closed when the VS Shell changes
             * see https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.ivspackage.close.aspx
             * for reference on how we're notified when the user closes VS
             */
            _startSessionDetails = StartSession();
        }

        /// <summary>
        /// This method allows you to add a property to your Mobile Analytics Event.
        /// Valid event types are defined in the Attributes Enum.
        /// Every event is composed of one or more properties which are all converted into KeyValue pairs and attached to an outgoing event call.
        /// Example: attributeKey: Attributes.ServiceName, attributeValue: "EC2"
        /// </summary>
        /// <param name="attributeKey">Valid Attribute Keys. Choose one from the Attribues.* Enum</param>
        /// <param name="attributeValue">The string you'd like associated with the Attribute key.</param>
        public void AddProperty(Attributes attributeKey, string attributeValue)
        {
            if (PermissionToCollectAnalytics)
            {
                if (_attributes == null)
                    _attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                if (!string.IsNullOrWhiteSpace(attributeValue))
                {
                    if (attributeValue.Contains(SERVICE_NAME_IDENTIFIER))
                    {
                        ParseServiceAndView(attributeValue);
                    }

                    //regardless of if the full name is parsed successfully, record the passed in value as long as it isn't null or empty
                    _attributes.Add(attributeKey.ToString(), attributeValue);
                }
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
        public void AddProperty(Metrics metricKey, double metricValue)
        {
            if (PermissionToCollectAnalytics)
            {
                if (_metrics == null)
                    _metrics = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

                if (metricValue != null)
                    _metrics.Add(metricKey.ToString(), metricValue);
            }
            
        }

        /// <summary>
        /// Internal method used to parse out specific information from a view identifier.
        /// Note: This method operates on the premise that a specific string is used to identify a view.
        /// Example string: "Amazon.AWSToolkit.SQS.View.CreateQueueControl"
        /// </summary>
        /// <param name="attributeString"></param>
        private void ParseServiceAndView(string attributeString) {

            //Grab the service name
            int startIndex = attributeString.LastIndexOf(SERVICE_NAME_IDENTIFIER) + SERVICE_NAME_IDENTIFIER.Length;
            int indexOfNextPeriod = attributeString.IndexOf(".", startIndex);
            string serviceName = attributeString.Substring(startIndex, indexOfNextPeriod - startIndex);
            AddProperty(Attributes.ServiceName, serviceName);

            //check to see if the view name exists, then grab it if it does
            if (attributeString.Contains(VIEW_NAME_IDENTIFIER))
            {
                startIndex = attributeString.LastIndexOf(VIEW_NAME_IDENTIFIER) + VIEW_NAME_IDENTIFIER.Length;
                indexOfNextPeriod = attributeString.IndexOf(".", startIndex);
                //check to see if there's a '.' after the view name. If not, grab the rest of the string.
                if (indexOfNextPeriod == -1)
                    indexOfNextPeriod = attributeString.Length;
                string viewName = attributeString.Substring(startIndex, indexOfNextPeriod - startIndex);
                AddProperty(Attributes.ViewName, viewName);
            }
        }

        /// <summary>
        /// Internal method used to start a mobile analytics session when the toolkit is initialized.
        /// It is intended to only occur one time for each "session" of the toolkit use.
        /// i.e. Whenever a user opens the toolkit after closing it or for the first time.
        /// </summary>
        /// <returns></returns>
        private Session StartSession()
        {
            if (PermissionToCollectAnalytics)
            {
                PutEventsRequest putEventRequest = new PutEventsRequest();
                putEventRequest.ClientContext = clientContext.ToJsonString();

                DateTime startTime = DateTime.Now;
                Event startSession = new Event
                {
                    Timestamp = startTime,
                    EventType = START_SESSION,
                    Session = new Session
                    {
                        StartTimestamp = startTime,
                        Id = SESSION_ID
                    }
                };

                putEventRequest.Events.Add(startSession);
                amazonMobileAnalyticsClient.PutEvents(putEventRequest);

                return startSession.Session;
            }

            return null;
        }


        /// <summary>
        /// This method is only intended to be called once the toolkit is closed.
        /// This is currently triggered in the IVsPackage.Close Method in AWSToolkitPackage.cs.
        /// Unless you have a specific reason to trigger the end the overall toolkit session, don't call it.
        /// </summary>
        public void StopSession()
        {
            if (PermissionToCollectAnalytics && _startSessionDetails != null)
            {
                PutEventsRequest putEventRequest = new PutEventsRequest();
                putEventRequest.ClientContext = clientContext.ToJsonString();

                DateTime endTime = DateTime.Now;
                Event stopSession = new Event
                {
                    Timestamp = endTime,
                    EventType = STOP_SESSION,
                    Session = new Session
                    {
                        StartTimestamp = _startSessionDetails.StartTimestamp,
                        StopTimestamp = endTime,
                        Id = SESSION_ID
                    }
                };

                putEventRequest.Events.Add(stopSession);
                amazonMobileAnalyticsClient.PutEvents(putEventRequest);
            }
        }

        /// <summary>
        /// This is an internal method used to populate an event with the properties that were added using the
        /// "AddProperty" method. It uses the session information defined in the StartSession() method.
        /// </summary>
        /// <param name="customEvent"></param>
        private void PopulateCustomEvent(Event customEvent)
        {
            if (PermissionToCollectAnalytics)
            {
                customEvent.EventType = GENERIC_EVENT_NAME;
                customEvent.Version = MOBILE_ANALYTICS_EVENT_VERSION_NUMBER;
                customEvent.Timestamp = DateTime.Now;

                if (_attributes != null)
                {
                    foreach (KeyValuePair<string, string> attribute in _attributes)
                    {
                        customEvent.Attributes.Add(attribute.Key, attribute.Value);
                    }
                    //after populating the event, create a new/empty dictonary for future events
                    _attributes = new Dictionary<string, string>();
                }

                if (_metrics != null)
                {
                    foreach (KeyValuePair<string, double> metric in _metrics)
                    {
                        customEvent.Metrics.Add(metric.Key, metric.Value);
                    }
                    //after populating the event, create a new/empty dictonary for future events
                    _metrics = new Dictionary<string, double>();
                }
            }
        }

        /// <summary>
        /// Asynchronously pushes out an event to Mobile Analytics with the currently stored attributes and metrics.
        /// </summary>
        public void RecordEventWithProperties()
        {
            if (PermissionToCollectAnalytics)
            {
                BackgroundWorker bw = new BackgroundWorker();

                // this allows our worker to report progress during work
                bw.WorkerReportsProgress = true;

                // what to do in the background thread
                bw.DoWork += new DoWorkEventHandler(
                    delegate(object o, DoWorkEventArgs args)
                    {
                        //create the putEventRequest and get the clientContext
                        PutEventsRequest putEventRequest = new PutEventsRequest();
                        putEventRequest.ClientContext = clientContext.ToJsonString();

                        /*
                         * This catches the case where a customer opted out of analytics before toolkit initialization,
                         * then turned analytics on and triggered events after the fact.
                         * 
                         * Although unlikely to occur, this would cause issues because populating an event requires information
                         * about the start session. Also the service requires a session is started before events can be aggregated.
                         */
                        if (_startSessionDetails == null)
                        {
                            _startSessionDetails = StartSession();
                        }

                        Event myEvent = new Event();
                        myEvent.Session = _startSessionDetails;
                        //Lock the singleton because the _attributes and _metric properties will be accessed and emptied
                        lock (this)
                        {
                            //populate the event with all _attributes and _metrics
                            PopulateCustomEvent(myEvent);
                        }
                        
                        //put the event in the putEventRequest list
                        putEventRequest.Events.Add(myEvent);

                        //execute the putEventRequest and grab the response
                        PutEventsResponse r = amazonMobileAnalyticsClient.PutEvents(putEventRequest);
                    });

                bw.RunWorkerAsync();
            }
        }

    }
}
