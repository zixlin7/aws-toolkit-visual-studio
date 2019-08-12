using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using Amazon.AWSToolkit.RDS.Model;
using System.ComponentModel;

namespace Amazon.AWSToolkit.RDS.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for LaunchDBInstanceEnginePage.xaml
    /// </summary>
    public partial class LaunchDBInstanceEnginePage : INotifyPropertyChanged
    {
        public LaunchDBInstanceEnginePage()
        {
            InitializeComponent();
        }

        public IEnumerable<DBEngineType> AvailableEngineTypes
        {
            set => _dbEngineList.ItemsSource = value;
        }

        public string SelectedEngineType
        {
            get
            {
                if (IsInitialized && _dbEngineList.SelectedItem != null)
                    return (_dbEngineList.SelectedItem as DBEngineType).Title;
                
                return string.Empty;
            }
        }

        private void _dbEngineList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NotifyPropertyChanged(string.Empty);
        }
    }

    /// <summary>
    /// RDS api allows us to get versions of the db engines, but not a reduced collection
    /// of types for the purposes of this page
    /// </summary>
    public class DBEngineType
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public ImageSource EngineIcon => DBEngineVersionWrapper.IconForEngineType(Title);
    }
}
