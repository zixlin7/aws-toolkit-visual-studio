using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Amazon;
using Amazon.MobileAnalytics;
using Amazon.MobileAnalytics.Model;
using Amazon.CognitoIdentity;
using System.ComponentModel;
using Amazon.Runtime.Internal.Settings;

namespace Amazon.AWSToolkit.MobileAnalytics
{
    public class SimpleMobileAnalytics
    {
        private const int MAX_QUEUE_SIZE = 500;
        private Queue<Event> _eventQueue;

        private Session _startSessionDetails;
        private string _sessionIdFromGuid;

        private AMAServiceCallHandler _serviceCallHandler;

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
            //initialize the event queue
            _eventQueue = new Queue<Event>(MAX_QUEUE_SIZE);

            //generate a session GUID
            _sessionIdFromGuid = SessionIdGuid;

            /*
             * Start the toolkit session - it will be closed when the VS Shell changes
             * see https://msdn.microsoft.com/en-us/library/microsoft.visualstudio.shell.interop.ivspackage.close.aspx
             * for reference on how we're notified when the user closes VS
             */
            StartMainSession();

            //Spin up instance/thread of the ServiceCallHandler
            _serviceCallHandler = AMAServiceCallHandler.Instance;
        }
        
        /// <summary>
        /// Generates a unique session GUID.
        /// </summary>
        private string SessionIdGuid
        {
            get
            {
                string _sessionIdGUID = _sessionIdFromGuid;
                if (string.IsNullOrEmpty(_sessionIdGUID))
                {
                    Guid g = Guid.NewGuid();
                    _sessionIdGUID = g.ToString();
                }

                return _sessionIdGUID;
            }
        }

        /// <summary>
        /// Internal method used to start a mobile analytics session when the toolkit is initialized.
        /// It is intended to only occur one time for each "session" of the toolkit use.
        /// i.e. Whenever a user opens the toolkit after closing it or for the first time.
        /// </summary>
        /// <returns></returns>
        private void StartMainSession()
        {
            DateTime startTime = DateTime.UtcNow;
            _startSessionDetails = new Session()
            {
                StartTimestamp = startTime,
                Id = _sessionIdFromGuid
            };

            ToolkitEvent startSessionEvent = new ToolkitEvent(AMAConstants.SessionEventNames.START_SESSION, _startSessionDetails);
            QueueEventToBeRecorded(startSessionEvent);
        }


        /// <summary>
        /// This method is only intended to be called once the toolkit is closed.
        /// This is currently triggered in the IVsPackage.Close() Method in AWSToolkitPackage.cs.
        /// Unless you have a specific reason to trigger the end the overall toolkit session, don't call it.
        /// </summary>
        public void StopMainSession()
        {
            DateTime stopTime = DateTime.UtcNow;
            _startSessionDetails.StopTimestamp = stopTime;

            ToolkitEvent stopSessionEvent = new ToolkitEvent(AMAConstants.SessionEventNames.STOP_SESSION, _startSessionDetails);
            QueueEventToBeRecorded(stopSessionEvent);

            //This method trigger when VS closes so we need to force the service call.
            //If we don't the background thread won't collect any remaining events in the queue.
            _serviceCallHandler.ForceAMAServiceCallAttempt();
        }

        /// <summary>
        /// Verifies the event is properly populated.
        /// </summary>
        /// <param name="analyticsEvent">The event to be validated.</param>
        /// <returns></returns>
        private bool ValidEvent(Event analyticsEvent)
        {
            //check to make sure Event has right version number
            if (!analyticsEvent.Version.Equals(AMAConstants.MOBILE_ANALYTICS_EVENT_VERSION_NUMBER))
                return false;

            //Ensure eventType is populated
            if (string.IsNullOrWhiteSpace(analyticsEvent.EventType))
                return false;

            //Ensure event timestamp is populated
            if (analyticsEvent.Timestamp == DateTime.MinValue)
                return false;

            //If the session is null, create one and use the main session to populate it
            if (analyticsEvent.Session == null)
            {
                analyticsEvent.Session = new Session();
                analyticsEvent.Session.Id = _startSessionDetails.Id;
                analyticsEvent.Session.StartTimestamp = _startSessionDetails.StartTimestamp;
            }

            //Ensure a custom session was populated correctly
            if (string.IsNullOrWhiteSpace(analyticsEvent.Session.Id))
                return false;

            //Ensure a custom session was populated correctly
            if (analyticsEvent.Session.StartTimestamp == DateTime.MinValue)
                return false;

            return true;
        }

        /// <summary>
        /// Queues an event to be sent to the Mobile Analytics service on a background thread.
        /// </summary>
        /// <param name="analyticsEvent">The event to be queued.</param>
        /// <returns>Whether or not the event was successfully queued.</returns>
        public bool QueueEventToBeRecorded(ToolkitEvent toolkitEvent)
        {
            Event analyticsEvent = toolkitEvent.ConvertToMobileAnalyticsEvent();

            if (ValidEvent(analyticsEvent) && _eventQueue.Count < MAX_QUEUE_SIZE)
            {
                lock (_eventQueue)
                {
                    _eventQueue.Enqueue(analyticsEvent);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the _eventQueue storing any events that occured.
        /// </summary>
        public Queue<Event> EventQueue { get { return _eventQueue; }  }
    }
}
