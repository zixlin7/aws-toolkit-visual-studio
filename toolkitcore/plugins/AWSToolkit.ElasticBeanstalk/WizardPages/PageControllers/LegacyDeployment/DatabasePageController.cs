using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.LegacyDeployment;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

using log4net;
using Amazon.RDS.Model;
using Amazon.AWSToolkit.CommonUI;
using Amazon.RDS;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.LegacyDeployment
{
    internal class DatabasePageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(DatabasePageController));

        List<DBSecurityGroup> _dbSecurityGroups = null;
        List<DBInstance> _dbInstances = null;   // keeping these around, may be useful
        bool _refreshPageContentOnActivation = false;

        DatabasePage _pageUI;

        string _lastSeenKey = null;

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
            get { return "Amazon RDS Database Security Group"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Connect your AWS Elastic Beanstalk environment to your RDS DB instance."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (!IsWizardInBeanstalkMode || this.IsLaunchIntoVPC)
                return false;

            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                AccountViewModel selectedAccount
                    = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                RegionEndPointsManager.RegionEndPoints region
                        = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                as RegionEndPointsManager.RegionEndPoints;

                string seenKey = string.Format("{0}_{1}", selectedAccount.SettingsUniqueKey, region.SystemName);
                if (string.Compare(seenKey, _lastSeenKey) != 0)
                {
                    _lastSeenKey = seenKey;
                    _dbInstances = FindAvailableRDSInstances(selectedAccount, region);
                    _dbSecurityGroups = QueryAvailableDBSecurityGroups(selectedAccount, region);
                    _refreshPageContentOnActivation = true;
                }
            }

            // seems we always have at least one group, so unless they also have instances
            // it's not worth showing the page. Also clear any previously held groups (user
            // may have gone back and changed accounts)
            bool show = _dbInstances != null && _dbInstances.Count > 0;
            if (!show)
                HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups] = null;

            return show;
        }

        public System.Windows.Controls.UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new DatabasePage();
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_refreshPageContentOnActivation)
            {
                _pageUI.SetAvailableSecurityGroups(_dbSecurityGroups, _dbInstances);
                _refreshPageContentOnActivation = false;
            }
            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            // if we're going backwards, always clear any held groups so that if user 
            // chooses another a/c without instances, the previous groups don't show
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                StorePageData();
            else
                HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups] = null;

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            if (!IsWizardInBeanstalkMode)
                return true;

            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            bool fwdsOK = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, fwdsOK);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, fwdsOK);
        }

        public bool AllowShortCircuit()
        {
            // always true, we have no mandatory data
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

        bool IsLaunchIntoVPC
        {
            get
            {
                bool launchIntoVPC = HostingWizard.GetProperty<bool>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC);
                return launchIntoVPC;
            }
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                return true;
            }
        }

        void StorePageData()
        {
            if (_pageUI == null)
                return;

            List<string> dbSecurityGroups = _pageUI.DBSecurityGroups;
            if (dbSecurityGroups.Count > 0)
                HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups] = dbSecurityGroups;
            else
                HostingWizard[BeanstalkDeploymentWizardProperties.DatabaseOptions.propkey_RDSSecurityGroups] = null;
        }

        void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        // synchronous because we need to know if instances are available to determine if page shows
        // itself
        List<DBInstance> FindAvailableRDSInstances(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            var config = new AmazonRDSConfig();
            config.ServiceURL = region.GetEndpoint(RegionEndPointsManager.RDS_SERVICE_NAME).Url;
            var rdsClient = new AmazonRDSClient(account.Credentials, config);

            List<DBInstance> dbInstances = null;
            try
            {
                var response = rdsClient.DescribeDBInstances(new DescribeDBInstancesRequest());
                dbInstances = response.DBInstances;
            }
            catch (Exception exc)
            {
                LOGGER.Error(GetType().FullName + ", exception in Worker", exc);
            }

            return dbInstances;
        }

        // sync query since we need to know if there are groups available to show the page
        List<DBSecurityGroup> QueryAvailableDBSecurityGroups(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            var config = new AmazonRDSConfig();
            config.ServiceURL = region.GetEndpoint(RegionEndPointsManager.RDS_SERVICE_NAME).Url;
            var rdsClient = new AmazonRDSClient(account.Credentials, config);

            var dbSecurityGroups = new List<DBSecurityGroup>();
            try
            {
                var response = rdsClient.DescribeDBSecurityGroups();
                // filter out vpc groups as Beanstalk doesn't support
                dbSecurityGroups.AddRange(response.DBSecurityGroups.Where(@group => string.IsNullOrEmpty(@group.VpcId)));
            }
            catch (Exception exc)
            {
                LOGGER.Error(GetType().FullName + ", exception in Worker", exc);
            }

            return dbSecurityGroups;
        }
    }
}
