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
using System.Windows.Shapes;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for FramelessDialogHost.xaml
    /// </summary>
    public partial class FramelessDialogHost : Window
    {
        IAWSControl _hostedControl;

        public FramelessDialogHost(IAWSControl hostedControl)
        {
            this._hostedControl = hostedControl;
            InitializeComponent();

            this.Width = (int)(hostedControl.UserControl.Width);
            this.Height = (int)(hostedControl.UserControl.Height);
            addHostedControl();

            MouseDown += delegate { DragMove(); };
        }

        public IAWSControl HostedControl
        {
            get { return this._hostedControl; }
        }

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
