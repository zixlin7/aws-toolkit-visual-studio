using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.Components;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI
{
    /// <summary>
    /// Interaction logic for DeploymentTemplateSelectorPage.xaml
    /// </summary>
    public partial class DeploymentTemplateSelectorPage : INotifyPropertyChanged
    {
        // property names used with NotifyPropertyChanged
        public static readonly string uiProperty_Region = "region";
        public static readonly string uiProperty_Account = "account";
        public static readonly string uiProperty_Template = "template";
        public static readonly string uiProperty_DeployMode = "deploymode";
        public static readonly string uiProperty_RedeployTarget = "redeploytarget";

        string _lastSeenAccount = string.Empty;
        RegisterAccountController _registerAccountController = null;
        object _syncObj = new object();
        HashSet<string> _verifiedAccounts = new HashSet<string>();
        
        // 'temp' vars allowing us to hold the service deployment we should auto-select
        // after we receive the collection of still-running deployments
        string _lastDeploymentService = string.Empty;
        string _lastDeploymentName = string.Empty;

        AdornerLayer _pageRootAdornerLayer;
        LoadingMessageAdorner _loadingMessageAdorner;

        /// <summary>
        /// Wraps ExistingDeployment instances for purpose of adding a logo image that
        /// we can show in the deployments drop-down
        /// </summary>
        class ExistingDeploymentWrapper
        {
            ExistingServiceDeployment _inner;
            public ExistingDeploymentWrapper(ExistingServiceDeployment inner)
            {
                _inner = inner;
            }

            public ExistingServiceDeployment Inner => _inner;

            public System.Windows.Media.ImageSource ServiceLogo
            {
                get
                {
                    if (_inner != null)
                    {
                        if (_inner.DeploymentService == DeploymentServiceIdentifiers.BeanstalkServiceName)
                            return IconHelper.GetIcon(this.GetType().Assembly, "beanstalk_deployment_small.png").Source;

                        if (_inner.DeploymentService == DeploymentServiceIdentifiers.CloudFormationServiceName)
                            return IconHelper.GetIcon(this.GetType().Assembly, "cloudformation_deployment_small.png").Source;
                    }

                    return null;
                }
            }

            ExistingDeploymentWrapper() { }
        }

        public DeploymentTemplateSelectorPage()
        {
            InitializeComponent();
            DataContext = this;
            _templateSelector.PropertyChanged += new PropertyChangedEventHandler(_templateSelector_PropertyChanged);
        }

        public DeploymentTemplateSelectorPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        AWSViewModel _rootViewModel;
        public AWSViewModel RootViewModel
        {
            get => IsInitialized ? _rootViewModel : null;
            set
            {                
                this._rootViewModel = value;
                this._accountSelector.IsEnabled = this.Accounts.Count != 0;
            }
        }

        ObservableCollection<AccountViewModel> _accounts;
        public ObservableCollection<AccountViewModel> Accounts
        {
            get
            {
                if(RootViewModel == null)
                    return null;

                if (this._accounts == null)
                {
                    this._accounts = new ObservableCollection<AccountViewModel>();

                    foreach (var account in this.RootViewModel.RegisteredAccounts)
                    {
                        if (!account.HasRestrictions)
                            this._accounts.Add(account);
                    }
                }

                return this._accounts;
            }
        }

        public AccountViewModel SelectedAccount
        {
            get => this._accountSelector.SelectedItem as AccountViewModel;
            protected set { if (IsInitialized) this._accountSelector.SelectedItem = value; }
        }

        bool _accountValidationPending = false;
        public bool AccountValidationPending
        {
            get
            {
                lock (_syncObj)
                    return _accountValidationPending;
            }

            set
            {
                lock (_syncObj)
                    _accountValidationPending = value;
            }
        }

        public bool IsSelectedAccountValid
        {
            get
            {
                if (AccountValidationPending)
                    return false;

                // collection only ever accessed on UI thread, no need to lock
                AccountViewModel account = _accountSelector.SelectedItem as AccountViewModel;
                return account != null && _verifiedAccounts.Contains(account.SettingsUniqueKey);
            }
        }

        public IEnumerable<DeploymentTemplateWrapperBase> Templates
        {
            set => this._templateSelector.Templates = value;
        }

        public DeploymentTemplateWrapperBase SelectedTemplate => _templateSelector.SelectedTemplate;

        public RegionEndPointsManager.RegionEndPoints SelectedRegion
        {
            get => _regionSelector.SelectedItem as RegionEndPointsManager.RegionEndPoints;
            set => _regionSelector.SelectedItem = value;
        }

        public bool RedeploySelected => _btnRedeploy.IsChecked == true;

        public void SetExistingDeployments(IEnumerable<ExistingServiceDeployment> deployments, string lastDeploymentService, string lastDeploymentName)
        {
            List<ExistingDeploymentWrapper> wrappedList = new List<ExistingDeploymentWrapper>();
            foreach (ExistingServiceDeployment item in deployments.OrderBy(x => x.DeploymentName))
            {
                var wrapper = new ExistingDeploymentWrapper(item); 
                wrappedList.Add(wrapper);
            }

            // 'post; these into call chain
            _lastDeploymentService = lastDeploymentService != null ? lastDeploymentService : string.Empty;
            _lastDeploymentName = lastDeploymentName != null ? lastDeploymentName : string.Empty;

            _existingDeployments.ItemsSource = wrappedList;
            ToggleRedeploymentMode(deployments != null && deployments.Count<ExistingServiceDeployment>() > 0);
        }

        void ToggleRedeploymentMode(bool redeployAvailable)
        {
            if (redeployAvailable)
            {
                _btnRedeploy.IsEnabled = true;
                _existingDeployments.IsEnabled = true; // xaml binding approach keyed to _btnRedeploy.IsEnabled didn't work

                // will not be null or empty in redeploy mode
                IEnumerable<ExistingDeploymentWrapper> deployments = _existingDeployments.ItemsSource as IEnumerable<ExistingDeploymentWrapper>;
                ExistingDeploymentWrapper wrapper = deployments.FirstOrDefault<ExistingDeploymentWrapper>
                    (
                        W => string.Compare(W.Inner.DeploymentService, _lastDeploymentService, true) == 0
                                && string.Compare(W.Inner.DeploymentName, _lastDeploymentName, true) == 0
                    );
                if (wrapper != null)
                {
                    _existingDeployments.SelectedItem = wrapper;
                    _btnRedeploy.IsChecked = true;
                }
                else
                    _btnDeployNew.IsChecked = true;
            }
            else
            {
                _btnDeployNew.IsChecked = true;
                _btnRedeploy.IsEnabled = false;
                _existingDeployments.IsEnabled = false; // xaml binding approach keyed to _btnRedeploy.IsEnabled didn't work
            }
        }

        public string SelectedRedeploymentName
        {
            get
            {
                string stackName = string.Empty;
                if (RedeploySelected)
                {
                    if (_existingDeployments.SelectedItem != null)
                        stackName = (_existingDeployments.SelectedItem as ExistingDeploymentWrapper).Inner.DeploymentName;
                }

                return stackName;
            }
        }

        public ExistingServiceDeployment SelectedRedeployment
        {
            get
            {
                if (RedeploySelected)
                {
                    if (_existingDeployments.SelectedItem != null)
                        return (_existingDeployments.SelectedItem as ExistingDeploymentWrapper).Inner;
                }

                return null;
            }
        }

        public void Initialize(AccountViewModel account)
        {
            if (account != null)
                this._accountSelector.SelectedItem = account;

            List<RegionEndPointsManager.RegionEndPoints> regions = new List<RegionEndPointsManager.RegionEndPoints>();
            foreach (RegionEndPointsManager.RegionEndPoints rep in RegionEndPointsManager.GetInstance().Regions)
            {
                if (rep.GetEndpoint(DeploymentServiceIdentifiers.CloudFormationServiceName) != null
                        || rep.GetEndpoint(DeploymentServiceIdentifiers.BeanstalkServiceName) != null)
                    regions.Add(rep);
            }

            this._regionSelector.ItemsSource = regions;
            if (this._regionSelector.Items.Count != 0)
            {
                var region = RegionEndPointsManager.GetInstance().GetDefaultRegionEndPoints();
                this._regionSelector.SelectedItem = region;
            }
        }

        public bool RunningDeploymentsQueryPending
        {
            set
            {
                lock (_syncObj)
                {
                    if (value)
                    {
                        if (_pageRootAdornerLayer == null)
                        {
                            _pageRootAdornerLayer = AdornerLayer.GetAdornerLayer(_deploymentTargetRoot);
                            if (_pageRootAdornerLayer == null)
                                return;
                        }

                        if (_loadingMessageAdorner == null)
                            _loadingMessageAdorner = new LoadingMessageAdorner(_deploymentTargetRoot, "Querying running deployments...");

                        var adorners = this._pageRootAdornerLayer.GetAdorners(_deploymentTargetRoot);
                        if(adorners == null || !adorners.Contains(_loadingMessageAdorner))
                            _pageRootAdornerLayer.Add(_loadingMessageAdorner);
                    }
                    else
                    {
                        if (_pageRootAdornerLayer != null)
                            _pageRootAdornerLayer.Remove(_loadingMessageAdorner);
                    }
                }
            }
        }

        void _accountEntryPopup_Loaded(object sender, RoutedEventArgs e)
        {
            if (_registerAccountController == null)
            {
                _registerAccountController = new RegisterAccountController();
                RegisterAccountControl control = new RegisterAccountControl(_registerAccountController);
                control.SetMandatoryFieldsReadyCallback(MandatoryFieldsReadinessChange);
                _accountFieldContainer.Content = control;
                _popupAccountOK.IsEnabled = false;
            }
        }

        void _popupAccountOK_Click(object sender, RoutedEventArgs e)
        {
            _registerAccountController.Persist();
            _useOtherAccount.IsChecked = false;

            RootViewModel.Refresh();

            this._accounts.Clear();
            AccountViewModel selectedAccount = null;
            foreach (AccountViewModel account in RootViewModel.RegisteredAccounts)
            {
                if (!account.HasRestrictions)
                {
                    this._accounts.Add(account);

                    if (string.Compare(account.AccountDisplayName, _registerAccountController.Model.DisplayName) == 0)
                    {
                        selectedAccount = account;
                    }
                }
            }

            if (this.Accounts.Count > 0)
            {
                _accountSelector.IsEnabled = true;

                if (selectedAccount != null)
                {
                    SelectedAccount = selectedAccount;
                }
            }
        }

        void _popupAccountCancel_Click(object sender, RoutedEventArgs e)
        {
            _useOtherAccount.IsChecked = false;
        }

        private void MandatoryFieldsReadinessChange(bool allCompleted)
        {
            _popupAccountOK.IsEnabled = allCompleted;
        }

        // Attempt to verify that the selected/added account is (a) valid and (b) signed up for CloudFormation.
        // This is awkward to handle outside the page.
        private void AccountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            AccountViewModel account = e.AddedItems[0] as AccountViewModel;
            if (!_verifiedAccounts.Contains(account.SettingsUniqueKey))
            {
                AccountValidationPending = true;

                BackgroundWorker bw = new BackgroundWorker();

                bw.DoWork += new DoWorkEventHandler(ValidateAccountWorker);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ValidateAccountWorkerCompleted);
                bw.RunWorkerAsync(new object[] { account, PageController.HostingWizard.Logger });
            }
            else
                NotifyPropertyChanged(uiProperty_Account);
        }

        void ValidateAccountWorker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            AccountViewModel account = args[0] as AccountViewModel;

            // temp bypass until we determine safe cloudformation or beanstalk call
            e.Result = new object[] { account, true };

            /* DEPLOYMENT_TODO
            ILog logger = args[1] as ILog;

            ListAvailableSolutionStacksResponse response = null;
            try
            {
                var beanstalkClient = DeploymentWizardHelper.GetBeanstalkClient(account);
                response = beanstalkClient.ListAvailableSolutionStacks(new ListAvailableSolutionStacksRequest());
            }
            catch (Exception exc)
            {
                logger.Error(GetType().FullName + ", exception in ValidateAccountWorker", exc);
            }
            finally
            {
                bool hasStacks = response != null && response.ListAvailableSolutionStacksResult.SolutionStacks.Count > 0;
                if (!hasStacks)
                    logger.InfoFormat("Did not find any solution stacks when validating account {0}, settings key {1}",
                                      account.AccountDisplayName,
                                      account.SettingsUniqueKey);

                e.Result = new object[] { account, hasStacks };
            }
          */

        }

        void ValidateAccountWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AccountValidationPending = false;

            object[] resultArr = e.Result as object[];
            AccountViewModel account = resultArr[0] as AccountViewModel;

            if ((bool)resultArr[1])
            {
                _verifiedAccounts.Add(account.SettingsUniqueKey);
            }
            else
            {
                /* DEPLOYMENT_TODO
                string marketingWebSite = "https://aws.amazon.com"; // fallback in case we can't find service site

                CloudFormationRootViewModel rootViewModel = account.FindSingleChild<CloudFormationRootViewModel>(false);
                if (rootViewModel != null)
                {
                    var serviceMeta = rootViewModel.MetaNode as ServiceRootViewMetaNode;
                    if (!string.IsNullOrEmpty(serviceMeta.MarketingWebSite))
                        marketingWebSite = serviceMeta.MarketingWebSite;
                }

                string msg = string.Format("The selected account is not valid or is not signed up for CloudFormation.{0}{0}To sign up, please visit {1}.",
                                           Environment.NewLine,
                                           marketingWebSite);
                ToolkitFactory.Instance.ShellProvider.ShowMessage("Account Validation", msg);
                */ 
            }

            NotifyPropertyChanged(uiProperty_Account);
        }

        private void _regionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Region);
        }

        void _templateSelector_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Template);
        }

        private void DeployModeChanged(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_DeployMode);
        }

        private void _existingDeployments_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _btnRedeploy.IsChecked = true; // nice touch...
            NotifyPropertyChanged(uiProperty_RedeployTarget);
        }
    }
}
