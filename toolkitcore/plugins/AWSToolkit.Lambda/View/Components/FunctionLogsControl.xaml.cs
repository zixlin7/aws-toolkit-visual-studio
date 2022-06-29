using Amazon.AWSToolkit.Lambda.Controller;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for FunctionLogsControl.xaml
    /// </summary>
    public partial class FunctionLogsControl
    {
        ViewFunctionController _controller;

        public FunctionLogsControl()
        {
            InitializeComponent();
        }

        public void Initialize(ViewFunctionController controller)
        {
            this._controller = controller;
            var logsControl = controller.GetLogStreamsView();
            //set view as child to the border control
            LogBorder.Child = logsControl;
        }

 
    }
}
