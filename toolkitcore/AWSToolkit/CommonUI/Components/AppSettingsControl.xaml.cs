using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            get { return this._settings; }
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
