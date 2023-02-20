using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Xml.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Util;
using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    public class StartPageController : IAWSWizardPageController
    {
        protected StartPage _pageUI;
        private bool _pageUiActivated = false;

        readonly object _syncLock = new object();
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(StartPageController));

        private readonly ToolkitContext _toolkitContext;

        private const int AccountRegionChangedDebounceMs = 250;

        private readonly DebounceDispatcher _accountRegionChangeDebounceDispatcher = new DebounceDispatcher();

        readonly Dictionary<string, List<DeploymentTemplateWrapperBase>> _templatesByRegion 
            = new Dictionary<string, List<DeploymentTemplateWrapperBase>>();

        int _workersActive = 0;
        public bool WorkersActive
        {
            get
            {
                bool ret;
                lock (_syncLock)
                    ret = _workersActive > 0;
                return ret;
            }
        }

        // Tracks the worker output by account and region as the user updates the account/region controls,
        // to save requerying. We puwh this to the wizard properties to save some workers being needed
        // on subsequent pages.
        readonly Dictionary<string, ICollection<DeployedApplicationModel>> _deploymentsByUserRegion = new Dictionary<string, ICollection<DeployedApplicationModel>>();

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard
        {
            get; 
            set;
        }

        public string PageGroup => DeploymentWizardPageGroups.AppTargetGroup;

        public string PageTitle => "Publish to AWS Elastic Beanstalk";

        public string ShortPageTitle => null;

        public string PageDescription => "Publish can create a new application/environment or redeploy to an existing environment.";

        public StartPageController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
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
                _pageUI = new StartPage(_toolkitContext, HostingWizard);
                _pageUI.PropertyChanged += OnPagePropertyChanged;

                string templateManifest = S3FileFetcher.Instance.GetFileContent(DeploymentTemplateWrapperBase.TEMPLATEMANIFEST_FILE);
                if (!string.IsNullOrEmpty(templateManifest))
                {
                    ParseTemplatesFromManifest(XElement.Parse(templateManifest));
                }
                else
                    LOGGER.Debug("Failed to download template manifest file; no templates will be shown.");
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (!_pageUiActivated) // first-time activation
            {
                var viewModel = HostingWizard[CommonWizardProperties.propkey_NavigatorRootViewModel] as AWSViewModel;

                AccountViewModel account = null;
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid))
                {
                    var accountGuidKey = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid] as string;
                    account = viewModel.AccountFromIdentityKey(accountGuidKey);
                }

                if (account == null) // go for the last-used (ie in-scope) toolkit account
                {
                    var lastSelectedCredentialId = ToolkitSettings.Instance.LastSelectedCredentialId;
                    if (!string.IsNullOrEmpty(lastSelectedCredentialId))
                    {
                       account = viewModel.AccountFromCredentialId(lastSelectedCredentialId);
                    }

                    // if we came from the account registration landing page, the wizard has 
                    // CommonWizardProperties.AccountSelection.propkey_SelectedAccount already set however
                    // this is the same instance as viewModel.RegisteredProfiles[0]
                    if (account == null && viewModel.RegisteredAccounts.Count > 0)
                        account = viewModel.RegisteredAccounts[0];
                }

                HostingWizard.SetSelectedAccount(account);

                _pageUI.Connection.Account = account;
                _pageUI.Connection.Region =
                    _toolkitContext.RegionProvider.GetRegion(ToolkitSettings.Instance.LastSelectedRegion) ??
                    _toolkitContext.RegionProvider.GetRegion(RegionEndpoint.USEast1.SystemName);


                HostingWizard.SetSelectedRegion(_pageUI.Connection.Region);

                //_accountSignUpValidation.ValidateSignUps(viewModel.RegisteredAccounts, DeploymentServiceIdentifiers.BeanstalkServiceName);


                //TODO: Revisit the last deployed region logic. This sets the connection Region to null because no regions have been loaded yet

                string lastRegionDeployedTo = string.Empty;
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo))
                {
                    lastRegionDeployedTo =
                        HostingWizard[DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo] as string;
                }

                if (!string.IsNullOrEmpty(lastRegionDeployedTo))
                {
                    _pageUI.SelectedRegionId = lastRegionDeployedTo;
                }

                _pageUiActivated = true;
            }

            // since our default is to deploy a new app environment, we can enable forward nav immediately
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, true);
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            StorePageData();
            return true;
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
            return false;
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                {
                    return false;
                }

                if (WorkersActive)
                {
                    return false;
                }

                if (!_pageUI.Connection.ConnectionIsValid || _pageUI.Connection.IsValidating)
                {
                    return false;
                }

                var fwdsOK = _pageUI.SelectedRegion != null && _pageUI.SelectedAccount != null;

                if (fwdsOK && _pageUI.RedeploySelected)
                {
                    var deployment = _pageUI.SelectedDeployment;
                    fwdsOK = deployment != null && !string.IsNullOrEmpty(deployment.SelectedEnvironmentName);

                    if (fwdsOK)
                    {
                        fwdsOK = _pageUI.SelectedDeployment.Environments
                            .Where(e => e.EnvironmentName.Equals(deployment.SelectedEnvironmentName,
                                StringComparison.Ordinal)).Any(e => e.Status == BeanstalkConstants.STATUS_READY);
                    }
                }

                return fwdsOK;
            }
        }

        void StorePageData()
        {
            HostingWizard.SetSelectedAccount(_pageUI.SelectedAccount);
            HostingWizard.SetSelectedRegion(_pageUI.SelectedRegion);

            var regionHasXray = _toolkitContext.RegionProvider.IsServiceAvailable(ServiceNames.Xray, _pageUI.SelectedRegionId);
            HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_XRayAvailable, regionHasXray);

            // this wizard is locked to Beanstalk deployments for now
            HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner, DeploymentServiceIdentifiers.BeanstalkServiceName);

            // we can simply pass this through if set
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel))
            {
                var seedVersion = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel] as string;
                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel, seedVersion);
            }

            // pass the collection of existing deployments for the selected user account/region to the downstream pages,
            // to avoid the need to requery
            var cachedDeploymentsKey = ConstructCachedDeploymentsKey(_pageUI.SelectedAccount, _pageUI.SelectedRegionId);
            HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_ExistingAppDeploymentsInRegion,
                                        _deploymentsByUserRegion.ContainsKey(cachedDeploymentsKey)
                                            ? _deploymentsByUserRegion[cachedDeploymentsKey]
                                            : new List<DeployedApplicationModel>());

            // record if the selected account/region is restricted to vpc usage only, for downstream pages to act upon (as all new
            // accounts and regions are vpc only, we'll assume so if the query fails)
            var vpcOnly = true;
            var ec2PluginService = GetEc2PluginService();
            if (ec2PluginService != null)
                vpcOnly = ec2PluginService.IsVpcOnly(_pageUI.SelectedAccount, _pageUI.SelectedRegion);
            HostingWizard.SetProperty(DeploymentWizardProperties.SeedData.propkey_VpcOnlyMode, vpcOnly);

            SetDeploymentToolOnWizard();

            if (IsRedeploy())
            {
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy, true);
                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv] = false;
                var selectedDeployment = GetSelectedDeployment();
                var selectedEnvironment = selectedDeployment.Environments.FirstOrDefault(env => env.EnvironmentName == selectedDeployment.SelectedEnvironmentName);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName, selectedDeployment.ApplicationName);
                HostingWizard[BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack] =
                    selectedEnvironment?.SolutionStackName;

                // reform the new model into that used by the legacy wizard; some duplication here
                var existingDeployment = new ExistingServiceDeployment
                {
                    DeploymentName = selectedDeployment.ApplicationName,
                    DeploymentService = DeploymentServiceIdentifiers.BeanstalkServiceName,
                };
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance, existingDeployment);
                HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] = selectedDeployment.SelectedEnvironmentName;

                // If redeploying to a non windows solution stack then use Amazon.ElasticBeanstalk.Tools to handle deployment. Both propKey_IsLinuxSolutionStack and propKey_UseEbToolsToDeploy are used
                // for now with the same value but in the future when we use EbTools for Windows .NET Core deployment they can differ.
                HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_IsLinuxSolutionStack] = !selectedDeployment.IsSelectedEnvironmentWindowsSolutionStack;

                // if a build configuration was recorded on last deployment for the environment, make it the default for the redeployment
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_PreviousDeployments))
                {
                    var bdh = DeploymentWizardHelper.DeploymentHistoryForAccountAndRegion(_pageUI.SelectedAccount.AccountViewModel, 
                                                                                          _pageUI.SelectedRegionId, 
                                                                                          HostingWizard.CollectedProperties);
                    if (bdh != null
                            && bdh.ApplicationName.Equals(selectedDeployment.ApplicationName, StringComparison.Ordinal)
                            && bdh.EnvironmentName.Equals(selectedDeployment.SelectedEnvironmentName, StringComparison.Ordinal)
                            && !string.IsNullOrEmpty(bdh.BuildConfiguration))
                        HostingWizard[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] = bdh.BuildConfiguration;
                }
            }
            else
            {
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy, false);
                foreach (var t in GetTemplatesForSelectedRegion())
                {
                    if (t.ServiceOwner.Equals(DeploymentServiceIdentifiers.BeanstalkServiceName, StringComparison.Ordinal))
                    {
                        HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate, t);
                        break;
                    }
                }
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance, null);
            }

            // if the user project contained a DeployIisAppPath setting, use that to seed the iis app path ready
            // for the app options page otherwise form up our default of 'Default Web Site/'
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_DeployIisAppPath))
            {
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] =
                    HostingWizard[DeploymentWizardProperties.SeedData.propkey_DeployIisAppPath];
            }
            else
            {
                // note change of default behavior here - we no longer use appname_deploy
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] 
                    = AWSDeployment.CommonParameters.DefaultIisAppPathFormat;
            }
        }

        protected virtual IAWSEC2 GetEc2PluginService()
        {
            return ToolkitFactory.Instance.QueryPluginService(typeof(IAWSEC2)) as IAWSEC2;
        }

        protected virtual bool IsRedeploy() => _pageUI.RedeploySelected;

        protected virtual DeployedApplicationModel GetSelectedDeployment() => _pageUI.SelectedDeployment;

        protected virtual List<DeploymentTemplateWrapperBase> GetTemplatesForSelectedRegion()
        {
            return _templatesByRegion[_pageUI.SelectedRegionId];
        }

        private void SetDeploymentToolOnWizard()
        {
            HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_UseEbToolsToDeploy] =
                Project.IsNetCoreWebProject(HostingWizard);
        }

        private void OnPagePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(StartPage.Connection), StringComparison.Ordinal))
            {
                if (_pageUI.Connection.ConnectionIsValid)
                {
                    _accountRegionChangeDebounceDispatcher.Debounce(AccountRegionChangedDebounceMs, _ =>
                    {
                        ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread(() =>
                        {
                            LoadDeploymentsForAccountAndRegion(_pageUI.SelectedAccount, _pageUI.SelectedRegion);
                        });
                    });
                }
            }

            TestForwardTransitionEnablement();
        }

        void LoadDeploymentsForAccountAndRegion(AccountViewModel account, ToolkitRegion region)
        {
            if (account == null || region == null)
                return;

            var cachedDeploymentsKey = ConstructCachedDeploymentsKey(account, region.Id);
            if (_deploymentsByUserRegion.ContainsKey(cachedDeploymentsKey))
            {
                _pageUI.LoadAvailableDeployments(_deploymentsByUserRegion[cachedDeploymentsKey]);
            }
            else
            {
                Interlocked.Increment(ref _workersActive);

                new QueryExistingDeploymentsWorker(account, 
                                                   region, 
                                                   HostingWizard.Logger,
                                                   OnExistingDeploymentsWorkerCompleted);
            }

            TestForwardTransitionEnablement();
        }

        string ConstructCachedDeploymentsKey(AccountViewModel account, string regionName)
        {
            return string.Format("Account:{0}_Region:{1}", account.SettingsUniqueKey, regionName);
        }

        void ParseTemplatesFromManifest(XElement templateManifest)
        {
            var knownRegions = _toolkitContext.RegionProvider.GetPartitions()
                .SelectMany(p => _toolkitContext.RegionProvider.GetRegions(p.Id))
                .ToList();

            foreach (var region in knownRegions)
            {
                var templateList = new List<DeploymentTemplateWrapperBase>();
                _templatesByRegion.Add(region.Id, templateList);

                // sure you can probably do this in one query but this will do for now
                var regions = from el in templateManifest.Elements("region")
                                   where (string) el.Attribute("systemname") == region.Id
                                   select el;
                if (regions.Any())
                {
                    HashSet<string> availablePlugins = null;
                    if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_AvailableServiceOwners))
                        availablePlugins = HostingWizard[DeploymentWizardProperties.SeedData.propkey_AvailableServiceOwners] as
                                HashSet<string>;

                    if (regions.Count() > 1)
                        LOGGER.ErrorFormat(
                            "Found more than 1 region element satisfying attribute match on systemname for region '{0}', will use first returned node set",
                            region.Id);

                    var templates = from el in regions.ElementAt(0).Elements("template")
                        select el;
                    foreach (var el in templates)
                    {
                        var deploymentTemplate = DeploymentTemplateSelectorPageController.ConvertToDeploymentTemplate(el);
                        if (deploymentTemplate.MinToolkitVersion == null 
                                || !VersionInfo.VersionManager.IsVersionGreaterThanToolkit(deploymentTemplate.MinToolkitVersion))
                        {
                            var addTemplate = (availablePlugins != null &&
                                               !availablePlugins.Contains(deploymentTemplate.ServiceOwner));

                            if (addTemplate)
                                templateList.Add(deploymentTemplate);
                        }
                    }
                }
            }
        }

        void OnExistingDeploymentsWorkerCompleted(AccountViewModel forAccount, 
                                                  string forRegion, 
                                                  ICollection<DeployedApplicationModel> deployments)
        {
            Interlocked.Decrement(ref _workersActive);

            var key = ConstructCachedDeploymentsKey(forAccount, forRegion);
            // check for key existence as we may have overlapping responses
            // from interaction between the account and region fields
            if (!_deploymentsByUserRegion.ContainsKey(key))
                _deploymentsByUserRegion.Add(key, deployments);

            _pageUI.LoadAvailableDeployments(deployments);

            TestForwardTransitionEnablement();
        }
    }
}
