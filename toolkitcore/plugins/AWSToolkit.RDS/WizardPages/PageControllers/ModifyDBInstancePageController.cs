using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.RDS.WizardPages.PageUI;
using Amazon.AWSToolkit.RDS.WizardPages.PageWorkers;
using Amazon.AWSToolkit.RDS.Nodes;

using Amazon.RDS;
using Amazon.RDS.Model;

using log4net;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageControllers
{
    /// <summary>
    /// Implements a 'one page' wizard to allow updating of instance details
    /// </summary>
    internal class ModifyDBInstancePageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ModifyDBInstancePageController));
        object _syncLock = new object();
        ModifyDBInstancePage _pageUI;

        int _backgroundWorkersActive = 0;
        int BackgroundWorkerCount
        {
            get
            {
                int count;
                lock (_syncLock)
                {
                    count = _backgroundWorkersActive;
                }
                return count;
            }
        }

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard  { get; set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return "Modify Instance Details"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Update one or more details of your running RDS instance."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public System.Windows.Controls.UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new ModifyDBInstancePage();
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);
            }
            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            IAmazonRDS rdsClient = HostingWizard[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS;
            IAmazonEC2 ec2Client = HostingWizard[RDSWizardProperties.SeedData.propkey_EC2Client] as IAmazonEC2;
            DBInstanceWrapper instance = HostingWizard[RDSWizardProperties.SeedData.propkey_DBInstanceWrapper] as DBInstanceWrapper;

            QueryAvailableDBEngineVersions(rdsClient, instance.Engine);

            if (instance.NativeInstance.DBSubnetGroup != null)
                QueryAvailableSecurityGroups(ec2Client, instance.NativeInstance.DBSubnetGroup.VpcId);
            else
                QueryAvailableDBSecurityGroups(rdsClient);
            
            _pageUI.InstanceWrapper = instance;
            _pageUI.RefreshContent();
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
            bool enable = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enable);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, enable);
        }

        public bool AllowShortCircuit()
        {
            return IsForwardsNavigationAllowed;
        }

        #endregion

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false;

                bool ok = BackgroundWorkerCount == 0
                            && _pageUI.SelectedVersion != null
                            && _pageUI.InstanceClass != null
                            && _pageUI.Storage != -1;

                // password is an empty field for 'no change' so must check
                // separately
                if (ok && !string.IsNullOrEmpty(_pageUI.MasterUserPassword))
                    ok = _pageUI.IsPasswordValid;

                return ok;
            }
        }

        // only store that which has changed (different from same method in other wizards)
        void StorePageData()
        {
            DBInstanceWrapper instance = HostingWizard[RDSWizardProperties.SeedData.propkey_DBInstanceWrapper] as DBInstanceWrapper;

            if (string.Compare(_pageUI.SelectedVersion.EngineVersion,
                               instance.NativeInstance.EngineVersion,
                               StringComparison.InvariantCultureIgnoreCase) != 0)
                HostingWizard[RDSWizardProperties.EngineProperties.propkey_EngineVersion] = _pageUI.SelectedVersion.EngineVersion;
            else
                HostingWizard[RDSWizardProperties.EngineProperties.propkey_EngineVersion] = null;

            if (string.Compare(_pageUI.InstanceClass.Id,
                               instance.NativeInstance.DBInstanceClass,
                               StringComparison.InvariantCultureIgnoreCase) != 0)
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_InstanceClass] = _pageUI.InstanceClass.Id;
            else
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_InstanceClass] = null;

            if (_pageUI.IsMultiAZ != instance.NativeInstance.MultiAZ)
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MultiAZ] = _pageUI.IsMultiAZ;
            else
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MultiAZ] = null;

            if (_pageUI.AutoUpgradeMinorVersions != instance.NativeInstance.AutoMinorVersionUpgrade)
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade] = _pageUI.AutoUpgradeMinorVersions;
            else
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade] = null;

            if (_pageUI.Storage != instance.NativeInstance.AllocatedStorage)
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_Storage] = _pageUI.Storage;
            else
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_Storage] = null;

            string dbParamGroup = null;
            if (_pageUI.DBParameterGroup != null)
            {
                if (instance.NativeInstance.DBParameterGroups.Count > 0)
                {
                    if (string.Compare(_pageUI.DBParameterGroup, instance.NativeInstance.DBParameterGroups[0].DBParameterGroupName, StringComparison.InvariantCultureIgnoreCase) != 0)
                        dbParamGroup = _pageUI.DBParameterGroup;
                }
                else
                    dbParamGroup = _pageUI.DBParameterGroup;
            }
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup] = dbParamGroup;

            // need to handle > 1 selected group and perhaps optimise to detect changes
            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_SecurityGroups] = _pageUI.SelectedSecurityGroups;

            if (!string.IsNullOrEmpty(_pageUI.MasterUserPassword))
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MasterUserPassword] = _pageUI.MasterUserPassword;
            else
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MasterUserPassword] = null;
        }

        void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ModifyDBInstancePage.propkey_EngineVersion)
            {
                QueryAvailableDBParameterGroups(HostingWizard[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS,
                                                _pageUI.SelectedVersion);
            }

            TestForwardTransitionEnablement();
        }

        void QueryAvailableDBEngineVersions(IAmazonRDS rdsClient, string forEngine)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryDBEngineVersionsWorker(rdsClient,
                                            forEngine,
                                            LOGGER,
                                            new QueryDBEngineVersionsWorker.DataAvailableCallback(QueryAvailableDBEngineVersionsComplete));
        }

        void QueryAvailableDBEngineVersionsComplete(IEnumerable<DBEngineVersion> dbEngineVersions)
        {
            _pageUI.SetAvailableEngineVersions(dbEngineVersions);
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void QueryAvailableDBParameterGroups(IAmazonRDS rdsClient, DBEngineVersion engineVersion)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryDBParameterGroupsWorker(rdsClient,
                                             engineVersion,
                                             LOGGER,
                                             new QueryDBParameterGroupsWorker.DataAvailableCallback(QueryAvailableDBParameterGroupsComplete));
        }

        void QueryAvailableDBParameterGroupsComplete(IEnumerable<DBParameterGroup> dbParameterGroups)
        {
            _pageUI.SetAvailableParameterGroups(dbParameterGroups);
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void QueryAvailableDBSecurityGroups(IAmazonRDS rdsClient)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryDBSecurityGroupsWorker(rdsClient,
                                            LOGGER,
                                            new QueryDBSecurityGroupsWorker.DataAvailableCallback(QueryAvailableDBSecurityGroupsComplete));
        }

        void QueryAvailableDBSecurityGroupsComplete(IEnumerable<DBSecurityGroup> dbSecurityGroups)
        {
            _pageUI.SetSecurityGroups(dbSecurityGroups);
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void QueryAvailableSecurityGroups(IAmazonEC2 ec2Client, string vpcId)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryVPCSecurityGroupsWorker(ec2Client,
                                             vpcId,
                                             LOGGER,
                                             new QueryVPCSecurityGroupsWorker.DataAvailableCallback(QueryAvailableSecurityGroupsComplete));
        }
        void QueryAvailableSecurityGroupsComplete(IEnumerable<SecurityGroup> securityGroups)
        {
            _pageUI.SetSecurityGroups(securityGroups);
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }
    }
}
