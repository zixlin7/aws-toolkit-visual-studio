using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Interaction logic for AppSettingsControl.xaml
    /// </summary>
    public partial class AppSettingsControl : UserControl, INotifyPropertyChanged
    {
        ObservableCollection<AppSetting> _settings = new ObservableCollection<AppSetting>();

        public AppSettingsControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        public ObservableCollection<AppSetting> Settings
        {
            get => this._settings;
            set
            {
                this.OnPropertyChanged("Settings");
                this._settings = value;
            }
        }

        public class AppSetting
        {
            public string Key
            {
                get;
                set;
            }

            public string Value
            {
                get;
                set;
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var comboBox = e.OriginalSource as ComboBox;
            comboBox.Focus();
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event 
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion

    }
}
