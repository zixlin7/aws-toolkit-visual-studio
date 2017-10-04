using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.View;
using log4net;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class ViewClusterController : ECSFeatureController<ViewClusterModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewClusterController));

        ViewClusterControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewClusterControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {

        }
    }
}
