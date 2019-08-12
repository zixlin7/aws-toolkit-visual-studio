using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for OkDialogHost.xaml
    /// </summary>
    public partial class OkDialogHost : Window
    {
        readonly IAWSToolkitControl _hostedControl;

        public OkDialogHost(IAWSToolkitControl hostedControl)
        {
            this._hostedControl = hostedControl;
            InitializeComponent();

            SetResourceReference(Control.BackgroundProperty, "awsDialogBackgroundBrushKey");
            SetResourceReference(Control.ForegroundProperty, "awsDialogTextBrushKey");

            this.Width = (int)(hostedControl.UserControl.Width + 50);
            this.Height = (int)(hostedControl.UserControl.Height + 100);
            AddHostedControl();
            this.Title = this._hostedControl.Title;

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
            okButton.IsEnabled = this._hostedControl.Validated();
        }

        public IAWSToolkitControl HostedControl => this._hostedControl;

        private void AddHostedControl()
        {
            this._hostedControl.UserControl.Width = double.NaN;
            this._hostedControl.UserControl.Height = double.NaN;
            Grid.SetColumn(this._hostedControl.UserControl, 0);
            Grid.SetColumnSpan(this._hostedControl.UserControl, 2);
            Grid.SetRow(this._hostedControl.UserControl, 0);
            Grid.SetRowSpan(this._hostedControl.UserControl, 3);
            this._ctlMainGrid.Children.Add(this._hostedControl.UserControl);
        }

        void okButton_Click(object sender, RoutedEventArgs e)
        {
            if (!this._hostedControl.Validated() || !this._hostedControl.OnCommit())
                return;

            // Dialog box accepted
            this.DialogResult = true;
        }
    }
}
