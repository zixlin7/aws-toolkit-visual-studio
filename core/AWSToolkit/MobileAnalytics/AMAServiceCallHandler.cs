using Amazon.MobileAnalytics;
using Amazon.MobileAnalytics.Model;
using Amazon.Runtime.Internal.Settings;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Amazon.AWSToolkit.MobileAnalytics
{
    public class AMAServiceCallHandler
    {
        internal static ILog LOGGER = LogManager.GetLogger(typeof(AMAServiceCallHandler));

        private const int BACKGROUND_QUEUE_MAX_CAPACITY = 500;
        private TimeSpan MAX_SLEEP_TIME_BEOFORE_SERVICE_CALL_ATTEMPT = TimeSpan.FromMinutes(5);
        private TimeSpan TIME_BETWEEN_QUEUE_CAPACITY_CHECKS = TimeSpan.FromMinutes(1);
        private TimeSpan MAX_SERVICE_CALL_TIME_ALLOWED = TimeSpan.FromSeconds(8);
        private DateTime _nextScheduledServiceCall;

        private Thread _mainBackgroundThread;
        private Queue<Event> _backgroundQueue;

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
            LOGGER.Info("Attempting to construct AMAServiceCallHandler.");
            LOGGER.InfoFormat("Note: User has set permission to collect analytics to: {0}. If false, no analytics will be pushed.", PermissionToCollectAnalytics);

            _backgroundQueue = new Queue<Event>(BACKGROUND_QUEUE_MAX_CAPACITY);

            string identityPoolId = MostRecentlyUsedCognitoIdentityPool;
            if (string.IsNullOrEmpty(identityPoolId) || !identityPoolId.Equals(AMAConstants.AWS_COGNITO_IDENTITY_POOL_ID))
            {
                //if null or if it doesn't match the currently stored pool id, clear the cached cognito identity id (different than pool id)
                //and write the newest identity pool id
                //when the _credentialsProvider is called next, it will handle caching a new identity id
                PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.AnalyticsCognitoIdentityId, "");
                PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.AnalyticsMostRecentlyUsedCognitoIdentityPoolId, AMAConstants.AWS_COGNITO_IDENTITY_POOL_ID);
                LOGGER.InfoFormat("AnalyticsMostRecentlyUsedCognitoIdentityPoolId needs set or updated. Clearing AnalyticsCognitoIdentityId and setting AnalyticsMostRecentlyUsedCognitoIdentityPoolId to {0} in MiscSettings", AMAConstants.AWS_COGNITO_IDENTITY_POOL_ID);
            }

            try
            {
                /* 
                 * Initialize the Amazon Cognito credentials provider.
                 * Using the Analytics extended CognitoAWSCredentials will
                 * ensure caching of the identityID for unauthticated session logging. 
                 */
                LOGGER.InfoFormat("Attempting to create credentials via Cognito using poolId: {0} and RegionEndpoint.USEast1.", AMAConstants.AWS_COGNITO_IDENTITY_POOL_ID);
                _credentialsProvider = new AnalyticsCognitoAWSCredentials(
                    AMAConstants.AWS_COGNITO_IDENTITY_POOL_ID,
                    RegionEndpoint.USEast1
                );
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to construct credentials provider.", e);
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
                    LOGGER.Error("Failed to construct AMA client.", e);
                }

                //construct the AWS Mobile Analytics Client Context
                ClientContextConfig clientContextConfig = new ClientContextConfig(CustomerGuid, AMAConstants.ClientInformation.APP_TITLE, AMAConstants.ClientInformation.APP_ID);
                _clientContext = new ClientContext(clientContextConfig);

                //start the main background thread
                LOGGER.Info("Attempting to start background thread to pull from main thread queue.");
                _mainBackgroundThread = new Thread(new ThreadStart(mainBackgroundThreadActivity));
                _mainBackgroundThread.Start();
            }

            //set when the next scheduled service call should be
            _nextScheduledServiceCall = DateTime.UtcNow + MAX_SLEEP_TIME_BEOFORE_SERVICE_CALL_ATTEMPT;
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
                if (DateTime.UtcNow > _nextScheduledServiceCall || _backgroundQueue.Count >= BACKGROUND_QUEUE_MAX_CAPACITY * 0.75)
                {
                    ForceAMAServiceCallAttempt();
                }
                else
                {
                    //just collect events from the main thread queue
                    CollectNewEventsFromMainThread();
                }

                Thread.Sleep(TIME_BETWEEN_QUEUE_CAPACITY_CHECKS);
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
        private void ServiceCallAttempt(object obj)
        {
            var request = obj as PutEventsRequest;
            if (PermissionToCollectAnalytics && !_credentialsOrClientFailedToConstruct && request != null)
            {
                if (request.Events.Count > 0)
                {
                    //Attempt to make the request
                    PutEventsResponse response = null;
                    try
                    {
                        response = _amazonMobileAnalyticsClient.PutEvents(request);
                        LOGGER.InfoFormat("Reponse from AMAClient.PutEvents(request) meta data: {0}, response HttpStatusCode: {0}", response.ResponseMetadata, response.HttpStatusCode);
                    }
                    catch (Exception e)
                    {
                        //wifi was probably off
                        LOGGER.Error("AMAClient.PutEvents(request) failed.", e);
                    }
                }
            }
        }

        /// <summary>
        /// Forces the collection of events from the main queue and
        /// attempts to push them out to the service.
        /// 
        /// This is called by both the background thread and external classes
        /// such as the SimpleMobileAnalytics class, when necessary.
        /// </summary>
        public void ForceAMAServiceCallAttempt()
        {
            CollectNewEventsFromMainThread();

            //populate the RecoveryQueue and PutEventsRequest
            Queue<Event> recoveryQueue = null;
            PutEventsRequest request = null;
            lock (_backgroundQueue)
            {
                if (_backgroundQueue.Count > 0)
                {
                    recoveryQueue = new Queue<Event>(BACKGROUND_QUEUE_MAX_CAPACITY);
                    request = new PutEventsRequest();
                    request.ClientContext = _clientContext.ToJsonString();

                    //In the event the service call fails, use the recovery queue to restore the events
                    //unload background queue into recovery queue and putEventsRequest
                    Event tempEvent;
                    while (_backgroundQueue.Count > 0)
                    {
                        tempEvent = _backgroundQueue.Dequeue();
                        request.Events.Add(tempEvent);
                        recoveryQueue.Enqueue(tempEvent);
                    }
                }
            }

            //Only make a service call if the request is populated, which it won't be if there was nothing in the queue
            if (request != null)
            {
                //start the service call thread
                Thread serviceCallThread = new Thread(new ParameterizedThreadStart(ServiceCallAttempt));
                serviceCallThread.Start(request);

                //Give the thread a set amount of seconds to push the events. If it can't, then kill it.
                var startTime = DateTime.UtcNow;
                while (serviceCallThread.IsAlive)
                {
                    if (!ShouldKeepThreadAlive(startTime))
                    {
                        //restore the background thread
                        RestoreBackgroundQueue(recoveryQueue);
                        break;
                    }

                    //sleep for a second and check again
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                //if the service call thread didn't exit gracefully, kill it
                if (serviceCallThread.IsAlive)
                {
                    LOGGER.DebugFormat("Service Call Thread took longer than {0} seconds. Aborting the call.", MAX_SERVICE_CALL_TIME_ALLOWED.TotalSeconds);
                    try
                    {
                        serviceCallThread.Abort();
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error("Error aborting background service call thread.", e);
                    }
                }
            }

            //set the next scheduled service call
            _nextScheduledServiceCall = DateTime.UtcNow + MAX_SLEEP_TIME_BEOFORE_SERVICE_CALL_ATTEMPT;
        }

        /// <summary>
        /// Determines whether or not the thread has been running for too long.
        /// </summary>
        /// <param name="startTime">When was the thread started.</param>
        /// <returns>Whether or not the thread should keep executing.</returns>
        private bool ShouldKeepThreadAlive(DateTime startTime)
        {
            if ((DateTime.UtcNow - startTime) < MAX_SERVICE_CALL_TIME_ALLOWED)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Restores the background queue.
        /// </summary>
        /// <param name="recoveryQueue">the recovery queue used to restore the background queue.</param>
        private void RestoreBackgroundQueue(Queue<Event> recoveryQueue)
        {
            lock (_backgroundQueue)
            {
                //unload background into recovery
                while (recoveryQueue.Count < BACKGROUND_QUEUE_MAX_CAPACITY && _backgroundQueue.Count > 0)
                {
                    recoveryQueue.Enqueue(_backgroundQueue.Dequeue());
                }

                //reload background with proper order
                while (_backgroundQueue.Count < BACKGROUND_QUEUE_MAX_CAPACITY && recoveryQueue.Count > 0)
                {
                    _backgroundQueue.Enqueue(recoveryQueue.Dequeue());
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
                    string analyticsPermission = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.AnalyticsPermitted);
                    return string.Equals(analyticsPermission, "true", StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception e)
                {
                    LOGGER.Error("Failed to access MiscSettings file. We don't know if we have permission to collect analytics. Assuming we do not have permission.");
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
                    string customerId = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.AnalyticsAnonymousCustomerId);
                    if (string.IsNullOrEmpty(customerId))
                    {
                        Guid g = Guid.NewGuid();
                        customerId = g.ToString();
                        PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.AnalyticsAnonymousCustomerId, customerId);
                    }

                    return customerId;
                }
                catch (Exception e)
                {
                    LOGGER.Error("Failed to access AnalyticsAnonymousCustomerId in MiscSettings. Creating a new one.", e);
                    return Guid.NewGuid().ToString();
                }
            }
        }

        /// <summary>
        /// Gets or generates the most recently cached cognito identity pool ID in the APP Data.
        /// We cache this so we know whether or not we need to clear the cached cognito identityID
        /// </summary>
        private string MostRecentlyUsedCognitoIdentityPool
        {
            get
            {
                //try to retrieve the most recently used cognito identity pool.
                try
                {
                    return PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.AnalyticsMostRecentlyUsedCognitoIdentityPoolId);
                }
                catch (Exception e)
                {
                    LOGGER.Error("Failed to access AnalyticsMostRecentlyUsedCognitoIdentityPoolId in MiscSettings file.", e);
                    return null;
                }
            }
            set
            {
                PersistenceManager.Instance.SetSetting(ToolkitSettingsConstants.AnalyticsMostRecentlyUsedCognitoIdentityPoolId, value);
            }
        }

    }
}
