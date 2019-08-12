using System.Collections.ObjectModel;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.CloudFront.View;

using Amazon.CloudFront;
using Amazon.CloudFront.Model;


namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class ViewInvalidationRequestsController : BaseContextCommand
    {
        CloudFrontDistributionViewModel _viewModel;
        ViewInvalidationRequestsModel _model;
        IAmazonCloudFront _cfClient;

        public override ActionResults Execute(IViewModel model)
        {
            this._viewModel = model as CloudFrontDistributionViewModel;
            if (model == null)
                return new ActionResults().WithSuccess(false);

            this._cfClient = this._viewModel.CFClient;

            var control = new ViewInvalidationRequestsControl(this);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(control);

            return new ActionResults().WithSuccess(true);
        }

        public void LoadModel()
        {
            this._model = new ViewInvalidationRequestsModel();
            this._model.Summaries = new ObservableCollection<InvalidationSummaryWrapper>();
            Refresh();
        }

        public void Refresh()
        {
            RefreshInvalidationSummaries();
        }

        public string DistributionId => this._viewModel.DistributionId;

        void RefreshInvalidationSummaries()
        {
            this._model.Summaries.Clear();
            var response = this._cfClient.ListInvalidations(new ListInvalidationsRequest() { DistributionId = this._viewModel.DistributionId });

            foreach (InvalidationSummary summary in response.InvalidationList.Items)
            {
                this._model.Summaries.Add(new InvalidationSummaryWrapper(summary));
            }
        }

        public void RefreshPaths(InvalidationSummaryWrapper summary)
        {
            var response = this._cfClient.GetInvalidation(new GetInvalidationRequest()
            {
                Id = summary.Id,
                DistributionId = this._viewModel.DistributionId
            });

            summary.CreateTime = response.Invalidation.CreateTime;
            foreach (string path in response.Invalidation.InvalidationBatch.Paths.Items)
            {
                summary.Paths.Add(new InvalidationSummaryWrapper.InvalidationPath(path));
            }

        }

        public ViewInvalidationRequestsModel Model => this._model;
    }
}
