using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Shared;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    /// <summary>
    /// Interaction logic for OkDialogWindowHost.xaml
    /// </summary>
    public partial class OkDialogWindowHost : DialogWindow
    {

        private const int ContentMargin = 12;
        private const int ButtonHeight = 23;

        private readonly IAWSToolkitControl _hostedControl;

        public OkDialogWindowHost(IAWSToolkitControl hostedControl)
        {
            _hostedControl = hostedControl;

            InitializeComponent();

            Width = (int) (hostedControl.UserControl.Width + (2 * ContentMargin));
            Height = (int) (hostedControl.UserControl.Height + (2 * ContentMargin)  + ButtonHeight);
            MinWidth = Width;
            MinHeight = Height;
            AddHostedControl();
            Title = _hostedControl.Title;

            if (_hostedControl.SupportsDynamicOKEnablement)
            {
                _hostedControl.PropertyChanged += HostedControlOnPropertyChanged;
                okButton.IsEnabled = _hostedControl.Validated();
            }
            else
            {
                okButton.IsEnabled = true;
            }
        }

        private void HostedControlOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            okButton.IsEnabled = _hostedControl.Validated();
        }


        private void AddHostedControl()
        {
            _hostedControl.UserControl.Width = double.NaN;
            _hostedControl.UserControl.Height = double.NaN;
            Grid.SetColumn(_hostedControl.UserControl, 0);
            Grid.SetRow(_hostedControl.UserControl, 0);
            _ctlMainGrid.Children.Add(_hostedControl.UserControl);
        }

        void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_hostedControl.Validated() || !_hostedControl.OnCommit())
                return;

            // Dialog box accepted
            DialogResult = true;
        }
    }
}
