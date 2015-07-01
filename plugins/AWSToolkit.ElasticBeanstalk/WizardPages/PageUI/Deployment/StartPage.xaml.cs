using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for StartPage.xaml
    /// </summary>
    public partial class StartPage : INotifyPropertyChanged
    {
        // property names used with NotifyPropertyChanged
        public static readonly string uiProperty_Region = "Region";
        public static readonly string uiProperty_Account = "Account";
        public static readonly string uiProperty_DeploymentMode = "DeploymentMode"; // deploy new vs redeploy
        public static readonly string uiProperty_ExistingDeployments = "ExistingDeployments";
        public static readonly string uiProperty_SelectedDeployment = "SelectedDeployment";

        RegisterAccountController _registerAccountController;

        public StartPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public StartPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void Initialize(AccountViewModel account)
        {
            if (account != null)
                this._accountSelector.SelectedItem = account;

            var regions = RegionEndPointsManager.Instance.Regions
                            .Where(rep => rep.GetEndpoint(DeploymentServiceIdentifiers.BeanstalkServiceName) != null)
                            .ToList();

            this._regionSelector.ItemsSource = regions;
            if (this._regionSelector.Items.Count != 0)
            {
                var region = RegionEndPointsManager.Instance.GetDefaultRegionEndPoints();
                this._regionSelector.SelectedItem = region;
            }
        }

        AWSViewModel _rootViewModel;
        public AWSViewModel RootViewModel
        {
            get
            {
                return IsInitialized ? _rootViewModel : null;
            }
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
                if (RootViewModel == null)
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
            get { return this._accountSelector.SelectedItem as AccountViewModel; }
            protected set { if (IsInitialized) this._accountSelector.SelectedItem = value; }
        }

        public RegionEndPointsManager.RegionEndPoints SelectedRegion
        {
            get
            {
                return _regionSelector.SelectedItem as RegionEndPointsManager.RegionEndPoints;
            }
            set
            {
                _regionSelector.SelectedItem = value;
            }
        }

        private ObservableCollection<DeployedApplicationModel> _existingDeployments;

        public ObservableCollection<DeployedApplicationModel> ExistingDeployments
        {
            get { return _existingDeployments; }
        }

        public void LoadAvailableDeployments(ICollection<DeployedApplicationModel> deployments)
        {
            _existingDeployments = new ObservableCollection<DeployedApplicationModel>(deployments);
            NotifyPropertyChanged(uiProperty_ExistingDeployments);
            _btnRedeploy.IsEnabled = deployments != null && deployments.Count > 0;
        }

        public bool RedeploySelected
        {
            get
            {
                return _btnRedeploy.IsChecked == true;
            }
        }

        // returns the selected environment to redeploy to - if the user has selected
        // an application (root) tree item, null is returned, allowing us to disable
        // forward navigation until an env is selected
        public DeployedApplicationModel SelectedDeployment
        {
            get
            {
                if (_existingDeploymentsTree == null || _existingDeploymentsTree.SelectedItem == null)
                    return null;

                var deployment = _existingDeploymentsTree.SelectedItem as DeployedApplicationModel;
                if (deployment == null)
                {
                    var environment = _existingDeploymentsTree.SelectedItem as EnvironmentDescription;
                    deployment = FindMatchingDeploymentModel(environment.ApplicationName);
                }
                return deployment;
            }
        }

        public bool LockToNewWizard
        {
            set { _btnUseLegacyWizard.Visibility = value ? Visibility.Hidden : Visibility.Visible; }
        }

        void AccountEntryPopup_Loaded(object sender, RoutedEventArgs e)
        {
            if (_registerAccountController == null)
            {
                _registerAccountController = new RegisterAccountController();
                var control = new RegisterAccountControl(_registerAccountController);
                control.SetMandatoryFieldsReadyCallback(MandatoryFieldsReadinessChange);
                _accountFieldContainer.Content = control;
                _popupAccountOK.IsEnabled = false;
            }
        }

        void MandatoryFieldsReadinessChange(bool allCompleted)
        {
            _popupAccountOK.IsEnabled = allCompleted;
        }

        void PopupAccountOK_Click(object sender, RoutedEventArgs e)
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

                    if (string.Compare(account.AccountDisplayName, _registerAccountController.Model.DisplayName, StringComparison.CurrentCulture) == 0)
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

        void PopupAccountCancel_Click(object sender, RoutedEventArgs e)
        {
            _useOtherAccount.IsChecked = false;
        }

        private void AccountSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
                return;

            NotifyPropertyChanged(uiProperty_Account);
        }

        void RegionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_Region);
        }

        private void UseLegacyWizard_OnClick(object sender, RoutedEventArgs e)
        {
            PageController.HostingWizard.CancelRun(DeploymentWizardProperties.SeedData.propkey_LegacyDeploymentMode, true);    
        }

        // this is used to wire an environment selection into the overall DeployedApplicationModel
        // attached to a treeitem root
        private void _existingDeploymentsTree_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var environment = e.NewValue as EnvironmentDescription;
            if (!this._btnRedeploy.IsChecked.GetValueOrDefault())
                this._btnRedeploy.IsChecked = true;

            if (environment != null)
            {
                var deployment = FindMatchingDeploymentModel(environment.ApplicationName);
                deployment.SelectedEnvironmentName = environment.EnvironmentName;
            }
            else
            {
                // if the user has selected an app with more than one environment, clear out selected env
                var deployment = e.NewValue as DeployedApplicationModel;
                if (deployment != null && deployment.Environments.Count > 1)
                    deployment.SelectedEnvironmentName = null;
            }

            NotifyPropertyChanged(uiProperty_SelectedDeployment);
        }

        private DeployedApplicationModel FindMatchingDeploymentModel(string applicationName)
        {
            return _existingDeployments.FirstOrDefault(d => d.ApplicationName.Equals(applicationName, StringComparison.Ordinal));
        }

        private void DeploymentModeButton_OnChecked(object sender, RoutedEventArgs e)
        {
            // if the user is toggling between create-new and redeploy, clean out any
            // selected environment otherwise when they come back to redeploy, the Next
            // button stays enabled but there is no selected environment (or the one
            // that is is out of date and not reflected in the UI)
            if (SelectedDeployment != null)
                SelectedDeployment.SelectedEnvironmentName = null;

            NotifyPropertyChanged(uiProperty_DeploymentMode);
        }
    }
}
