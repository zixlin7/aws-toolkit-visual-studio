namespace Amazon.AWSToolkit.MobileAnalytics
{
    /// <summary>
    /// These event standards are used universally across the various Mobile Analytics Events
    /// </summary>
    public static class AMAConstants
    {
        /// <summary>
        /// In order to start, stop, pause, or resume a session, a Mobile Analytics event requires
        /// one of these as the event name.
        /// </summary>
        public static class SessionEventNames
        {
            public const string START_SESSION = "_session.start";
            public const string STOP_SESSION = "_session.stop";
            public const string PAUSE_SESSION = "_session.pause";
            public const string RESUME_SESSION = "_session.resume";
        }
        
        /// <summary>
        /// Constants used SPECIFICALLY for the toolkits Mobile Analytics.
        /// 
        /// Note: The CLIENT_ID will most likely be replaced by GUIDs
        /// in the implementation that references this class.
        /// </summary>
        public static class ClientInformation
        {
            public const string CLIENT_ID = "anonymous_user";

            #if DEBUG
            public const string APP_TITLE = "visual-studio-toolkit-test"; //test app
            #else
            public const string APP_TITLE = "visual-studio-toolkit-prod"; //prod app
            #endif

            #if DEBUG
            public const string APP_ID = "9b7a8a27eec74c6696a15222961af9ae"; //test app
            #else
            public const string APP_ID = "ff4badf7ee1345b89e5b0fdc3a28be9e"; //prod app
            #endif
    }

        public static class EventTypes
        {
            public const string VisualStudioToolkitEvent = "VisualStudioToolkitEvent";
        }

        /// <summary>
        /// Event version number required by Mobile Analytics Service.
        /// 
        /// NOTE: Do NOT change MOBILE_ANALYTICS_EVENT_VERSION_NUMBER!!
        /// See Version documentation here: https://docs.aws.amazon.com/mobileanalytics/latest/ug/PutEvents.html
        /// </summary>
        public const string MOBILE_ANALYTICS_EVENT_VERSION_NUMBER = "v2.0";

        /// <summary>
        /// Generic session ID. You are recommended to use something more significant than this.
        /// The toolkit analytics generates a GUID for the session rather than using this constant.
        /// </summary>
        public const string SESSION_ID = "anonymous_session";

        /// <summary>
        /// Cognito Identity Pool to use for the Mobile Analytics service.
        /// </summary>
        #if DEBUG
        public const string AWS_COGNITO_IDENTITY_POOL_ID = "us-east-1:93504934-38ab-4e75-aa41-d0f9bf5c5268"; //test app
        #else
        public const string AWS_COGNITO_IDENTITY_POOL_ID = "us-east-1:0ab25aed-7bb5-47df-9b30-df1aa3ec1bd3"; //prod app
        #endif
    }
}
