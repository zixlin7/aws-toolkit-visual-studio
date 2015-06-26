using Amazon.MobileAnalytics;
using Amazon.MobileAnalytics.Model;
using Amazon.Runtime.Internal.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Amazon.AWSToolkit.MobileAnalytics
{
    public class AMAServiceCallHandler
    {
        private const int BACKGROUND_QUEUE_MAX_CAPACITY = 500;
        private const int MAX_SLEEP_TIME_BEOFRE_SERVICE_CALL_ATTEMPT = 300000; //5 minutes in milliseconds
        private const int TIME_BETWEEN_QUEUE_CHECKS = 60000; //1 minute in milliseconds

        private Thread _mainBackgroundThread;
        private Queue<Event> _backgroundQueue;
        private int _maxTimesAllowedToSleepBeforeServiceCallAttempt = MAX_SLEEP_TIME_BEOFRE_SERVICE_CALL_ATTEMPT / TIME_BETWEEN_QUEUE_CHECKS;
        private int _timesSlept = 0;

        private AmazonMobileAnalyticsClient _amazonMobileAnalyticsClient;
        private AnalyticsCognitoAWSCredentials _credentialsProvider;
        private ClientContext _clientContext;

        //flag that prevents analytics collection in the event that we can't
        //generate the necessary prepatory service calls
        //we won't need this once the high level client comes available
        private bool _credentialsOrClientFailedToConstruct = false;

        /*
         * Create a singleton instance of this class
         */
        private static volatile AMAServiceCallHandler instance;
        private static object syncRoot = new Object();
        public static AMAServiceCallHandler Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new AMAServiceCallHandler();
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Constructor spins up a single background thread, initializes
        /// a background queue, and constructs any necessary credentials/clients
        /// </summary>
        private AMAServiceCallHandler()
        {
            _mainBackgroundThread = new Thread(new ThreadStart(mainBackgroundThreadActivity));
            _backgroundQueue = new Queue<Event>(BACKGROUND_QUEUE_MAX_CAPACITY);

            try
            {
                /* 
                 * Initialize the Amazon Cognito credentials provider.
                 * Using the Analytics extended CognitoAWSCredentials will
                 * ensure caching of the identityID for unauthticated session logging. 
                 */
                _credentialsProvider = new AnalyticsCognitoAWSCredentials(
                    AMAConstants.AWS_COGNITO_IDENTITY_POOL_ID,
                    RegionEndpoint.USEast1
                );
            }
            catch
            {
                //log me eventually
                _credentialsOrClientFailedToConstruct = true;
            }

            if (!_credentialsOrClientFailedToConstruct)
            {
                try
                {
                    //construct the AWS Mobile Analytics Client
                    _amazonMobileAnalyticsClient = new AmazonMobileAnalyticsClient(_credentialsProvider, RegionEndpoint.USEast1);
                }
                catch (Exception e)
                {
                    //log me eventually
                }
            }

            //construct the AWS Mobile Analytics Client Context
            ClientContextConfig clientContextConfig = new ClientContextConfig(CustomerGuid, AMAConstants.ClientInformation.APP_TITLE, AMAConstants.ClientInformation.APP_ID);
            _clientContext = new ClientContext(clientContextConfig);

            _mainBackgroundThread.Start();
        }

        private void mainBackgroundThreadActivity()
        {
            while (true)
            {
                /*
                 * Check to see if:
                 * 1) If you've slept longer than the time defined by MAX_SLEEP_TIME_BEOFRE_SERVICE_CALL_ATTEMPT_MILLISECONDS
                 * 2) The queue is full
                 * 
                 * In either case, attempt to make a service call so the queue doesn't have to reject new incoming events
                 */
                if (_timesSlept >= _maxTimesAllowedToSleepBeforeServiceCallAttempt || _backgroundQueue.Count >= BACKGROUND_QUEUE_MAX_CAPACITY * 0.75)
                {
                    _timesSlept = 0;
                    ForceAMAServiceCallAttempt();
                }
                else
                {
                    //just collect events from the main thread queue
                    CollectNewEventsFromMainThread();
                }

                Thread.Sleep(TIME_BETWEEN_QUEUE_CHECKS);
                _timesSlept++;
            }
        }

        /// <summary>
        /// Collects events from the main thread queue and stores them in the background thread.
        /// </summary>
        /// <returns>Whether or not the collection was successful</returns>
        private bool CollectNewEventsFromMainThread()
        {
            lock (SimpleMobileAnalytics.Instance.EventQueue)
            {
                while (SimpleMobileAnalytics.Instance.EventQueue.Count != 0)
                {
                    //If the backgroundQueue is full, return a failed fetch attempt
                    if (_backgroundQueue.Count >= BACKGROUND_QUEUE_MAX_CAPACITY)
                        return false;

                    //otherwise, keep queuing the new events into the background thread
                    _backgroundQueue.Enqueue(SimpleMobileAnalytics.Instance.EventQueue.Dequeue());
                }
            }

            return true;
        }

        /// <summary>
        /// Attempt to offload the background queue and push the events to AMA.
        /// Note: this method is should ONLY be called from the ForceAMAServiceCallAttempt() method
        /// because that method will time out the service call thread if need be in order to
        /// mitigate customer pain.
        /// </summary>
        private void ServiceCallAttempt()
        {
            if (PermissionToCollectAnalytics && !_credentialsOrClientFailedToConstruct)
            {
                PutEventsRequest request = new PutEventsRequest();
                request.ClientContext = _clientContext.ToJsonString();

                lock (_backgroundQueue)
                {
                    //only bother with service calls if the backgroundQueue has something in it.
                    if (_backgroundQueue.Count > 0)
                    {
                        //In the event the service call fails, use the queue to restore the events
                        Queue<Event> recoveryQueue = new Queue<Event>(BACKGROUND_QUEUE_MAX_CAPACITY);
                        Event tempEvent;

                        //unload background queue into recovery queue and putEventsRequest
                        while (_backgroundQueue.Count > 0)
                        {
                            tempEvent = _backgroundQueue.Dequeue();
                            request.Events.Add(tempEvent);
                            recoveryQueue.Enqueue(tempEvent);
                        }

                        //Attemp to make the request
                        PutEventsResponse response = null;
                        try
                        {
                            response = _amazonMobileAnalyticsClient.PutEvents(request);
                        }
                        catch (Exception e)
                        {
                            //wifi was probably off
                        }

                        //if the call failed, restore the background queue
                        if (response != null && !response.HttpStatusCode.ToString().Equals("Accepted"))
                        {
                            _backgroundQueue = recoveryQueue;
                        }
                    }
                }
            }

        }

        /// <summary>
        /// Forces the collection of events from the main queue and
        /// attempting to push them out to the service.
        /// 
        /// This is called by both the background thread and external classes
        /// such as the SimpleMobileAnalytics class when necessary.
        /// </summary>
        public void ForceAMAServiceCallAttempt()
        {
            CollectNewEventsFromMainThread();

            Thread serviceCallThread = new Thread(new ThreadStart(ServiceCallAttempt));
            serviceCallThread.Start();

            //Give the thread 8 seconds to push the events. If it can't then kill it.
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalSeconds < 8 && serviceCallThread.IsAlive);
            
            if (serviceCallThread.IsAlive)
            {
                try
                {
                    serviceCallThread.Abort();
                }
                catch (Exception e)
                {
                    //Log me eventually
                }
            }
        }

        /// <summary>
        /// Returns whether or not the customer has given us permission to collect analytics.
        /// </summary>
        private bool PermissionToCollectAnalytics
        {
            get
            {
                //try to retrieve permission. If we can't access the file for whatever reason, assume we don't have permission.
                try
                {
                    string analyticsPermission = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.AnalyticsPermission);
                    return (analyticsPermission.Equals("true")) ? true : false;
                }
                catch (Exception e)
                {
                    //log exception eventually

                    return false;
                }
            }
        }

        /// <summary>
        /// Gets or Generates a unique, non-identifying/anonymous customer GUID cached in the APP Data.
        /// </summary>
        private string CustomerGuid
        {
            get
            {
                //try to retrieve customer guid. If we can't access the file for whatever reason, create an arbitrary one.
                try
                {
                    string customerGuid = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.AnalyticsCustomerGuid);
                    if (string.IsNullOrEmpty(customerGuid))
                    {
                        Guid g = Guid.NewGuid();
                        customerGuid = g.ToString();
                        PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.AnalyticsCustomerGuid, customerGuid);
                    }

                    return customerGuid;
                }
                catch (Exception e)
                {
                    return Guid.NewGuid().ToString();
                }
            }
        }

    }
}
