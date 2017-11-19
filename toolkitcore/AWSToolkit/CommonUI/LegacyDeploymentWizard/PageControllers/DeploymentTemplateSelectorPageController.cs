using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel;
using System.Xml.Linq;
using System.Threading;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Account;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Persistence.Deployment;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageWorkers;

using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers
{
    public class DeploymentTemplateSelectorPageController : IAWSWizardPageController
    {
        object _syncLock = new object();
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(DeploymentTemplateSelectorPageController));

        DeploymentTemplateSelectorPage _pageUI;
        XElement _templateManifest;

        AccountSignUpValidation _accountSignUpValidation = new AccountSignUpValidation();

        Dictionary<string, List<DeploymentTemplateWrapperBase>> _templatesByRegion = new Dictionary<string, List<DeploymentTemplateWrapperBase>>();

        // contains the running deployments (CloudFormation stacks or Beanstalk applications) discovered on change of account & region; 
        // any of these is a redeployment target (no actual relationship between project and the deployment service content)
        Dictionary<string, List<ExistingServiceDeployment>> _deploymentsByUserRegion = new Dictionary<string, List<ExistingServiceDeployment>>();

        // contains the static deployment history from the solution options file for a project; this is
        // used solely to preselect from any running stacks or beanstalks that the app that was last-deployed-to
        DeploymentHistories<CloudFormationDeploymentHistory> _cloudformationDeploymentHistories = null;
        DeploymentHistories<BeanstalkDeploymentHistory> _beanstalkDeploymentHistories = null;
        // and this indicates which of the two collections we should use, recovered from wizard seed data
        string _lastServiceDeployment = string.Empty;

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return "Template"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Select a template to deploy your application against."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new DeploymentTemplateSelectorPage(this);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(onPropertyChanged);

                _accountSignUpValidation.OnAccountValidationsCompleted += new EventHandler(AccountSignUpValidation_OnAccountValidationsCompleted);

                string templateManifest = S3FileFetcher.Instance.GetFileContent(DeploymentTemplateWrapperBase.TEMPLATEMANIFEST_FILE);
                if (!string.IsNullOrEmpty(templateManifest))
                    _templateManifest = XElement.Parse(templateManifest);
                else
                    LOGGER.Debug("Failed to download template manifest file; no templates will be shown.");
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI.RootViewModel == null) // first-time activation
            {
                AWSViewModel viewModel = HostingWizard[CommonWizardProperties.propkey_NavigatorRootViewModel] as AWSViewModel;
                _pageUI.RootViewModel = viewModel;

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_PreviousDeployments))
                {
                    if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_LastServiceDeployedTo))
                        _lastServiceDeployment = HostingWizard[DeploymentWizardProperties.SeedData.propkey_LastServiceDeployedTo] as string;
                    else
                        _lastServiceDeployment = string.Empty;

                    // unfortunately we can only pass as 'object' so unpick to individual service types for convenience later
                    Dictionary<string, object> deployments = HostingWizard[DeploymentWizardProperties.SeedData.propkey_PreviousDeployments] as Dictionary<string, object>;
                    if (deployments.ContainsKey(DeploymentServiceIdentifiers.CloudFormationServiceName))
                        _cloudformationDeploymentHistories = deployments[DeploymentServiceIdentifiers.CloudFormationServiceName] as DeploymentHistories<CloudFormationDeploymentHistory>;
                    if (deployments.ContainsKey(DeploymentServiceIdentifiers.BeanstalkServiceName))
                        _beanstalkDeploymentHistories = deployments[DeploymentServiceIdentifiers.BeanstalkServiceName] as DeploymentHistories<BeanstalkDeploymentHistory>;
                }

                AccountViewModel account = null;
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid))
                {
                    string accountGuidKey = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid] as string;
                    account = viewModel.AccountFromIdentityKey(accountGuidKey);
                }

                if (account == null) // go for the last-used (ie in-scope) toolkit account
                {
                    var lastAccountId = PersistenceManager.Instance.GetSetting(ToolkitSettingsConstants.LastAcountSelectedKey);
                    if (!string.IsNullOrEmpty(lastAccountId))
                        account = viewModel.AccountFromIdentityKey(lastAccountId);

                    // if we came from the account registration landing page, the wizard has 
                    // CommonWizardProperties.AccountSelection.propkey_SelectedAccount already set however
                    // this is the same instance as viewModel.RegisteredProfiles[0]
                    if (account == null && viewModel.RegisteredAccounts.Count > 0)
                        account = viewModel.RegisteredAccounts[0];
                }

                HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] = account;

                _pageUI.Initialize(account);

                _accountSignUpValidation.ValidateSignUps(viewModel.RegisteredAccounts, DeploymentServiceIdentifiers.BeanstalkServiceName);
                _accountSignUpValidation.ValidateSignUps(viewModel.RegisteredAccounts, DeploymentServiceIdentifiers.CloudFormationServiceName);

                string lastRegionDeployedTo = string.Empty;
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo))
                    lastRegionDeployedTo = HostingWizard[DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo] as string;

                if (!string.IsNullOrEmpty(lastRegionDeployedTo))
                    _pageUI.SelectedRegion = RegionEndPointsManager.Instance.GetRegion(lastRegionDeployedTo);
            }
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            bool allowNav = true;
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                // for a first-time deployment, make sure the selected account is signed up for the service that 'owns' the template
                if (_pageUI.RedeploySelected
                        || _accountSignUpValidation.IsAccountSignedUp(_pageUI.SelectedAccount, _pageUI.SelectedTemplate.ServiceOwner))
                    StorePageData();
                else
                {
                    RedirectToMarketingSiteSignUp(_pageUI.SelectedTemplate.ServiceOwner);
                    allowNav = false;
                }
            }

            return allowNav;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            bool pageComplete = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, pageComplete);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, pageComplete);
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        #endregion

        bool _deploymentsQueryPending = false;
        bool DeploymentsQueryPending
        {
            get
            {
                bool ret;
                lock (_syncLock)
                {
                    ret = _deploymentsQueryPending;
                }

                return ret;
            }

            set
            {
                lock (_syncLock)
                {
                    _deploymentsQueryPending = value;
                }
            }
        }

        void AccountSignUpValidation_OnAccountValidationsCompleted(object sender, EventArgs e)
        {
            _pageUI.Dispatcher.Invoke((Action)(() =>
            {
                TestForwardTransitionEnablement();
            }));
        }

        void RedirectToMarketingSiteSignUp(string serviceName)
        {
            string marketingWebSite = "http://aws.amazon.com"; // fallback in case we can't find service site
            string serviceDisplayName = serviceName;

            // we don't reference plugin assemblies, so have to walk and match on name as best we can
            AccountViewModel account = _pageUI.SelectedAccount;
            foreach (var serviceRoot in account.Children)
            {
                if (serviceRoot.MetaNode.EndPointSystemName == serviceName)
                {
                    serviceDisplayName = serviceRoot.Name;
                    marketingWebSite = (serviceRoot.MetaNode as ServiceRootViewMetaNode).MarketingWebSite;
                    break;
                }
            }

            // todo: convert this to a taskdialog....also need a beanstalk-specific message
            string msg;
            if (serviceName == DeploymentServiceIdentifiers.BeanstalkServiceName)
                msg = string.Format("The account details in the selected profile may not be valid or signed up for {0}, or no Windows environments are available for deployment.{1}{1}To sign up, please visit {2}.",
                                     serviceDisplayName,
                                     Environment.NewLine,
                                     marketingWebSite);
            else
                msg = string.Format("The account details in the selected profile are not valid or are not signed up for {0}.{1}{1}To sign up, please visit {2}.",
                                     serviceDisplayName,
                                     Environment.NewLine,
                                     marketingWebSite);

            ToolkitFactory.Instance.ShellProvider.ShowMessage("Service Sign-Up", msg);
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false;

                if (DeploymentsQueryPending || _accountSignUpValidation.IsAccountSignUpValidationPending)
                    return false;

                bool fwdsOK = _pageUI.SelectedRegion != null
                                    && _pageUI.SelectedAccount != null
                                    && _pageUI.IsSelectedAccountValid
                                    && (_pageUI.RedeploySelected
                                                ? !string.IsNullOrEmpty(_pageUI.SelectedRedeploymentName)
                                                : _pageUI.SelectedTemplate != null);

                return fwdsOK;
            }
        }

        void StorePageData()
        {
            HostingWizard.SetProperty(CommonWizardProperties.AccountSelection.propkey_SelectedAccount, _pageUI.SelectedAccount);
            HostingWizard.SetProperty(CommonWizardProperties.AccountSelection.propkey_SelectedRegion, _pageUI.SelectedRegion);

            if (_pageUI.RedeploySelected)
            {
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy, true);
                ExistingServiceDeployment deployment = _pageUI.SelectedRedeployment;
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName, deployment.DeploymentName);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance, deployment);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner, deployment.DeploymentService);
            }
            else
            {
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy, false);
                DeploymentTemplateWrapperBase template = _pageUI.SelectedTemplate;
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner, template.ServiceOwner);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate, template);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance, null);
            }
        }

        private void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Compare(e.PropertyName, DeploymentTemplateSelectorPage.uiProperty_Region, false) == 0)
                HandleRegionChange();
            else if (string.Compare(e.PropertyName, DeploymentTemplateSelectorPage.uiProperty_Account, false) == 0)
                HandleAccountChange();

            TestForwardTransitionEnablement();
        }

        void HandleAccountChange()
        {
            LoadPreviousDeployments(_pageUI.SelectedAccount, _pageUI.SelectedRegion);
        }

        void HandleRegionChange()
        {
            if (_templateManifest != null)
            {
                RegionEndPointsManager.RegionEndPoints rep = _pageUI.SelectedRegion;
                List<DeploymentTemplateWrapperBase> templateList = null;

                if (_templatesByRegion.ContainsKey(rep.SystemName))
                    templateList = _templatesByRegion[rep.SystemName];
                else
                {
                    templateList = new List<DeploymentTemplateWrapperBase>();
                    _templatesByRegion.Add(rep.SystemName, templateList);

                    // sure you can probably do this in one query but this will do for now
                    IEnumerable<XElement> regions
                            = from el in _templateManifest.Elements("region")
                              where (string)el.Attribute("systemname") == rep.SystemName
                              select el;
                    if (regions.Count<XElement>() > 0)
                    {
                        HashSet<string> availablePlugins = null;
                        if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_AvailableServiceOwners))
                            availablePlugins = HostingWizard[DeploymentWizardProperties.SeedData.propkey_AvailableServiceOwners] as HashSet<string>;

                        if (regions.Count<XElement>() > 1)
                            LOGGER.ErrorFormat("Found more than 1 region element satisfying attribute match on systemname for region '{0}', will use first returned node set", rep.SystemName);

                        IEnumerable<XElement> templates = from el in regions.ElementAt<XElement>(0).Elements("template") select el;
                        foreach (XElement el in templates)
                        {
                            var deploymentTemplate = ConvertToDeploymentTemplate(el);
                            if (deploymentTemplate.MinToolkitVersion == null || !VersionInfo.VersionManager.IsVersionGreaterThanToolkit(deploymentTemplate.MinToolkitVersion))
                            {
                                bool addTemplate = true;
                                if (availablePlugins != null && !availablePlugins.Contains(deploymentTemplate.ServiceOwner))
                                    addTemplate = false;

                                if (addTemplate)
                                    templateList.Add(deploymentTemplate);
                            }
                        }
                    }
                }

                _pageUI.Templates = templateList;

                LoadPreviousDeployments(_pageUI.SelectedAccount, rep);
            }
        }

        public static DeploymentTemplateWrapperBase ConvertToDeploymentTemplate(XElement template)
        {
            string serviceOwnerName = DeploymentServiceIdentifiers.CloudFormationServiceName;
            XAttribute serviceOwner = template.Attribute("serviceOwner");
            if (serviceOwner != null)
                serviceOwnerName = serviceOwner.Value;

            string header = template.Elements("header").ElementAt<XElement>(0).Value;
            string description = template.Elements("description").ElementAt<XElement>(0).Value;
            string file = template.Elements("templatefile").ElementAt<XElement>(0).Value;

            string minToolkitVersion = null;
            var element = template.Elements("min-toolkit-version");
            if (element != null && element.Count() > 0)
                minToolkitVersion = element.ElementAt<XElement>(0).Value;

            IEnumerable<string> supportedFrameworkVersions = null;
            element = template.Elements("frameworks");
            if (element != null && element.Count() > 0)
            {
                var versionsAttr = element.ElementAt<XElement>(0).Attribute("supportedVersions");
                if (versionsAttr != null)
                {
                    string fxVersions = versionsAttr.Value;
                    supportedFrameworkVersions = fxVersions.Split('|');
                }
            }

            var deploymentTemplate = DeploymentTemplateWrapperBase.FromToolkitFile(serviceOwnerName, header,
                description, file, minToolkitVersion, supportedFrameworkVersions);

            return deploymentTemplate;
        }

        void LoadPreviousDeployments(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            if (account == null)
                return;

            string cachedDeploymentsKey = GetCachedDeploymentsKey(account, region.SystemName);
            if (_deploymentsByUserRegion.ContainsKey(cachedDeploymentsKey))
            {
                string lastDeploymentName;

                GetLastDeployment(account, region.SystemName, _lastServiceDeployment, out lastDeploymentName);
                _pageUI.SetExistingDeployments(_deploymentsByUserRegion[cachedDeploymentsKey], _lastServiceDeployment, lastDeploymentName);
            }
            else
            {
                DeploymentsQueryPending = _pageUI.RunningDeploymentsQueryPending = true;

                var deploymentServices = ToolkitFactory.Instance.QueryPluginServiceImplementors<IAWSToolkitDeploymentService>();
                new QueryExistingDeploymentsWorker(account,
                                                   region.SystemName,
                                                   deploymentServices,
                                                   HostingWizard.Logger,
                                                   new QueryExistingDeploymentsWorker.DataAvailableCallback(OnExistingDeploymentsAvailable));
            }

            TestForwardTransitionEnablement();
        }

        string GetCachedDeploymentsKey(AccountViewModel account, string regionName)
        {
            return string.Format("Account:{0}_Region:{1}", account.SettingsUniqueKey, regionName);
        }

        void OnExistingDeploymentsAvailable(AccountViewModel forAccount, string forRegion, List<ExistingServiceDeployment> deployments)
        {
            string lastDeploymentName;
            GetLastDeployment(forAccount, forRegion, _lastServiceDeployment, out lastDeploymentName);
            _deploymentsByUserRegion.Add(GetCachedDeploymentsKey(forAccount, forRegion), deployments);

            _pageUI.SetExistingDeployments(deployments, _lastServiceDeployment, lastDeploymentName);

            _pageUI.RunningDeploymentsQueryPending = DeploymentsQueryPending = false;
            TestForwardTransitionEnablement();
        }

        void GetLastDeployment(AccountViewModel forAccount, string forRegion, string lastService, out string lastName)
        {
            lastName = string.Empty;
            if (string.IsNullOrEmpty(lastService))
                return;

            if (string.Compare(lastService, DeploymentServiceIdentifiers.CloudFormationServiceName, true) == 0)
            {
                if (_cloudformationDeploymentHistories != null)
                {
                    Dictionary<string, CloudFormationDeploymentHistory> deployments = _cloudformationDeploymentHistories.DeploymentsForAccount(forAccount.SettingsUniqueKey);
                    if (deployments.ContainsKey(forRegion))
                        lastName = deployments[forRegion].LastStack;
                }

                return;
            }

            if (string.Compare(lastService, DeploymentServiceIdentifiers.BeanstalkServiceName, true) == 0)
            {
                if (_beanstalkDeploymentHistories != null)
                {
                    Dictionary<string, BeanstalkDeploymentHistory> deployments = _beanstalkDeploymentHistories.DeploymentsForAccount(forAccount.SettingsUniqueKey);
                    if (deployments.ContainsKey(forRegion))
                        lastName = deployments[forRegion].ApplicationName;
                }

                return;
            }
        }
    }

    /// <summary>
    /// Helper class used to verify one or more accounts are signed up for the
    /// various deployment services. When the user selects and account, region
    /// and deployment service template, we can then prompt the user to go sign-up
    /// if necessary before the wizard can proceed.
    /// </summary>
    class AccountSignUpValidation
    {
        object _syncLock = new Object();
        Dictionary<string, HashSet<string>> _validatedAccountsAndServices = new Dictionary<string, HashSet<string>>();
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AccountSignUpValidation));

        int _accountSignUpValidationsPending = 0;

        public event EventHandler OnAccountValidationsCompleted;

        public bool IsAccountSignUpValidationPending
        {
            get 
            {
                int pendingQueries;
                lock (_syncLock)
                {
                    pendingQueries = _accountSignUpValidationsPending;
                }
                return pendingQueries > 0; 
            }
        }

        public bool IsAccountSignedUp(AccountViewModel account, string serviceName)
        {
            bool signedUp = false;
            lock (_syncLock)
            {
                if (_validatedAccountsAndServices.ContainsKey(account.SettingsUniqueKey))
                {
                    HashSet<string> services = _validatedAccountsAndServices[account.SettingsUniqueKey];
                    signedUp = services.Contains(serviceName);
                }
            }

            return signedUp;
        }

        /// <summary>
        /// Launches background worker to test and record one or more accounts are signed up
        /// for the given service
        /// </summary>
        /// <param name="accounts"></param>
        /// <param name="serviceName"></param>
        public void ValidateSignUps(IEnumerable<AccountViewModel> accounts, string serviceName)
        {
            if (serviceName == DeploymentServiceIdentifiers.BeanstalkServiceName)
            {
                Interlocked.Increment(ref _accountSignUpValidationsPending);
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(ValidateBeanstalkAccountSignUpWorker);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ValidateAccountSignUpWorkerCompleted);
                bw.RunWorkerAsync(new object[]
                {
                    accounts,
                    // may arrive on page in different region, so force us-east for this
                    RegionEndPointsManager.Instance.GetRegion("us-east-1")
                });

                return;
            }

            if (serviceName == DeploymentServiceIdentifiers.CloudFormationServiceName)
            {
                Interlocked.Increment(ref _accountSignUpValidationsPending);
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(ValidateCloudFormationAccountSignUpWorker);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(ValidateAccountSignUpWorkerCompleted);
                bw.RunWorkerAsync(new object[]
                {
                    accounts,
                    // may arrive on page in different region, so force us-east for this
                    RegionEndPointsManager.Instance.GetRegion("us-east-1")
                });
                return;
            }
        }

        void ValidateBeanstalkAccountSignUpWorker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            IEnumerable<AccountViewModel> accounts = args[0] as IEnumerable<AccountViewModel>;
            RegionEndPointsManager.RegionEndPoints region = args[1] as RegionEndPointsManager.RegionEndPoints;

            int numAccounts = accounts.Count<AccountViewModel>();
            bool[] isSignedUp = new bool[numAccounts];
            for (int i = 0; i < numAccounts; i++)
            {
                isSignedUp[i] = false;
                AccountViewModel account = accounts.ElementAt<AccountViewModel>(i);
                ListAvailableSolutionStacksResponse response = null;
                try
                {
                    var beanstalkClient = GetBeanstalkClient(account, region);
                    response = beanstalkClient.ListAvailableSolutionStacks(new ListAvailableSolutionStacksRequest());
                }
                catch (Exception exc)
                {
                    LOGGER.Error(GetType().FullName + ", exception in ValidateBeanstalkAccountSignUpWorker", exc);
                }
                finally
                {
                    // UI for stacks filters to Windows only by name (in the absence of any metadata about the stack)
                    // so we should do the same - no point letting the user in if there are no stacks!
                    if (response != null && response.SolutionStacks.Count > 0)
                    {
                        foreach (string stack in response.SolutionStacks)
                        {
                            if (stack.Contains(" Windows "))
                            {
                                isSignedUp[i] = true;
                                break;
                            }
                        }
                    }
                }
            }

            e.Result = new object[] { accounts, DeploymentServiceIdentifiers.BeanstalkServiceName, isSignedUp };
        }

        void ValidateCloudFormationAccountSignUpWorker(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            IEnumerable<AccountViewModel> accounts = args[0] as IEnumerable<AccountViewModel>;
            RegionEndPointsManager.RegionEndPoints region = args[1] as RegionEndPointsManager.RegionEndPoints;

            int numAccounts = accounts.Count<AccountViewModel>();
            bool[] isSignedUp = new bool[numAccounts];
            for (int i = 0; i < numAccounts; i++)
            {
                // need to think of a test
                isSignedUp[i] = true;
            }

            e.Result = new object[] { accounts, DeploymentServiceIdentifiers.CloudFormationServiceName, isSignedUp };
        }

        /// <summary>
        /// All deployment service account verifications arrive here; we use service name in
        /// result args to redirect to relevant marketing site if needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ValidateAccountSignUpWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            object[] resultArr = e.Result as object[];
            IEnumerable<AccountViewModel> accounts = resultArr[0] as IEnumerable<AccountViewModel>;
            string serviceName = resultArr[1] as string;
            bool[] isSignedUp = resultArr[2] as bool[];

            int numAccounts = accounts.Count<AccountViewModel>();
            for (int i = 0; i < numAccounts; i++)
            {
                if (isSignedUp[i])
                {
                    AccountViewModel account = accounts.ElementAt<AccountViewModel>(i);
                    lock (_syncLock)
                    {
                        if (_validatedAccountsAndServices.ContainsKey(account.SettingsUniqueKey))
                        {
                            HashSet<string> services = _validatedAccountsAndServices[account.SettingsUniqueKey];
                            if (!services.Contains(serviceName))
                                services.Add(serviceName);
                        }
                        else
                        {
                            HashSet<string> services = new HashSet<string>(new string[] { serviceName });
                            _validatedAccountsAndServices.Add(account.SettingsUniqueKey, services);
                        }
                    }
                }
                else
                {
                    AccountViewModel account = accounts.ElementAt<AccountViewModel>(i);
                }
            }
            Interlocked.Decrement(ref _accountSignUpValidationsPending);

            if (!IsAccountSignUpValidationPending && OnAccountValidationsCompleted != null)
                OnAccountValidationsCompleted(this, EventArgs.Empty);
        }

        IAmazonElasticBeanstalk GetBeanstalkClient(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            var endpoint = region.GetEndpoint(RegionEndPointsManager.ELASTICBEANSTALK_SERVICE_NAME);
            var config = new AmazonElasticBeanstalkConfig {ServiceURL = endpoint.Url, AuthenticationRegion = endpoint.AuthRegion};
            return new AmazonElasticBeanstalkClient(account.Credentials, config);
        }

        IAmazonCloudFormation GetCloudFormationClient(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            var config = new AmazonCloudFormationConfig {ServiceURL = region.GetEndpoint(RegionEndPointsManager.CLOUDFORMATION_SERVICE_NAME).Url};
            return new AmazonCloudFormationClient(account.Credentials, config);
        }
    }
}
