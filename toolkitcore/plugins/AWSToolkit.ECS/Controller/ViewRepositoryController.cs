using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.View;
using Amazon.ECR.Model;
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
            if (this.Model.Repository == null) // first time load
            {
                var repositoryViewModel = this.FeatureViewModel as RepositoryViewModel;
                if (repositoryViewModel == null)
                    throw new InvalidOperationException("Expected RepositoryViewModel type for FeatureViewModel");

                // all available data already loaded
                this.Model.Repository = repositoryViewModel.Repository;
            }
            else
            {
                // refresh request from the control
                try
                {
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
                }
                catch (Exception e)
                {
                    var msg = "Failed to refresh data for repository. The service returned the error " + e.Message;
                    LOGGER.Error(msg, e);
                    ToolkitFactory.Instance.ShellProvider.ShowError(msg, "Refresh Failed");
                }
            }
        }
    }
}
