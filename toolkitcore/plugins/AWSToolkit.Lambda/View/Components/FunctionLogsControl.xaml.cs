using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Lambda.Controller;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for FunctionLogsControl.xaml
    /// </summary>
    public partial class FunctionLogsControl
    {
        ViewFunctionController _controller;
        private BaseAWSControl _logsControl;

        public FunctionLogsControl()
        {
            InitializeComponent();
        }

        public void Initialize(ViewFunctionController controller)
        {
            this._controller = controller;
            var logsControl = controller.GetLogStreamsView();
            _logsControl = logsControl;
            //set view as child to the border control
            LogBorder.Child = logsControl;
        }

        public BaseAWSControl LogsControl => _logsControl;
    }
}
