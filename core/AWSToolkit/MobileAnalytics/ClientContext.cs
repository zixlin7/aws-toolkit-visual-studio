﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.MobileAnalytics
{
    public class ClientContext
    {

        //client related keys
        private const string CLIENT_KEY = "client";
        private const string CLIENT_ID_KEY = "client_id";
        private const string CLIENT_APP_TITLE_KEY = "app_title";
        private const string CLIENT_APP_VERSION_NAME_KEY = "app_version_name";
        private const string CLIENT_APP_VERSION_CODE_KEY = "app_version_code";
        private const string CLIENT_APP_PACKAGE_NAME_KEY = "app_package_name";

        //custom keys
        private const string CUSTOM_KEY = "custom";

        //env related keys
        private const string ENV_KEY = "env";
        private const string ENV_PLATFORM_KEY = "platform";
        private const string ENV_MODEL_KEY = "model";
        private const string ENV_MAKE_KEY = "make";
        private const string ENV_PLATFORM_VERSION_KEY = "platform_version";
        private const string ENV_LOCALE_KEY = "locale";

        //servies related keys
        private const string SERVICES_KEY = "services";
        private const string SERVICE_MOBILE_ANALYTICS_KEY = "mobile_analytics";
        private const string SERVICE_MOBILE_ANALYTICS_APP_ID_KEY = "app_id";

        private IDictionary<string, string> _client;
        private IDictionary<string, string> _custom;
        private IDictionary<string, string> _env;
        private IDictionary<string, IDictionary> _services;

        private IDictionary _clientContext;

        //env and platform related values
        private string _envPlatformVersion = "";
        private string _envLocale = "";
        private string _envMake = "";
        private string _envModel = "";

        private ClientContextConfig _config;

        private static object _lock = new object();

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="config">Config</param>
        public ClientContext(ClientContextConfig config)
        {
            this.Config = config;
            _custom = new Dictionary<string, string>();

            try
            {
                _envPlatformVersion = System.Environment.OSVersion.ToString();
            }
            catch (Exception e)
            {
                //log exception eventually
                _envPlatformVersion = "";
            }

            try
            {
                _envLocale = CultureInfo.CurrentCulture.ToString();
            }
            catch (Exception e)
            {
                //log exception eventually
                _envLocale = "";
            }

            _envPlatformVersion = typeof(ClientContext).Assembly.GetName().Version.ToString();
        }

        /// <summary>
        /// Gets or sets the config.
        /// </summary>
        /// <value>The config.</value>
        public ClientContextConfig Config
        {
            set
            {
                _config = value;
            }

            get
            {
                return _config;
            }
        }

        /// <summary>
        /// Adds the custom attributes to the Client Context
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void AddCustomAttributes(string key, string value)
        {
            lock (_lock)
            {
                _custom.Add(key, value);
            }
        }

        /// <summary>
        /// Gets a Json Representation of the Client Context
        /// </summary>
        /// <returns>Json Representation of Client Context</returns>
        public String ToJsonString()
        {
            lock (_lock)
            {
                _client = new Dictionary<string, string>();
                _env = new Dictionary<string, string>();
                _services = new Dictionary<string, IDictionary>();

                // client
                _client.Add(CLIENT_ID_KEY, Config.ClientId);
                _client.Add(CLIENT_APP_TITLE_KEY, Config.AppTitle);

                if (!string.IsNullOrEmpty(Config.AppVersionName))
                {
                    _client.Add(CLIENT_APP_VERSION_NAME_KEY, Config.AppVersionName);
                }

                if (!string.IsNullOrEmpty(Config.AppVersionCode))
                {
                    _client.Add(CLIENT_APP_VERSION_CODE_KEY, Config.AppVersionCode);
                }

                if (!string.IsNullOrEmpty(Config.AppPackageName))
                {
                    _client.Add(CLIENT_APP_PACKAGE_NAME_KEY, Config.AppPackageName);
                }


                // env
                _env.Add(ENV_PLATFORM_KEY, "Windows");

                if (!string.IsNullOrEmpty(_envPlatformVersion))
                {
                    _env.Add(ENV_PLATFORM_VERSION_KEY, _envPlatformVersion);
                }

                if (!string.IsNullOrEmpty(_envLocale))
                {
                    _env.Add(ENV_LOCALE_KEY, _envLocale);
                }

                if (!string.IsNullOrEmpty(_envMake))
                {
                    _env.Add(ENV_MAKE_KEY, _envMake);
                }

                if (!string.IsNullOrEmpty(_envModel))
                {
                    _env.Add(ENV_MODEL_KEY, _envModel);
                }

                // services
                IDictionary mobileAnalyticsService = new Dictionary<string, string>();
                mobileAnalyticsService.Add(SERVICE_MOBILE_ANALYTICS_APP_ID_KEY, Config.AppId);
                _services.Add(SERVICE_MOBILE_ANALYTICS_KEY, mobileAnalyticsService);


                _clientContext = new Dictionary<string, IDictionary>();
                _clientContext.Add(CLIENT_KEY, _client);
                _clientContext.Add(ENV_KEY, _env);
                _clientContext.Add(CUSTOM_KEY, _custom);
                _clientContext.Add(SERVICES_KEY, _services);

                return JsonMapper.ToJson(_clientContext);
            }
        }

        /// <summary>
        /// Gets or sets the environment Locale. This is an optional field for any event call.
        /// </summary>
        /// <value>The environment Locale.</value>
        public string EnvLocale
        { 
            get
            {
                return _envLocale;
            }
            set
            {
                _envLocale = value;
            }
        }

        /// <summary>
        /// Gets or sets the environment Make. This is an optional field for any event call.
        /// </summary>
        /// <value>The environment Make.</value>
        public string EnvMake
        { 
            get
            {
                return _envMake;
            }
            set
            {
                _envMake = value;
            }
        }

        /// <summary>
        /// Gets or sets the environment Model. This is an optional field for any event call.
        /// </summary>
        /// <value>The environment Model.</value>
        public string EnvModel
        { 
            get
            {
                return _envModel;
            }
            set
            {
                _envModel = value;
            }
        }

        /// <summary>
        /// Gets or sets the environment Platform Version. This is an optional field for any event call.
        /// </summary>
        /// <value>The environment Version.</value>
        public string EnvPlatformVersion
        {
            get
            {
                return _envPlatformVersion;
            }
            set
            {
                _envPlatformVersion = value;
            }
        }

    }
}
