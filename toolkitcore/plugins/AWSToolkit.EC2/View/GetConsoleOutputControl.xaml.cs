using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for GetConsoleControl.xaml
    /// </summary>
    public partial class GetConsoleOutputControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(GetConsoleOutputControl));

        GetConsoleOutputController _controller;

        public GetConsoleOutputControl(GetConsoleOutputController controller)
        {
            this._controller = controller;
            this.DataContext = controller.Model;
            InitializeComponent();
        }

        public override string Title => string.Format("Console Output for Instance {0}", this._controller.Model.InstanceId);
    }
}
