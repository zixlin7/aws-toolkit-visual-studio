using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using Amazon.ECR;
using Amazon.ECR.Model;

using log4net;

namespace Amazon.AWSToolkit.ECS.Controller
{
    public class ViewRepositoryController : BaseConnectionContextCommand
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(ViewRepositoryController));

        public ViewRepositoryModel Model { get; }

        public string RepositoryArn => Model.Repository.RepositoryArn;
        public string RepositoryName => Model.Repository.Name;

        private ViewRepositoryControl _control;
        private readonly IAmazonECR _ecrClient;

        public ViewRepositoryController(ViewRepositoryModel model,
            AwsConnectionSettings connectionSettings,
            ToolkitContext toolkitContext,
            IAmazonECR ecrClient)
            : base(toolkitContext, connectionSettings)
        {
            _ecrClient = ecrClient;
            Model = model;
        }

        public override ActionResults Execute()
        {
            _control = new ViewRepositoryControl(this);
            _toolkitContext.ToolkitHost.OpenInEditor(_control);

            return new ActionResults().WithSuccess(true);
        }

        public void LoadModel()
        {
            try
            {
                this.Refresh();
            }
            catch (Exception e)
            {
                var msg = "Failed to query details for repository with ARN " + RepositoryArn;
                Logger.Error(msg, e);
                _toolkitContext.ToolkitHost.ShowError(msg, "Resource Query Failure");
            }
        }

        public void Refresh()
        {
            var request = new DescribeRepositoriesRequest { RepositoryNames = new List<string> { RepositoryName } };

            ((Amazon.Runtime.Internal.IAmazonWebServiceRequest) request).AddBeforeRequestHandler(AWSToolkit.Constants
                .AWSExplorerDescribeUserAgentRequestEventHandler);
            var response = _ecrClient.DescribeRepositories(request);
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

            var request = new DescribeImagesRequest { RepositoryName = RepositoryName };

            do
            {
                var response = _ecrClient.DescribeImages(request);
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
            _toolkitContext.ToolkitHost.BeginExecuteOnUIThread((System.Action) (() =>
            {
                this.Model.Repository.SetImages(images);
            }));
        }
    }
}
