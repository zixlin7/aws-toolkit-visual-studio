using Amazon.AWSToolkit.ECS.Nodes;
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
    public class ViewTaskDefinitionController : FeatureController<ViewTaskDefinitionModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewTaskDefinitionController));

        ViewTaskDefinitionControl _control;

        protected override void DisplayView()
        {
            this._control = new ViewTaskDefinitionControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            if (this.Model.TaskDefinition == null) // first time load
            {
                var taskDefinitionViewModel = this.FeatureViewModel as TaskDefinitionViewModel;
                if (taskDefinitionViewModel == null)
                    throw new InvalidOperationException("Expected TaskDefinitionViewModel type for FeatureViewModel");

                // all available data already loaded
                this.Model.TaskDefinition = taskDefinitionViewModel.TaskDefinition;
            }
            else
            {
                // refresh request from the control
                try
                {
                    /* todo
                    var request = new DescribeRepositoriesRequest
                    {
                        RepositoryNames = new List<string>
                        {
                            this.Model.Repository.RepositoryArn
                        }
                    };
                    ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(AWSToolkit.Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
                    var response = this.ECRClient.DescribeRepositories(request);
                    var wrapper = new RepositoryWrapper(response.Repositories.FirstOrDefault());
                    this.Model.Repository = wrapper;
                    */
                }
                catch (Exception e)
                {
                    var msg = "Failed to refresh data for task definition. The service returned the error " + e.Message;
                    LOGGER.Error(msg, e);
                    ToolkitFactory.Instance.ShellProvider.ShowError(msg, "Refresh Failed");
                }
            }
        }
    }
}
