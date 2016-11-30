using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;
using System.ComponentModel;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for DBInstanceBackupsPage.xaml
    /// </summary>
    public partial class DBInstanceBackupsPage : INotifyPropertyChanged
    {
        const int MAX_RETENTION_PERIOD = 35;
        static List<string> _hours = new List<string>();
        static List<string> _mins = new List<string>();

        static DBInstanceBackupsPage()
        {
            // horrible controls :-(
            for (int i = 0; i < 24; i++)
            {
                _hours.Add(i.ToString("d2"));
            }

            for (int i = 0; i < 60; i++)
            {
                _mins.Add(i.ToString("d2"));
            }
        }

        public DBInstanceBackupsPage()
        {
            InitializeComponent();

            InitRetentionPeriodsCombo();
            InitBackupControls();
            InitMaintenanceControls();
        }

        public void SetBackupAndMaintenanceInfo(int retentionPeriod, string backupWindow, string maintenanceWindow)
        {
            _btnRetainBackups.IsChecked = !(retentionPeriod == 0);
            if (retentionPeriod > 0)
            {
                foreach (ComboBoxItem cbi in _retentionPeriod.Items)
                {
                    if ((int)cbi.Tag == retentionPeriod)
                    {
                        _retentionPeriod.SelectedItem = cbi;
                        break;
                    }
                }
            }

            DeconstructBackupWindow(backupWindow);
            DeconstructMaintenanceWindow(maintenanceWindow);
        }

        public bool BackupsEnabled
        {
            get { return _btnRetainBackups.IsChecked == true; }
        }

        public int BackupRetentionPeriod
        {
            get { return int.Parse((_retentionPeriod.SelectedItem as ComboBoxItem).Tag.ToString()); }
        }

        public bool IsCustomBackupWindow
        {
            get { return _btnCustomBackupWindow.IsChecked == true; }
        }

        public DateTime CustomBackupStart
        {
            get
            {
                string startTime = string.Format("{0:d2}:{1:d2}:00Z", 
                                                 _backupStartHours.SelectedItem as string,
                                                 _backupStartMins.SelectedItem as string);

                DateTime dt;
                DateTime.TryParseExact(startTime,
                                       "HH:mm:ssZ",
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.RoundtripKind | DateTimeStyles.NoCurrentDateDefault,
                                       out dt);

                return dt;
            }
        }

        public TimeSpan CustomBackupDuration
        {
            get { return new TimeSpan(0, (int)(_backupDuration.Value * 60), 0); }
        }

        public bool IsCustomMaintenanceWindow
        {
            get { return _btnCustomMaintenanceWindow.IsChecked == true; }
        }

        public string CustomMaintenanceDay
        {
            get { return _maintDay.SelectedItem as string; }
        }

        public DateTime CustomMaintenanceStart
        {
            get
            {
                string startTime = string.Format("{0:d2}:{1:d2}:00Z",
                                                 _maintStartHours.SelectedItem as string,
                                                 _maintStartMins.SelectedItem as string);

                DateTime dt;
                DateTime.TryParseExact(startTime,
                                       "HH:mm:ssZ",
                                       CultureInfo.InvariantCulture,
                                       DateTimeStyles.RoundtripKind | DateTimeStyles.NoCurrentDateDefault,
                                       out dt);
                
                return dt;
            }
        }

        public TimeSpan CustomMaintenanceDuration
        {
            get { return new TimeSpan(0, (int)(_maintDuration.Value * 60), 0); }
        }

        public bool BackupAndMaintenancePeriodsOverlap
        {
            get
            {
                if (!IsInitialized)
                    return false;

                if (!BackupsEnabled)
                    return false;

                bool overlap = false;

                // if one is set to default, console wizard doesn't seem to check so
                // do the same
                if (IsCustomBackupWindow && IsCustomMaintenanceWindow)
                {
                    DateTime backupStart = CustomBackupStart;
                    DateTime backupEnd = backupStart + CustomBackupDuration;

                    DateTime maintenanceStart = CustomMaintenanceStart;
                    DateTime maintenanceEnd = maintenanceStart + CustomMaintenanceDuration;

                    overlap = !((maintenanceEnd <= backupStart) || (maintenanceStart >= backupEnd));
                }

                _overlappingPeriodsMessage.Visibility = overlap ? Visibility.Visible : Visibility.Hidden;

                return overlap;
            }
        }

        void DeconstructBackupWindow(string backupWindow)
        {
            _currentBackupWindow.Text = string.Format("Current window: {0}", backupWindow);
            _currentBackupWindow.Visibility = Visibility.Visible;
        }

        void DeconstructMaintenanceWindow(string maintenanceWindow)
        {
            _currentMaintenanceWindow.Text = string.Format("Current window: {0}", maintenanceWindow);
            _currentMaintenanceWindow.Visibility = Visibility.Visible;
        }

        void InitRetentionPeriodsCombo()
        {
            for (int i = 1; i <= MAX_RETENTION_PERIOD; i++)
            {
                ComboBoxItem cbi = new ComboBoxItem();
                cbi.Tag = i;
                cbi.Content = string.Format("{0} day{1}", i, i > 1 ? "s" : "");
                _retentionPeriod.Items.Add(cbi);
            }

            _retentionPeriod.SelectedIndex = 0;
        }

        void InitBackupControls()
        {
            _backupStartHours.ItemsSource = _hours;
            _backupStartHours.SelectedIndex = 0;
            _backupStartMins.ItemsSource = _mins;
            _backupStartMins.SelectedIndex = 0;
            _backupDuration.Value = 0.5;
        }

        void InitMaintenanceControls()
        {
            // really need to l18n this and add three letter prefix as tag!
            string[] maintDays = new string[]
            {
                "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
            };

            _maintDay.ItemsSource = maintDays;
            _maintDay.SelectedIndex = 0;
            _maintStartHours.ItemsSource = _hours;
            _maintStartHours.SelectedIndex = 0;
            _maintStartMins.ItemsSource = _mins;
            _maintStartMins.SelectedIndex = 0;
            _maintDuration.Value = 0.5;
        }

        private void _btnCustomBackupWindow_Click(object sender, RoutedEventArgs e)
        {
            SetBackupControlsEnablement(_btnCustomBackupWindow.IsChecked == true);
            NotifyPropertyChanged("CustomBackup");
        }

        void SetBackupControlsEnablement(bool enable)
        {
            _backupStartHours.IsEnabled = enable;
            _backupStartMins.IsEnabled = enable;
            _backupDuration.IsEnabled = enable;
        }

        private void _btnCustomMaintenanceWindow_Click(object sender, RoutedEventArgs e)
        {
            SetMaintenanceControlsEnablement(_btnCustomMaintenanceWindow.IsChecked == true);
            NotifyPropertyChanged("CustomMaintenance");
        }

        void SetMaintenanceControlsEnablement(bool enable)
        {
            _maintDay.IsEnabled = enable;
            _maintStartHours.IsEnabled = enable;
            _maintStartMins.IsEnabled = enable;
            _maintDuration.IsEnabled = enable;
        }

        private void _btnRetainBackups_Checked(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                _retentionPeriod.IsEnabled = true;
                _btnCustomBackupWindow.IsEnabled = true;
                SetBackupControlsEnablement(_btnCustomBackupWindow.IsChecked == true);
                NotifyPropertyChanged("BackupRetention");
            }
        }

        private void _btnTurnOffBackups_Checked(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                _retentionPeriod.IsEnabled = false;
                _btnCustomBackupWindow.IsEnabled = false;
                SetBackupControlsEnablement(false);
                NotifyPropertyChanged("BackupRetention");
            }
        }

        private void BackupStart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("BackupTime");    
        }

        private void _backupDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized)
            {
                _lblBackupDurationHours.Content = string.Format("{0} hours", _backupDuration.Value);
                NotifyPropertyChanged("BackupDuration");
            }
        }

        private void MaintenanceStart_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged("MaintenanceTime");
        }

        private void _mainDuration_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (IsInitialized)
            {
                _lblMaintDurationHours.Content = string.Format("{0} hours", _maintDuration.Value);
                NotifyPropertyChanged("MaintenanceDuration");
            }
        }

        void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
