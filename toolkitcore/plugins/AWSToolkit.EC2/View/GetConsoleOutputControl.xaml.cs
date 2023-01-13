using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for GetConsoleControl.xaml
    /// </summary>
    public partial class GetConsoleOutputControl : BaseAWSControl
    {
        private readonly GetConsoleOutputModel _model;

        public GetConsoleOutputControl(GetConsoleOutputModel model)
        {
            _model = model;
            InitializeComponent();
            DataContext = model;
        }

        public override string Title => $"Console Output for Instance {_model.InstanceId}";
    }
}
