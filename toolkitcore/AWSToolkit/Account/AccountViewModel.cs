﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Amazon.Runtime.Internal.Settings;

using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Tasks;
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
        private string _accountNumber;
        string _displayName;

        ObservableCollection<IViewModel> _serviceViewModels;

        private Dictionary<string, ServiceSpecificCredentials> _cachedServiceCredentials
            = new Dictionary<string, ServiceSpecificCredentials>(StringComparer.OrdinalIgnoreCase);

        public AccountViewModel(AccountViewMetaNode metaNode, AWSViewModel awsViewModel, ICredentialProfileStore profileStore, CredentialProfile profile)
            : base(metaNode, awsViewModel, profile.Name)
        {
            this._metaNode = metaNode;
            this._awsViewModel = awsViewModel;
            this._profileStore = profileStore;
            this._profile = profile;
            this._displayName = profile.Name;

            this._credentials = this._profile.GetAWSCredentials(this._profileStore);
            DetermineAccountNumber();
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

            _cachedServiceCredentials.Clear();
        }

        internal void CreateServiceChildren()
        {
            var region = ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints;
            if (region == null)
                return;

            IList<ServiceRootViewModel> services = new List<ServiceRootViewModel>();
            foreach (var child in this._metaNode.Children)
            {
                var serviceRootMetaNode = child as ServiceRootViewMetaNode;
                if (serviceRootMetaNode != null && serviceRootMetaNode.CanSupportRegion(region))
                {
                    var model = serviceRootMetaNode.CreateServiceRootModel(this);
                    services.Add(model);
                }
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

        protected override string IconName => "Amazon.AWSToolkit.Controls.Resources.Accounts.tree-node.png";

        public string SettingsUniqueKey
        {
            get
            {
                CredentialProfileUtils.EnsureUniqueKeyAssigned(this._profile, this._profileStore);
                return CredentialProfileUtils.GetUniqueKey(this._profile).ToString();
            }
        }

        public override string Name => this.DisplayName;

        public string DisplayName
        {
            get => this._profile.Name;
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

        public ICredentialProfileStore ProfileStore => this._profileStore;

        public CredentialProfile Profile => this._profile;

        public string AccountNumber
        {
            get => _accountNumber;
            set => _accountNumber = value;
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

        public bool HasRestrictions => GetRestrictions()?.Count > 0;

        public HashSet<string> Restrictions => GetRestrictions();

        /// <summary>
        /// Returns any service specific credentials persisted for a service.
        /// </summary>
        /// <param name="serviceName"></param>
        /// <returns></returns>
        public ServiceSpecificCredentials GetCredentialsForService(string serviceName)
        {
            var serviceCredentials = ServiceSpecificCredentialStore
                                        .Instance
                                        .GetCredentialsForService(this.SettingsUniqueKey, serviceName);
            if (serviceCredentials != null)
                _cachedServiceCredentials[serviceName] = serviceCredentials;

            return serviceCredentials;
        }

        public void SaveCredentialsForService(string serviceName, string userName, string password)
        {
            var serviceCredentials = ServiceSpecificCredentialStore
                                        .Instance
                                        .SaveCredentialsForService(this.SettingsUniqueKey, serviceName, userName, password);
            _cachedServiceCredentials[serviceName] = serviceCredentials;
        }

        public void ClearCredentialsForService(string serviceName)
        {
            ServiceSpecificCredentialStore.Instance.ClearCredentialsForService(this.SettingsUniqueKey, serviceName);
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


        public override ObservableCollection<IViewModel> Children => this._serviceViewModels;

        public void FullReload(bool async)
        {
            Refresh(async);
        }

        public override void Refresh(bool async)
        {
            if (RegionEndPointsManager.GetInstance().FailedToLoad)
            {
                RegionEndPointsManager.GetInstance().Refresh();
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
                endpoint.ApplyToClientConfig(config);

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

        /// <summary>
        /// NOTE:This method is introduced as a stop gap to set the account number till the new credential system is integrated
        /// Determines and sets the account number for this account
        /// </summary>
        private void DetermineAccountNumber()
        {
            Task.Run(async () =>
            {
                await DetermineAccountNumberAsync();
            }).LogExceptionAndForget();
        }

        /// <summary>
        /// Determines and sets the value of the account number associated with this view model asynchronously
        /// </summary>
        private async Task DetermineAccountNumberAsync()
        {
            try
            {
                string accountNumber = null;
                SettingsCollection settings = null;
                if (this.ProfileStore is NetSDKCredentialsFile)
                {
                    settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
                }
                else if (this.ProfileStore is SharedCredentialsFile)
                {
                    settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants
                        .NonNetSDKCredentialStoreMetadata);
                }

                if (settings != null)
                {
                    var os = settings[this.SettingsUniqueKey];
                    accountNumber = os?[ToolkitSettingsConstants.AccountNumberField];
                }

                //NOTE: The sts call to the account manager is used as a stop gap till the new credential system is integrated
                this._accountNumber = accountNumber ?? await ToolkitFactory.Instance.AccountManager.GetAccountId(this);
            }
            catch(Exception e)
            {
                LOGGER.Error(e);
                this._accountNumber = null;
            }
           
        }
    }
}
