﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.AWSToolkit.RDS.WizardPages.PageUI;
using Amazon.AWSToolkit.RDS.WizardPages.PageWorkers;

using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.RDS.Model;
// avoid collision with RDS zones
using EC2AvailabilityZone = Amazon.EC2.Model.AvailabilityZone;

using log4net;
using Amazon.RDS;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageControllers
{
    internal class LaunchDBInstanceAdvancedSettingsPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(LaunchDBInstanceAdvancedSettingsPageController));

        readonly object _syncLock = new object();
        LaunchDBInstanceAdvancedSettingsPage _pageUI;
        string _lastSeenEngineType = string.Empty;
        bool? _lastSeenMultiAz = null;
        bool _launchIntoVpcOnly;

        int _backgroundWorkersActive;
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

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Advanced Settings";

        public string ShortPageTitle => null;

        public string PageDescription => "Set additional network and database options for your instance.";

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
                _pageUI = new LaunchDBInstanceAdvancedSettingsPage();
                _pageUI.PropertyChanged += _pageUI_PropertyChanged;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            var engineVersions = HostingWizard[RDSWizardProperties.SeedData.propkey_DBEngineVersions] as List<DBEngineVersionWrapper>;
            var isMultiAz = true;
            if (HostingWizard.IsPropertySet(RDSWizardProperties.InstanceProperties.propkey_MultiAZ))
                isMultiAz = (bool)HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MultiAZ];

            if (string.Compare(engineVersions[0].Title, _lastSeenEngineType, StringComparison.InvariantCultureIgnoreCase) != 0
                || _lastSeenMultiAz.GetValueOrDefault() != isMultiAz)
            {
                // first time in or user has gone back to preceding page and changed engine class, need to re-initialize
                _lastSeenEngineType = engineVersions[0].Title;
                _lastSeenMultiAz = isMultiAz;

                _launchIntoVpcOnly = (bool) HostingWizard[RDSWizardProperties.SeedData.propkey_VPCOnly];
                _pageUI.RefreshContent(_lastSeenEngineType, isMultiAz, _launchIntoVpcOnly);

                // suspect these can be done once, on first page load and kept around regardless
                // of engine changes but we'll only worry about it if there's a perf hit once the
                // wizard is finished
                QueryAvailableVpcsAndDBSubnetGroups();
                QueryAvailableDBSecurityGroups();
                QueryAvailabilityZones();
                QueryAvailableDBParameterGroups(HostingWizard[RDSWizardProperties.EngineProperties.propkey_EngineVersion] as DBEngineVersion);
            }

            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                StorePageData();

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            var allowed = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, allowed);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, allowed);
        }

        public bool AllowShortCircuit()
        {
            return !_launchIntoVpcOnly;
        }

        #endregion

        void StorePageData()
        {
            if (_pageUI != null)
            {
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DatabaseName] = _pageUI.DatabaseName;
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DatabasePort] = _pageUI.DatabasePort;

                if (_pageUI.LaunchingIntoVpc)
                {
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_VpcId] = _pageUI.SelectedVpc.VpcId;
                    var createNewSubnetGroup = _pageUI.CreateNewDBSubnetGroup;
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_CreateDBSubnetGroup] = createNewSubnetGroup;
                    if (!createNewSubnetGroup)
                        HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DBSubnetGroup] = _pageUI.SelectedDbSubnetGroup.NativeSubnetGroup.DBSubnetGroupName;
                    else
                        HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DBSubnetGroup] = null;
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_PubliclyAccessible] = _pageUI.PubliclyAccessible;
                }
                else
                {
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_VpcId] = null;
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DBSubnetGroup] = null;
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_PubliclyAccessible] = null;
                }

                if (!_pageUI.NoPreferenceAvailabilityZone)
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_AvailabilityZone] = _pageUI.SelectedAvailabilityZone.ZoneName;
                else
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_AvailabilityZone] = null;

                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup] = _pageUI.DbParameterGroup;
                if (_pageUI.CreateNewSecurityGroup)
                {
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_CreateNewDBSecurityGroup] = true;
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_SecurityGroups] = null;
                }
                else
                {
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_CreateNewDBSecurityGroup] = false;
                    HostingWizard[RDSWizardProperties.InstanceProperties.propkey_SecurityGroups] = _pageUI.SelectedSecurityGroups;
                }

                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_AddCIDRToGroups] = _pageUI.AddCidrToSecurityGroups;
            }
            else
            {
                // poke the default port for the engine so user can verify in review
                var engineVersions = HostingWizard[RDSWizardProperties.SeedData.propkey_DBEngineVersions] as List<DBEngineVersionWrapper>;
                var meta = RDSServiceMeta.Instance.MetaForEngine(engineVersions[0].Title);
                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DatabasePort] = meta.DefaultPort;
            }
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                // If the view has never been viewed then allow it to be skipped and use the default parameters.
                if (_pageUI == null)
                    return true;

                // If the view has been created but is currently fetching data then do not allow forward progress
                if (BackgroundWorkerCount > 0)
                    return false;

                // If launch into VPC is enabled but a subnet has not been selected that stop forward progress.
                if (_pageUI.LaunchingIntoVpc && !_pageUI.CreateNewDBSubnetGroup && _pageUI.SelectedDbSubnetGroup == null)
                    return false;

                return true;
            }
        }

        void _pageUI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("SelectedVpc", StringComparison.Ordinal))
            {
                var vpcId = _pageUI.SelectedVpc.VpcId;
                switch (vpcId)
                {
                    case VPCWrapper.NotInVpcPseudoId:
                        QueryAvailableDBSecurityGroups();
                        break;
                    case VPCWrapper.CreateNewVpcPseudoId:
                        _pageUI.SetSecurityGroups(new List<SecurityGroup>()); // will auto-add 'create new' for us
                        break;
                    default:
                        QueryAvailableVPCSecurityGroups(vpcId);
                        break;
                }
            }

            TestForwardTransitionEnablement();
        }

        void QueryAvailableDBParameterGroups(DBEngineVersion engineVersion)
        {
            var rdsClient = HostingWizard[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS;
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryDBParameterGroupsWorker(rdsClient,
                                             engineVersion,
                                             LOGGER,
                                             QueryAvailableDBParameterGroupsComplete);
        }

        void QueryAvailableDBParameterGroupsComplete(IEnumerable<DBParameterGroup> dbParameterGroups)
        {
            Interlocked.Decrement(ref _backgroundWorkersActive);
            _pageUI.SetDbParameterGroupList(dbParameterGroups);
        }

        void QueryAvailableDBSecurityGroups()
        {
            var rdsClient = HostingWizard[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS;
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryDBSecurityGroupsWorker(rdsClient,
                                            LOGGER,
                                            QueryAvailableDBSecurityGroupsComplete);
        }

        void QueryAvailableDBSecurityGroupsComplete(IEnumerable<DBSecurityGroup> dbSecurityGroups)
        {
            Interlocked.Decrement(ref _backgroundWorkersActive);
            _pageUI.SetSecurityGroups(dbSecurityGroups);
        }

        void QueryAvailableVPCSecurityGroups(string vpcId)
        {
            var ec2Client = HostingWizard[RDSWizardProperties.SeedData.propkey_EC2Client] as IAmazonEC2;
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryVPCSecurityGroupsWorker(ec2Client,
                                             vpcId,
                                             LOGGER,
                                             QueryAvailableVPCSecurityGroupsComplete);
        }

        void QueryAvailableVPCSecurityGroupsComplete(IEnumerable<SecurityGroup> securityGroups)
        {
            Interlocked.Decrement(ref _backgroundWorkersActive);
            _pageUI.SetSecurityGroups(securityGroups);
        }

        void QueryAvailableVpcsAndDBSubnetGroups()
        {
            var ec2Client = HostingWizard[RDSWizardProperties.SeedData.propkey_EC2Client] as IAmazonEC2;
            var rdsClient = HostingWizard[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS;
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryVpcsAndSubnetGroupsWorker(ec2Client, rdsClient, LOGGER, QueryAvailableVpcsAndDBSubnetGroupsComplete);
        }

        void QueryAvailableVpcsAndDBSubnetGroupsComplete(IEnumerable<Vpc> vpcs, IEnumerable<DBSubnetGroup> dbSubnetGroups)
        {
            Interlocked.Decrement(ref _backgroundWorkersActive);
            _pageUI.SetAvailableVpcsAndDbSubnetGroups(vpcs, dbSubnetGroups, _launchIntoVpcOnly);
        }

        void QueryAvailabilityZones()
        {
            var account = HostingWizard.GetSelectedAccount();
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryAvailabilityZonesWorker(account,
                                             ToolkitFactory.Instance.Navigator.SelectedRegion,
                                             LOGGER,
                                             QueryAvailabilityZonesComplete);
        }

        void QueryAvailabilityZonesComplete(IEnumerable<EC2AvailabilityZone> availabilityZones)
        {
            Interlocked.Decrement(ref _backgroundWorkersActive);
            _pageUI.SetAvailabilityZoneList(availabilityZones);
        }
    }
}
