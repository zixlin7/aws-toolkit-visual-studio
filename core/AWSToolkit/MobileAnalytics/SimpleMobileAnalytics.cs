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

namespace Amazon.AWSToolkit.MobileAnalytics
{
    public enum Attributes
    {
        OpenedView,
    };

    public enum Metrics
    {
        TimeSpentInView,
    };

    public class SimpleMobileAnalytics
    {
        /*
         * Need to add a flag for the opt in program
         */

        AmazonMobileAnalyticsClient client;
        private CognitoAWSCredentials credentialsProvider;
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

        private Dictionary<string, string> _attributes;
        private Dictionary<string, double> _metrics;

        public void AddProperty(Attributes attributeKey, string attributeValue)
        {
            _attributes.Add(attributeKey.ToString(), attributeValue);
        }

        public void AddProperty(Metrics metricKey, double metricValue)
        {
            _metrics.Add(metricKey.ToString(), metricValue);
        }

        public SimpleMobileAnalytics() {
            // Initialize the Amazon Cognito credentials provider
            credentialsProvider = new CognitoAWSCredentials(
                AWS_COGNITO_IDENTITY_POOL_ID, // Identity Pool ID
                RegionEndpoint.USEast1 // Region
            );
            //construct the AWS Mobile Analytics Client
            client = new AmazonMobileAnalyticsClient(credentialsProvider, RegionEndpoint.USEast1);
            //construct the AWS Mobile Analytics Client Context
            ClientContextConfig config = new ClientContextConfig(CLIENT_ID, APP_TITLE, APP_ID);
            clientContext = new ClientContext(config);

            //initialize attributes and metrics
            _attributes = new Dictionary<string, string>();
            _metrics = new Dictionary<string, double>();
        }

        private Session StartSession()
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
            client.PutEvents(putEventRequest);

            return startSession.Session;
        }

        private void StopSession(Session startSessionDetails)
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
                    StartTimestamp = startSessionDetails.StartTimestamp,
                    StopTimestamp = endTime,
                    Id = SESSION_ID
                }
            };

            putEventRequest.Events.Add(stopSession);
            client.PutEvents(putEventRequest);
        }

        private void PopulateCustomEvent(Event customEvent)
        {
            customEvent.EventType = "OpenView";
            customEvent.Version = "v2.0"; //Do not change this!!! See Version documentation here: http://docs.aws.amazon.com/mobileanalytics/latest/ug/PutEvents.html
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

        public void RecordEventWithProperties()
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

                //start the session
                Session startSessionDetails = StartSession();

                //grab some arbitary event
                Event myEvent = new Event();
                myEvent.Session = startSessionDetails;
                PopulateCustomEvent(myEvent);

                //put the event in the putEventRequest list
                putEventRequest.Events.Add(myEvent);

                //execute the putEventRequest and grab the response
                PutEventsResponse r = client.PutEvents(putEventRequest);

                //end the session
                StopSession(startSessionDetails);
            });

            bw.RunWorkerAsync();
        }

    }
}
