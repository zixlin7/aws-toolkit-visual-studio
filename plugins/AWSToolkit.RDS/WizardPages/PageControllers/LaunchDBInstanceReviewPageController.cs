using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.RDS.WizardPages.PageUI;
using System.Windows.Controls;
using Amazon.RDS.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageControllers
{
    internal class LaunchDBInstanceReviewPageController : IAWSWizardPageController
    {
        LaunchDBInstanceReviewPage _pageUI;

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
            get { return "Review"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Review the information below, then click Finish to start instance creation."; }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public System.Windows.Controls.UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
                _pageUI = new LaunchDBInstanceReviewPage();

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            this._pageUI.ClearPanels();

            string engine = HostingWizard[RDSWizardProperties.EngineProperties.propkey_DBEngine] as string;
            DBEngineMeta meta = RDSServiceMeta.Instance.MetaForEngine(engine);

            AddEngineSelectionPanel(meta);
            AddOptionsPanel(meta);
            AddBackupMaintenancePanel(meta);

            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Launch");
            HostingWizard.RequestFinishEnablement(this);
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingBack)
            {
                HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Finish");
                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, true);
            }
            else
            {
                HostingWizard.SetProperty(RDSWizardProperties.ReviewProperties.propkey_LaunchInstancesViewOnClose, _pageUI.LaunchRDSInstancesWindow);
            }

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        #endregion

        void AddEngineSelectionPanel(DBEngineMeta engineMeta)
        {
            var sb = new StringBuilder();

            sb.AppendFormat("Launch an RDS instance using engine '{0}', version '{1}' with license '{2}'.",
                            engineMeta.DBEngine,
                            (HostingWizard[RDSWizardProperties.EngineProperties.propkey_EngineVersion] as DBEngineVersion).EngineVersion,
                            HostingWizard[RDSWizardProperties.InstanceProperties.propkey_LicenseModel] as string);

            sb.AppendLine();
            if (getValue<bool>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade))
                sb.AppendLine("Perform minor version upgrades automatically.");
            else
                sb.AppendLine("Do not perform minor version upgrades.");

            if (!engineMeta.IsSqlServer)
                sb.AppendFormat("Use an instance class of '{0} ({1})' and {2} to multiple availability zones.",
                                getValue<DBInstanceClass>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_InstanceClass).Name,
                                getValue<DBInstanceClass>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_InstanceClass).Id,
                               getValue<bool>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_MultiAZ)
                                    ? "deploy"
                                    : "do not deploy");
            else
                sb.AppendFormat("Use an instance class of '{0} ({1})'.",
                                getValue<DBInstanceClass>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_InstanceClass).Name,
                                getValue<DBInstanceClass>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_InstanceClass).Id);

            sb.AppendLine();

            sb.AppendFormat("Launch the instance with identifier '{0}', storage of {1}GB and a master user with id '{2}'.",
                            getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_DBInstanceIdentifier),
                            getValue<int>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_Storage),
                            getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_MasterUserName));

            var tb = new TextBlock {TextWrapping = System.Windows.TextWrapping.Wrap, Text = sb.ToString()};

            _pageUI.AddReviewPanel("DB Engine Details", tb);
        }

        void AddOptionsPanel(DBEngineMeta engineMeta)
        {
            var sb = new StringBuilder();

            var dbName = getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_DatabaseName);
            if (string.IsNullOrEmpty(dbName))
                sb.AppendFormat("No database {0} specified.", engineMeta.IsOracle ? "SID" : "name");
            else
                sb.AppendFormat("Use '{0}' as the database {1} on the instance.", dbName, engineMeta.IsOracle ? "SID" : "name");
            sb.AppendLine();

            sb.AppendFormat("Use port '{0}' to access the database.", 
                            getValue<int>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_DatabasePort));
            sb.AppendLine();

            var makePublic = getValue<bool>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_PubliclyAccessible);
            if (makePublic)
                sb.AppendLine("Make the instance publicly available.");
            else
                sb.AppendLine("Do not make the instance publicly available.");

            var vpcId = getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_VpcId);
            if (string.IsNullOrEmpty(vpcId))
                sb.AppendFormat("Do not launch the instance in a VPC.");
            else
            {
                if (vpcId.Equals(VPCWrapper.CreateNewVpcPseudoId))
                    sb.AppendFormat("Create a new VPC and subnet group for the instance.");
                else
                {
                    var subnetGroup = getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_DBSubnetGroup);
                    sb.AppendFormat("Launch the instance in subnet group '{0}' for VPC '{1}'.", subnetGroup, vpcId);
                }
            }
            sb.AppendLine();

            if (!getValue<bool>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_MultiAZ))
            {
                var az = getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_AvailabilityZone);
                if (string.IsNullOrEmpty(az))
                    sb.AppendLine("No availability zone preference selected.");
                else
                {
                    sb.AppendFormat("Launch the instance in availability zone '{0}'.", az);
                    sb.AppendLine();
                }   
            }

            var paramGroup = getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup);
            if (string.IsNullOrEmpty(paramGroup))
                sb.Append("No DB Parameter Groups available/specified.");
            else
                sb.AppendFormat("Use DB Parameter Group '{0}'.", paramGroup);
            sb.AppendLine();

            var createNewSecurityGroup = getValue<bool>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_CreateNewDBSecurityGroup);
            if (createNewSecurityGroup)
            {
                sb.AppendFormat("Create a new security group for the instance, allowing access for my current IP address {0}/32 (best estimate).", 
                                IPAddressUtil.DetermineIPFromExternalSource());
                sb.AppendLine();
            }
            else
            {
                var securityGroups = getValue<List<SecurityGroupInfo>>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_SecurityGroups);
                if (securityGroups != null && securityGroups.Count > 0)
                {
                    sb.AppendLine("Use the following security groups:");
                    foreach (var group in securityGroups)
                    {
                        sb.AppendFormat("    {0}", group.DisplayName);
                        sb.AppendLine();
                    }

                    if (getValue<bool>(HostingWizard.CollectedProperties, RDSWizardProperties.InstanceProperties.propkey_AddCIDRToGroups))
                    {
                        sb.AppendFormat("Add my current IP address (best estimate {0}) to the selected group(s).",
                                        IPAddressUtil.DetermineIPFromExternalSource() + "/32");
                        sb.AppendLine();
                    }
                }
                else
                    sb.AppendLine("No security groups available or specified for the instance.");
            }

            var tb = new TextBlock {TextWrapping = System.Windows.TextWrapping.Wrap, Text = sb.ToString()};

            _pageUI.AddReviewPanel("DB Options", tb);
        }

        void AddBackupMaintenancePanel(DBEngineMeta engineMeta)
        {
            var sb = new StringBuilder();

            var retention = getValue<int>(HostingWizard.CollectedProperties, RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod);
            if (retention > 0)
            {
                sb.AppendFormat("Turn on automatic backups and retain backups for {0} day(s).", retention);
                sb.AppendLine();

                var customBackup = getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.MaintenanceProperties.propkey_BackupWindow);
                if (!string.IsNullOrEmpty(customBackup))
                {
                    sb.AppendFormat("Set a custom backup window of '{0}'.", customBackup);
                    sb.AppendLine();
                }
                else
                    sb.AppendLine("Use default backup window.");
            }
            else
                sb.AppendLine("Do not perform automatic backups of the instance.");

            var customMaint = getValue<string>(HostingWizard.CollectedProperties, RDSWizardProperties.MaintenanceProperties.propkey_MaintenanceWindow);
            if (!string.IsNullOrEmpty(customMaint))
            {
                sb.AppendFormat("Set a custom maintenance window of '{0}'.", customMaint);
                sb.AppendLine();
            }
            else
                sb.AppendLine("Use default maintenance window.");
            
            var tb = new TextBlock {TextWrapping = System.Windows.TextWrapping.Wrap, Text = sb.ToString()};

            _pageUI.AddReviewPanel("Backup and Maintenance", tb);
        }

        T getValue<T>(Dictionary<string, object> properties, string key)
        {
            object value;
            if (properties.TryGetValue(key, out value))
            {
                T convertedValue = (T)Convert.ChangeType(value, typeof(T));
                return convertedValue;
            }

            return default(T);
        }
    }
}
