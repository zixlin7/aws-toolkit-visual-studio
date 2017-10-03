using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.View;
using log4net;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class ViewClustersController : FeatureController<ViewClustersModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewClustersController));

        ViewClustersControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewClustersControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            
        }
    }
}
