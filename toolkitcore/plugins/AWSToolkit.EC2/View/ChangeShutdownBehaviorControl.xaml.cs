using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChangeShutdownBehaviorControl.xaml
    /// </summary>
    public partial class ChangeShutdownBehaviorControl : BaseAWSControl
    {
        public ChangeShutdownBehaviorControl(ChangeShutdownBehaviorModel model)
        {
            InitializeComponent();
            this.DataContext = model;
        }

        public override string Title => "Change Shutdown Behavior";

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlBehavior.Focus();
        }
    }
}
