using System.Collections.Generic;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Persistence.Deployment;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.LegacyDeployment
{
    internal class ApplicationPageController : IAWSWizardPageController
    {
        ApplicationPage _pageUI = null;

        #region IAWSWizardPageController Members

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Application";

        public string ShortPageTitle => null;

        public string PageDescription => "Select whether to deploy a new application or update an existing one.";

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return IsWizardInBeanstalkMode;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            return _pageUI ?? (_pageUI = new ApplicationPage(this));
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // Note that a given VS project can be deployed to any Beanstalk app; there is no correlation between the
            // two!
            string appName;
            string appDescription = string.Empty;
            string versionLabel;

            bool? useIncrementalDeployment = null;
            string incrementalDeploymentLocn = null;

            RegionEndPointsManager.RegionEndPoints region
                    = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] 
                            as RegionEndPointsManager.RegionEndPoints;
            _pageUI.SelectedRegion = region;

            // Only want to use seed data on first page activation, thereafter use what we stored from previous run.
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName))
                appName = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
            else
                appName = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedName] as string;
            
            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel))    
                versionLabel = HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] as string;
            else
                versionLabel = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel] as string;
            
            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_AppDescription))
                appDescription = HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_AppDescription] as string;
            
            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment))
                useIncrementalDeployment = (bool)HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment];

            if (IsRedeploying)
            {
                // see if we have an incremental location we should re-use, to get the benefits - note that where the project went (app name)
                // is irrelevant, just where it was deployed from
                AccountViewModel selectedAccount 
                    = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                BeanstalkDeploymentHistory bdh 
                    = DeploymentHistoryForAccountAndRegion(selectedAccount, region);
                if (bdh != null)
                {
                    useIncrementalDeployment = bdh.IsIncrementalDeployment;
                    incrementalDeploymentLocn = bdh.IncrementalPushRepositoryLocation;
                }
            }

            // if location is null/empty, we'll calculate a new predictable location on page transition
            HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalPushRepositoryLocation] = incrementalDeploymentLocn;

            if (useIncrementalDeployment == null)
                useIncrementalDeployment = false; // we default to incremental push :-)
            _pageUI.Initialize(appName, appDescription, versionLabel, IsRedeploying, useIncrementalDeployment);

            TestForwardTransitionEnablement();
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
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
        }

        public bool AllowShortCircuit()
        {
            // user may have gone forwards enough for Finish to be enabled, then come back
            // and changed something so re-save
            StorePageData();
            return true;
        }

        #endregion

        bool IsWizardInBeanstalkMode
        {
            get
            {
                string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.BeanstalkServiceName;
            }
        }

        bool IsRedeploying
        {
            get
            {
                bool redeployment = false;
                if (HostingWizard != null && HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                    redeployment = (bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];
                return redeployment;
            }
        }

        void StorePageData()
        {
            if (!IsWizardInBeanstalkMode)
                return;

            if (!IsRedeploying)
            {
                HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] = _pageUI.AppName.Trim();
                HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_AppDescription] = _pageUI.AppDescription;
            }

            HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment] = _pageUI.UseIncrementalDeployment;
            // persist version label even if not used (at this stage) - may be seeded
            HostingWizard[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] = _pageUI.DeploymentVersionLabel.Trim();

            // always set a default, has no harm and if we ever expose this to the user, would allow them to toggle incremental
            // mode without losing the path in between times
            if (!HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalPushRepositoryLocation))
            {
                string projectGuid = HostingWizard[DeploymentWizardProperties.SeedData.propkey_VSProjectGuid] as string;
                // where the project content is going is irrelevant, as one vs project can be deployed to > 1 beanstalk app and environment;
                // where we deployed from needs to be predictable if incremental deployment is to work
                string incrementalReposLocation = DeploymentWizardHelper.ComputeDeploymentArtifactFolder(projectGuid);
                HostingWizard[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalPushRepositoryLocation] = incrementalReposLocation;
            }
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (!IsWizardInBeanstalkMode)
                    return true;

                bool nextEnabled = false;
                // these fields are mandatory for either app create or update. Data binding timing issues mean we must
                // consider wizard property or control selection for account.
                if (_pageUI != null && !_pageUI.VersionFetchPending)
                {
                    if (!IsRedeploying)
                    {
                        nextEnabled = !string.IsNullOrEmpty(this._pageUI.AppName);
                        if (nextEnabled)
                        {
                            if (!_pageUI.UseIncrementalDeployment)
                                nextEnabled = !string.IsNullOrEmpty(this._pageUI.DeploymentVersionLabel);
                        }
                    }
                    else
                    {
                        if (_pageUI.UseIncrementalDeployment)
                            nextEnabled = true;
                        else
                            nextEnabled = _pageUI.IsSelectedVersionValid;
                    }
                }

                return nextEnabled;
            }
        }

        /// <summary>
        /// Inspects prior deployments and extracts details matching the selected account and region, if any.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        BeanstalkDeploymentHistory DeploymentHistoryForAccountAndRegion(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            Dictionary<string, object> allPreviousDeployments
                    = HostingWizard[DeploymentWizardProperties.SeedData.propkey_PreviousDeployments] as Dictionary<string, object>;
            if (allPreviousDeployments != null && allPreviousDeployments.ContainsKey(DeploymentServiceIdentifiers.BeanstalkServiceName))
            {
                DeploymentHistories<BeanstalkDeploymentHistory> beanstalkDeployments
                        = allPreviousDeployments[DeploymentServiceIdentifiers.BeanstalkServiceName] 
                                as DeploymentHistories<BeanstalkDeploymentHistory>;
                // deployments within service organised by accountid: <region, T>
                Dictionary<string, BeanstalkDeploymentHistory> accountDeployments = beanstalkDeployments.DeploymentsForAccount(account.SettingsUniqueKey);
                if (accountDeployments != null && accountDeployments.ContainsKey(region.SystemName))
                {
                    return accountDeployments[region.SystemName];
                }
            }

            return null;
        }
    }
}
