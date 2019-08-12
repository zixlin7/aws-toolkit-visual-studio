using System.Collections.Generic;
using System.Text;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.RDS.WizardPages.PageUI;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageControllers
{
    internal class ModifyDBInstanceReviewPageController : IAWSWizardPageController
    {
        ModifyDBInstanceReviewPage _pageUI;

        #region IAWSWizardPageController Members

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Review";

        public string ShortPageTitle => null;

        public string PageDescription => "Review the information below, then click Finish to modify the instance.";

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
                _pageUI = new ModifyDBInstanceReviewPage();

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            this._pageUI.ClearPanels();

            AddModifiedInstanceSettingsPanel();
            AddModifiedMaintenanceSettingsPanel();

            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Modify");
            HostingWizard.RequestFinishEnablement(this);
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            HostingWizard[RDSWizardProperties.ReviewProperties.propkey_ApplyImmediately] = _pageUI.ApplyImmediately;
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        #endregion

        void AddModifiedInstanceSettingsPanel()
        {
            StringBuilder sb = new StringBuilder();

            if (HostingWizard.IsPropertySet(RDSWizardProperties.EngineProperties.propkey_EngineVersion))
            {
                sb.AppendFormat("Set DB Engine to version '{0}'.", 
                                HostingWizard[RDSWizardProperties.EngineProperties.propkey_EngineVersion] as string);
                sb.AppendLine();
            }

            if (HostingWizard.IsPropertySet(RDSWizardProperties.InstanceProperties.propkey_InstanceClass))
            {
                sb.AppendFormat("Set DB Instance Class to '{0}'.", 
                                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_InstanceClass] as string);
                sb.AppendLine();
            }

            if (HostingWizard.IsPropertySet(RDSWizardProperties.InstanceProperties.propkey_MultiAZ))
            {
                if ((bool)HostingWizard[RDSWizardProperties.InstanceProperties.propkey_MultiAZ])
                    sb.AppendLine("Enable MultiAZ support for the instance.");
                else
                    sb.AppendLine("Disable MultiAZ support for the instance.");
            }

            if (HostingWizard.IsPropertySet(RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade))
            {
                if ((bool)HostingWizard[RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade])
                    sb.AppendLine("Enable automatic updates for minor version upgrades.");
                else
                    sb.AppendLine("Disable automatic updates for minor version upgrades.");
            }

            if (HostingWizard.IsPropertySet(RDSWizardProperties.InstanceProperties.propkey_Storage))
            {
                sb.AppendFormat("Set allocated storage for the instance to {0}GB.",
                                (int)HostingWizard[RDSWizardProperties.InstanceProperties.propkey_Storage]);
                sb.AppendLine();
            }

            if (HostingWizard.IsPropertySet(RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup))
            {
                sb.AppendFormat("Set the DB Parameter Group for the instance to be '{0}'.",
                                HostingWizard[RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup]);
                sb.AppendLine();
            }

            if (HostingWizard.IsPropertySet(RDSWizardProperties.InstanceProperties.propkey_SecurityGroups))
            {
                // use an empty collection as a sign the user changed nothing
                List<string> securityGroups = HostingWizard[RDSWizardProperties.InstanceProperties.propkey_SecurityGroups] as List<string>;
                if (securityGroups.Count > 0)
                {
                    sb.AppendLine("Assign the following DB Security Groups:");
                    foreach (string group in securityGroups)
                    {
                        sb.AppendFormat("    {0}", group);
                        sb.AppendLine();
                    }
                }
            }

            if (HostingWizard.IsPropertySet(RDSWizardProperties.InstanceProperties.propkey_MasterUserPassword))
            {
                sb.AppendLine("Set a new master user password.");
            }

            if (sb.Length == 0)
                sb.Append("No DB engine/instance settings were changed.");

            TextBlock tb = new TextBlock();
            tb.TextWrapping = System.Windows.TextWrapping.Wrap;
            tb.Text = sb.ToString();

            _pageUI.AddReviewPanel("DB Engine/Instance Changes", tb);
        }

        void AddModifiedMaintenanceSettingsPanel()
        {
            StringBuilder sb = new StringBuilder();

            if (HostingWizard.IsPropertySet(RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod))
            {
                int retentionPeriod = (int)HostingWizard[RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod];
                if (retentionPeriod == 0)
                    sb.AppendLine("Turn off automatic backups for the instance.");
                else
                {
                    sb.AppendFormat("Enable automatic backups and retain for {0} day(s).",
                                    retentionPeriod);
                    sb.AppendLine();
                }
            }

            if (sb.Length == 0)
                sb.Append("No backup/maintenance settings were changed.");

            TextBlock tb = new TextBlock();
            tb.TextWrapping = System.Windows.TextWrapping.Wrap;
            tb.Text = sb.ToString();

            _pageUI.AddReviewPanel("Backup and Maintenance Changes", tb);
        }
    }
}
