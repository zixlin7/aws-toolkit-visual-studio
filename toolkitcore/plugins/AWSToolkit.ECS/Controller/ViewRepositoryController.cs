using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Nodes;
using Amazon.AWSToolkit.ECS.View;
using Amazon.ECR;
using Amazon.ECR.Model;
using log4net;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class ViewRepositoryController : FeatureController<ViewRepositoryModel>
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewRepositoryController));

        private ViewRepositoryControl _control;
        private RepositoryViewModel _viewModel;

        public string RepositoryArn
        {
            get { return ViewModel.Repository.RepositoryArn; }
        }

        public string RepositoryName
        {
            get { return ViewModel.RepositoryName; }
        }

        private RepositoryViewModel ViewModel
        {
            get
            {
                if (_viewModel == null)
                {
                    _viewModel = this.FeatureViewModel as RepositoryViewModel;
                    if (_viewModel == null)
                        throw new InvalidOperationException("Expected RepositoryViewModel type for FeatureViewModel");
                }

                return _viewModel;
            }
        }

        protected override void DisplayView()
        {
            this._control = new ViewRepositoryControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            var repositoryViewModel = this.FeatureViewModel as RepositoryViewModel;
            if (repositoryViewModel == null)
                throw new InvalidOperationException("Expected RepositoryViewModel type for FeatureViewModel");

            ECRClient = repositoryViewModel.ECRClient;

            try
            {
                this.Refresh();
            }
            catch (Exception e)
            {
                var msg = "Failed to query details for repository with ARN " + RepositoryArn;
                LOGGER.Error(msg, e);
                ToolkitFactory.Instance.ShellProvider.ShowError(msg, "Resource Query Failure");
            }
        }

        public void Refresh()
        {
            var request = new DescribeRepositoriesRequest
            {
                RepositoryNames = new List<string>
                {
                    RepositoryName
                }
            };

            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)request).AddBeforeRequestHandler(AWSToolkit.Constants.AWSExplorerDescribeUserAgentRequestEventHandler);
            var response = this.ECRClient.DescribeRepositories(request);
            if (response.Repositories.Count != 1)
                throw new Exception("Failed to find repository for ARN: " + RepositoryArn);

            if (this.Model.Repository == null)
                this.Model.Repository = new RepositoryWrapper(response.Repositories[0]);
            else
                this.Model.Repository.LoadFrom(response.Repositories[0]);

            this.LoadImagesCollection();
        }

        public void LoadImagesCollection()
        {
            System.Threading.Tasks.Task.Run(() => this.QueryRepositoryImages()).ContinueWith(x =>
            {
                if (x.Exception == null)
                {
                    UpdateModelImagesCollection(x.Result);
                }
            });
        }

        public IList<ImageDetailWrapper> QueryRepositoryImages()
        {
            var images = new List<ImageDetailWrapper>();

            var request = new DescribeImagesRequest
            {
                RepositoryName = RepositoryName
            };

            do
            {
                var response = this.ECRClient.DescribeImages(request);
                request.NextToken = response.NextToken;

                foreach (var i in response.ImageDetails)
                {
                    images.Add(new ImageDetailWrapper(i));
                }

            } while (!string.IsNullOrEmpty(request.NextToken));

            return images;
        }

        public void UpdateModelImagesCollection(ICollection<ImageDetailWrapper> images)
        {
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((System.Action) (() =>
            {
                this.Model.Repository.SetImages(images);
            }));
        }
    }
}
