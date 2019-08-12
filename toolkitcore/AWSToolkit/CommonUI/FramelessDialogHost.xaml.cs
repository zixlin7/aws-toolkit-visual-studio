using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for FramelessDialogHost.xaml
    /// </summary>
    public partial class FramelessDialogHost : Window
    {
        IAWSToolkitControl _hostedControl;

        public FramelessDialogHost(IAWSToolkitControl hostedControl)
        {
            this._hostedControl = hostedControl;
            InitializeComponent();

            this.Width = (int)(hostedControl.UserControl.Width);
            this.Height = (int)(hostedControl.UserControl.Height);
            addHostedControl();

            MouseDown += delegate { DragMove(); };
        }

        public IAWSToolkitControl HostedControl => this._hostedControl;

        private void addHostedControl()
        {
            this._hostedControl.UserControl.Width = double.NaN;
            this._hostedControl.UserControl.Height = double.NaN;
            Grid.SetColumn(this._hostedControl.UserControl, 0);
            Grid.SetRow(this._hostedControl.UserControl, 0);
            this._ctlMainGrid.Children.Add(this._hostedControl.UserControl);
        }
    }
}
