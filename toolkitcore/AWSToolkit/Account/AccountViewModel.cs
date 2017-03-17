using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Amazon.Runtime.Internal.Settings;

using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement.Internal;
using Amazon.Runtime.CredentialManagement;

using log4net;

namespace Amazon.AWSToolkit.Account
{
    public class AccountViewModel : AbstractViewModel
    {
        ILog LOGGER = LogManager.GetLogger(typeof(AccountViewModel));

        public static readonly ObservableCollection<IViewModel> NO_CHILDREN = new ObservableCollection<IViewModel>();

        readonly AccountViewMetaNode _metaNode;
        AWSViewModel _awsViewModel;

        // credentials obtained on-demand from the bound profile
        AWSCredentials _credentials;
        CredentialProfile _profile;
        ICredentialProfileStore _profileStore;

        string _displayName;

        ObservableCollection<IViewModel> _serviceViewModels;

        private Dictionary<string, ServiceSpecificCredentials> _cachedServiceCredentials;

        public AccountViewModel(AccountViewMetaNode metaNode, AWSViewModel awsViewModel, ICredentialProfileStore profileStore, CredentialProfile profile)
            : base(metaNode, awsViewModel, profile.Name)
        {
            this._metaNode = metaNode;
            this._awsViewModel = awsViewModel;
            this._profileStore = profileStore;
            this._profile = profile;
            this._displayName = profile.Name;

            this._credentials = this._profile.GetAWSCredentials(this._profileStore);

            this.CreateServiceChildren();
        }

        public void ReloadFromPersistence()
        {
            this._profile = ProfileStore.ListProfiles().FirstOrDefault(x =>
            {
                return string.Equals(CredentialProfileUtils.GetUniqueKey(x), this.SettingsUniqueKey, StringComparison.Ordinal);
            });

            this._displayName = this._profile.Name;
            this._credentials = this._profile.GetAWSCredentials(ProfileStore);

            if (_cachedServiceCredentials != null)
                _cachedServiceCredentials.Clear();
            else
                _cachedServiceCredentials = new Dictionary<string, ServiceSpecificCredentials>(StringComparer.OrdinalIgnoreCase);
        }



        internal void CreateServiceChildren()
        {
            var region = ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints;
            if (region == null)
                return;

            IList<ServiceRootViewModel> services = new List<ServiceRootViewModel>();
            foreach (var child in this._metaNode.Children)
            {
                var endPoint = region.GetEndpoint(child.EndPointSystemName);
                var serviceRootMetaNode = child as ServiceRootViewMetaNode;
                if (serviceRootMetaNode == null || endPoint == null)
                    continue;
                
                var model = serviceRootMetaNode.CreateServiceRootModel(this);
                services.Add(model);
            }

            if (this._serviceViewModels == null)
                this._serviceViewModels = new ObservableCollection<IViewModel>();
            else
                this._serviceViewModels.Clear();

            foreach (var service in services.OrderBy(x => x.Name))
            {
                this._serviceViewModels.Add(service);
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.Controls.Resources.Accounts.tree-node.png";
            }
        }

        public string SettingsUniqueKey
        {
            get
            {
                CredentialProfileUtils.EnsureUniqueKeyAssigned(this._profile, this._profileStore);
                return CredentialProfileUtils.GetUniqueKey(this._profile).ToString();
            }
        }

        public override string Name
        {
            get { return this.DisplayName; }
        }

        public string DisplayName
        {
            get { return this._profile.Name; }
            set
            {
                this._displayName = value;
                base.NotifyPropertyChanged("DisplayName");
                base.NotifyPropertyChanged("Name");
            }
        }

        /// <summary>
        /// Returns the AWSCredentials instance bound to the model. To obtain actual
        /// keys, use the CredentialKeys property.
        /// </summary>
        public AWSCredentials Credentials
        {
            get
            {
                if (_credentials == null)
                    throw new InvalidOperationException("No credential profile has been loaded.");

                return _credentials;
            }
        }

        public ICredentialProfileStore ProfileStore
        {
            get { return this._profileStore; }
        }

        public CredentialProfile Profile
        {
            get { return this._profile; }
        }

        public string AccountNumber
        {
            get
            {
                SettingsCollection settings = null;
                if(this.ProfileStore is NetSDKCredentialsFile)
                {
                    settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
                }
                else if (this.ProfileStore is SharedCredentialsFile)
                {
                    settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.NonNetSDKCredentialStoreMetadata);
                }

                if(settings != null)
                {
                    var os = settings[this.SettingsUniqueKey];
                    return os?[ToolkitSettingsConstants.AccountNumberField];
                }

                return null;
            }
        }

