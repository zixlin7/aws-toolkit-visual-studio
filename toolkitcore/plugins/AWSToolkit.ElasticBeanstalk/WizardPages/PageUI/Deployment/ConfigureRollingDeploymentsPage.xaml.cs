using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for ConfigureRollingDeployments.xaml
    /// </summary>
    public partial class ConfigureRollingDeploymentsPage : INotifyPropertyChanged
    {
        public static readonly string uiProperty_EnableConfigurationRollingDeployment = "EnableConfigurationRollingDeployment";
        public static readonly string uiProperty_ConfigMaximumBatchSize = "ConfigMaximumBatchSize";
        public static readonly string uiProperty_ConfigMinimumInstancesInService = "MinimumInstancesInService";

        public static readonly string uiProperty_ApplicationBatchType = "ApplicationBatchType";
        public static readonly string uiProperty_ApplicationBatchSize = "ApplicationBatchSize";

        public ConfigureRollingDeploymentsPage()
        {
            InitializeComponent();
            DataContext = this;
        }

        public ConfigureRollingDeploymentsPage(IAWSWizardPageController controller)
            : this()
        {
            PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public string AppBatchType
        {
            get
            {
                if (this._ctlIsFixedSizeType.IsChecked.GetValueOrDefault())
                    return "Fixed";

                return "Percentage";
            }
            set
            {
                if (string.Equals(value, "Fixed", StringComparison.InvariantCultureIgnoreCase))
                    this._ctlIsFixedSizeType.IsChecked = true;
                else
                    this._ctlIsPercentageType.IsChecked = true;
            }
        }

        public int? AppBatchSize
        {
            get
            {
                int value;
                if(this._ctlIsPercentageType.IsChecked.GetValueOrDefault())
                {
                    if (int.TryParse(this._ctlPercentage.Text, out value))
                        return value;
                }
                else
                {
                    if (int.TryParse(this._ctlFixedSize.Text, out value))
                        return value;
                }

                return null;
            }
        }

        public bool EnableConfigRollingDeployment
        {
            get => this._ctlEnableRolling.IsChecked.GetValueOrDefault();
            set => this._ctlEnableRolling.IsChecked = value;
        }

        public int? MaximumBatchSize
        {
            get
            {
                int value;
                if (int.TryParse(this._ctlMaxBatchSize.Text, out value))
                    return value;

                return null;
            }
            set => this._ctlMaxBatchSize.Text = value == null ? "" : value.ToString();
        }

        public int? MinInstanceInService
        {
            get
            {
                int value;
                if (int.TryParse(this._ctlMinInstanceInService.Text, out value))
                    return value;

                return null;
            }
            set => this._ctlMinInstanceInService.Text = value == null ? "" : value.ToString();
        }

        private void _ctlEnableRolling_Checked(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_EnableConfigurationRollingDeployment);
        }

        private void _ctlMaxBatchSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_ConfigMaximumBatchSize);
        }

        private void _ctlMinInstanceInService_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_ConfigMinimumInstancesInService);
        }

        private void _ctlIsPercentageType_Checked(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_ConfigMinimumInstancesInService);
            NotifyPropertyChanged(uiProperty_ApplicationBatchSize);
        }

        private void _ctlIsFixedSizeType_Checked(object sender, RoutedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_ConfigMinimumInstancesInService);
            NotifyPropertyChanged(uiProperty_ApplicationBatchSize);
        }

        private void _ctlPercentage_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_ApplicationBatchSize);
        }

        private void _ctlFixedSize_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged(uiProperty_ApplicationBatchSize);
        }
    }
}
