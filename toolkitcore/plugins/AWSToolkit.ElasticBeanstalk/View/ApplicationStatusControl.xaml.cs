using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View
{
    /// <summary>
    /// Interaction logic for ApplicationStatusControl.xaml
    /// </summary>
    public partial class ApplicationStatusControl : BaseAWSControl
    {
        ApplicationStatusController _controller;
        public ApplicationStatusControl(ApplicationStatusController controller)
        {
            this._controller = controller;
            InitializeComponent();

            this._ctlEvents.Initialize(this._controller);
            this._ctlApplicationVersions.Initialize(this._controller);
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void OnEditorOpened(bool success)
        {
            ToolkitFactory.Instance.TelemetryLogger.RecordBeanstalkOpenApplication(new BeanstalkOpenApplication()
            {
                Result = success ? Result.Succeeded : Result.Failed
            });
        }
        
        public override string Title => "App: " + this._controller.Model.ApplicationName;

        public override string UniqueId => "AppStatus" + this._controller.Model.ApplicationName;

        void onRefreshClick(object sender, RoutedEventArgs e)
        {
            this._controller.Refresh();
        }
    }
}
