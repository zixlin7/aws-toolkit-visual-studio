using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.RDS.WizardPages.PageUI;
using System.Globalization;
using Amazon.AWSToolkit.RDS.Model;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageControllers
{
    internal class DBInstanceBackupsPageController : IAWSWizardPageController
    {
        DBInstanceBackupsPage _pageUI;

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
            get { return "Backup and Maintenance"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Set backup and maintenance options for your instance."; }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public System.Windows.Controls.UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new DBInstanceBackupsPage();
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            DBInstanceWrapper instance =  HostingWizard.GetProperty<DBInstanceWrapper>(RDSWizardProperties.SeedData.propkey_DBInstanceWrapper);
            if (instance != null)
            {
                // page being used in Modify DB Instance wizard - note that there is no way to detect
                // if this info is custom or a randomly assigned default
                _pageUI.SetBackupAndMaintenanceInfo(instance.NativeInstance.BackupRetentionPeriod,
                                                    instance.NativeInstance.PreferredBackupWindow,
                                                    instance.NativeInstance.PreferredMaintenanceWindow);
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
            // we have no mandatory parameters
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            bool enable = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enable);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, enable);
        }

        public bool AllowShortCircuit()
        {
            StorePageData();
            return true;
        }

        #endregion

        bool IsForwardsNavigationAllowed
        {
            get
            {
                return !(_pageUI.BackupAndMaintenancePeriodsOverlap);
            }
        }

        void StorePageData()
        {
            if (_pageUI != null)
            {
                if (_pageUI.BackupsEnabled)
                {
                    HostingWizard[RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod] = _pageUI.BackupRetentionPeriod;
                    if (_pageUI.IsCustomBackupWindow)
                    {
                        string backupWindow = ConstructPeriod(_pageUI.CustomBackupStart, _pageUI.CustomBackupDuration, "");
                        HostingWizard[RDSWizardProperties.MaintenanceProperties.propkey_BackupWindow] = backupWindow;
                    }
                    else
                    {
                        HostingWizard[RDSWizardProperties.MaintenanceProperties.propkey_BackupWindow] = null;
                    }
                }
                else
                    HostingWizard[RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod] = 0;

                if (_pageUI.IsCustomMaintenanceWindow)
                {
                    string maintWindow = ConstructPeriod(_pageUI.CustomMaintenanceStart, 
                                                         _pageUI.CustomMaintenanceDuration,
                                                         _pageUI.CustomMaintenanceDay);
                    HostingWizard[RDSWizardProperties.MaintenanceProperties.propkey_MaintenanceWindow] = maintWindow;
                }
                else
                    HostingWizard[RDSWizardProperties.MaintenanceProperties.propkey_MaintenanceWindow] = null;
            }
        }

        string ConstructPeriod(DateTime start, TimeSpan duration, string dayOfWeek)
        {
            DateTime dtEnd = start + duration;

            if (!string.IsNullOrEmpty(dayOfWeek))
            {
                // can't rely on dtEnd.ToString("ddd") here to get 'next day', as the actual
                // date is not accurately set
                string startDay = dayOfWeek;
                string endDay;
                if (start.Day == dtEnd.Day)
                    endDay = startDay;
                else
                {
                    DayOfWeek dow = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayOfWeek);
                    dow++;
                    endDay = dow.ToString();
                }

                return string.Format("{0}:{1:d2}:{2:d2}-{3}:{4:d2}:{5:d2}",
                                     dayOfWeek.Substring(0, 3),
                                     start.Hour,
                                     start.Minute,
                                     endDay.Substring(0, 3),
                                     dtEnd.Hour,
                                     dtEnd.Minute);
            }
            else
                return string.Format("{0:d2}:{1:d2}-{2:d2}:{3:d2}",
                                     start.Hour,
                                     start.Minute,
                                     dtEnd.Hour,
                                     dtEnd.Minute);
        }

        void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
