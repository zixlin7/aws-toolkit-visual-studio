using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CloudFormation.Controllers;

namespace Amazon.AWSToolkit.CloudFormation.View
{
    /// <summary>
    /// Interaction logic for ViewDeploymentLogControl.xaml
    /// </summary>
    public partial class ViewDeploymentLogControl : BaseAWSControl
    {
        ViewDeploymentLogController _controller;

        public ViewDeploymentLogControl(ViewDeploymentLogController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContextChanged += onDataContextChanged;            
        }

        void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if(string.IsNullOrEmpty(this._controller.Model.ErrorMessage))
                this._ctlLoadingStatus.Text = string.Format("Retrieved log from instance {0}", this._controller.Model.InstanceId);
            else
                this._ctlLoadingStatus.Text = string.Format("Error retrieving log: {0}", this._controller.Model.ErrorMessage);
        }

        public override string Title => string.Format("Deployment Log for Instance {0}", this._controller.Model.InstanceId);

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }
    }
}
