using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using log4net;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class ViewRepositoryController : FeatureController<ViewRepositoryModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewRepositoryController));

        ViewRepositoryControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewRepositoryControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {

        }
    }
}
