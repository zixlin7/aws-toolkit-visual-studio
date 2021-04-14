using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;

using log4net;

namespace Amazon.AWSToolkit.Account
{
    public class AccountViewModel : AbstractViewModel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountViewModel));

        public static readonly ObservableCollection<IViewModel> NO_CHILDREN = new ObservableCollection<IViewModel>();
        private ICredentialIdentifier _identifier;
        readonly AccountViewMetaNode _metaNode;
        AWSViewModel _awsViewModel;
        string _displayName;

        private readonly ObservableCollection<IViewModel> _serviceViewModels = new ObservableCollection<IViewModel>();
        private readonly ToolkitContext _toolkitContext;

        private Dictionary<string, ServiceSpecificCredentials> _cachedServiceCredentials
            = new Dictionary<string, ServiceSpecificCredentials>(StringComparer.OrdinalIgnoreCase);

        public AccountViewModel(AccountViewMetaNode metaNode, AWSViewModel awsViewModel,
            ICredentialIdentifier identifier,
            ToolkitContext toolkitContext)
            : base(metaNode, awsViewModel, identifier.DisplayName)
        {
            this._metaNode = metaNode;
            this._awsViewModel = awsViewModel;
            this._identifier = identifier;
            this._displayName = identifier.DisplayName;
            this._toolkitContext = toolkitContext;
        }

        public void ReloadFromPersistence(string displayName)
        {
            var identifier = _toolkitContext.CredentialManager.GetCredentialIdentifiers()
                .FirstOrDefault(x => string.Equals(x.DisplayName, displayName));
            this._identifier = identifier;
            this._displayName = identifier?.DisplayName;
            _cachedServiceCredentials.Clear();
        }

        internal void CreateServiceChildren()
        {
            this._serviceViewModels.Clear();
            
            // TODO : make region a function parameter instead
            var region = ToolkitFactory.Instance.Navigator.SelectedRegion;
            if (region == null) { return; }

            // TODO : when region is null make the child list empty instead of preserving the
            // previous list.

            IList<ServiceRootViewModel> services = new List<ServiceRootViewModel>();
            foreach (var child in this._metaNode.Children)
            {
                var serviceRootMetaNode = child as ServiceRootViewMetaNode;
                if(serviceRootMetaNode != null && serviceRootMetaNode.CanSupportRegion(region, _toolkitContext.RegionProvider))
                {
                    var model = serviceRootMetaNode.CreateServiceRootModel(this, region);
                    services.Add(model);
                }
            }

            foreach (var service in services.OrderBy(x => x.Name))
            {
                this._serviceViewModels.Add(service);
            }
        }

        protected override string IconName => "Amazon.AWSToolkit.Controls.Resources.Accounts.tree-node.png";

        public string SettingsUniqueKey => ProfileProperties?.UniqueKey;

        public override string Name => this.DisplayName;

        public string DisplayName
        {
            get => this._identifier?.DisplayName;
            set
            {
                this._displayName = value;
                base.NotifyPropertyChanged("DisplayName");
                base.NotifyPropertyChanged("Name");
            }
        }

        public ToolkitRegion Region
        {
            get
            {
                string regionId = ProfileProperties?.Region;
                if (string.IsNullOrWhiteSpace(regionId))
                {
                    regionId = RegionEndpoint.USEast1.SystemName;
                }
                return _toolkitContext.RegionProvider?.GetRegion(regionId);
            }
        }

        public string PartitionId => Region?.PartitionId;

        public ICredentialIdentifier Identifier => this._identifier;

        /// <summary>
        /// Returns profile properties associated with the model's credential profile
        /// </summary>
        public ProfileProperties ProfileProperties
        {
            get
            {
                try
                {
                    return _toolkitContext.CredentialSettingsManager.GetProfileProperties(Identifier);
                }
                catch (Exception e)
                {
                    Logger.Error($"No profile properties found for: {DisplayName}", e);
                    return null;
                }
            }
        }

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

        public override ObservableCollection<IViewModel> Children => this._serviceViewModels;

        public void FullReload(bool async)
        {
            Refresh(async);
        }

        public override void Refresh(bool async)
        {
            foreach (IViewModel child in this.Children)
            {
                child.Refresh(async);
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            var accountId = _toolkitContext.ConnectionManager.ActiveAccountId;
            if (!string.IsNullOrEmpty(accountId))
            {
                dndDataObjects.SetData("ACCOUNT_NUMBER", accountId);
            }
        }

        public override string ToString()
        {
            if (ProfileProperties == null)
                return string.Empty;

            return string.Concat(DisplayName, "/", ProfileProperties?.AccessKey);
        }

        /// <summary>
        /// Produces an AWS Service client with credentials for this object's account,
        /// for the provided region and service client type.
        /// </summary>
        public T CreateServiceClient<T>(ToolkitRegion region) where T : class, IAmazonService
        {
            return _toolkitContext.ServiceClientManager.CreateServiceClient<T>(_identifier, region);
        }

        /// <summary>
        /// Produces an AWS Service client with credentials for this object's account,
        /// for the provided region and service client type.
        /// 
        /// CreateServiceClient overload for calling code that has additional configuration needs
        /// </summary>
        public T CreateServiceClient<T>(ToolkitRegion region, ClientConfig serviceClientConfig) where T : class, IAmazonService
        {
            return _toolkitContext.ServiceClientManager.CreateServiceClient<T>(_identifier, region, serviceClientConfig);
        }
    }
}