        public string UniqueIdentifier
        {
            get
            {
                if (!string.IsNullOrEmpty(AccountNumber))
                    return AccountNumber.Trim().Replace("-", ""); // Get rid of hyphens so we won't get inconsistent names if used to create buckets

                var credentials = Credentials.GetCredentials();
                if (string.IsNullOrEmpty(credentials.Token))
                    return credentials.AccessKey;

                throw new InvalidOperationException("Temporary credentials cannot be used to generate a unique identifier.");
            }
        }

        public bool HasRestrictions
        {
            get { return GetRestrictions()?.Count > 0; }
        }
        
        public HashSet<string> Restrictions
        {
            get
            {
                return GetRestrictions();
            }
        }

        /// <summary>
        /// Returns any service specific credentials persisted for a service.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public ServiceSpecificCredentials GetCredentialsForService(string serviceName)
        {
            var serviceCredentials = ServiceSpecificCredentialStoreManager
                                        .Instance
                                        .GetCredentialsForService(this.SettingsUniqueKey, serviceName);
            if (serviceCredentials != null)
                _cachedServiceCredentials[serviceName] = serviceCredentials;

            return serviceCredentials;
        }

        public void SaveCredentialsForService(string serviceName, string userName, string password)
        {
            var serviceCredentials = ServiceSpecificCredentialStoreManager
                                        .Instance
                                        .SaveCredentialsForService(this.SettingsUniqueKey, serviceName, userName, password);
            _cachedServiceCredentials[serviceName] = serviceCredentials;
        }

        public void ClearCredentialsForService(string serviceName)
        {
            ServiceSpecificCredentialStoreManager.Instance.ClearCredentialsForService(this.SettingsUniqueKey, serviceName);
            if (_cachedServiceCredentials.ContainsKey(serviceName))
                _cachedServiceCredentials.Remove(serviceName);
        }

        private HashSet<string> GetRestrictions()
        {
            HashSet<string> restrictions = new HashSet<string>();

            SettingsCollection settings = null;
            if (this.ProfileStore is NetSDKCredentialsFile)
            {
                settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
            }
            else if (this.ProfileStore is SharedCredentialsFile)
            {
                settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.NonNetSDKCredentialStoreMetadata);
            }

            if(settings != null)
            {
                var os = settings[this.SettingsUniqueKey];

                var str = os?[ToolkitSettingsConstants.Restrictions];
                if (str != null)
                {
                    foreach (var token in str.Split(','))
                    {
                        if (!string.IsNullOrEmpty(token))
                            restrictions.Add(token);
                    }
                }
            }

            return restrictions;
        }


        public override ObservableCollection<IViewModel> Children
        {
            get
            {
                return this._serviceViewModels;
            }
        }

        public void FullReload(bool async)
        {
            Refresh(async);
        }

        public override void Refresh(bool async)
        {
            if (RegionEndPointsManager.Instance.FailedToLoad)
            {
                RegionEndPointsManager.Instance.Refresh();
            }

            foreach (IViewModel child in this.Children)
            {
                child.Refresh(async);
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            if (!string.IsNullOrEmpty(this.AccountNumber))
            {
                dndDataObjects.SetData("ACCOUNT_NUMBER", this.AccountViewModel.AccountNumber);
            }
        }

        public override string ToString()
        {
            if (_credentials == null)
                return string.Empty;

            var c = Credentials.GetCredentials();
            return string.Concat(DisplayName, "/", c.AccessKey);
        }

        public T CreateServiceClient<T>(RegionEndPointsManager.EndPoint endpoint) where T : class
        {
            Type type = typeof(T);
            try
            {
                var configTypeName = type.FullName.Replace("Client", "Config");
                var configType = type.Assembly.GetType(configTypeName);
                var config = Activator.CreateInstance(configType) as ClientConfig;
                config.ServiceURL = endpoint.Url;
                config.AuthenticationRegion = endpoint.AuthRegion;

                var constructor = type.GetConstructor(new[] { typeof(AWSCredentials), configType });
                var client = constructor.Invoke(new object[] { Credentials, config }) as T;
                return client;
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Error constructing service client type {0}: {1}", type, e.Message);
            }

            return null;
        }

        public T CreateServiceClient<T>(RegionEndPointsManager.RegionEndPoints region) where T : class
        {
            Type type = typeof(T);
            try
            {
                var constructor = type.GetConstructor(new[] { typeof(AWSCredentials),typeof(RegionEndpoint) });
                var client = constructor.Invoke(new object[] { Credentials, RegionEndpoint.GetBySystemName(region.SystemName) }) as T;
                return client;
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Error constructing service client type {0}: {1}", type, e.Message);    
            }

            return null;
        }
    }
}
