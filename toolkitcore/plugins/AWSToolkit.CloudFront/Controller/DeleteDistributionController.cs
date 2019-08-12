using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.CloudFront.Model;


namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class DeleteDistributionController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            CloudFrontBaseDistributionViewModel distributionModel = model as CloudFrontBaseDistributionViewModel;
            if (distributionModel == null)
                return new ActionResults().WithSuccess(false);

            string msg = string.Format("Are you sure you want to delete the {0} distribution?", model.Name);
            if (ToolkitFactory.Instance.ShellProvider.Confirm("Delete Distribution", msg))
            {
                try
                {
                    string etag = distributionModel.GetETag();
                    if (etag == null)
                        throw new ApplicationException("Failed to retrieve etag for distribution.");

                    if (distributionModel is CloudFrontDistributionViewModel)
                    {
                        var request = new DeleteDistributionRequest()
                        {
                            Id = distributionModel.DistributionId,
                            IfMatch = etag
                        };
                        distributionModel.CFClient.DeleteDistribution(request);
                    }
                    else
                    {
                        var request = new DeleteStreamingDistributionRequest()
                        {
                            Id = distributionModel.DistributionId,
                            IfMatch = etag
                        };
                        distributionModel.CFClient.DeleteStreamingDistribution(request);
                    }
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting distribution: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
                return new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(model.Name)
                    .WithShouldRefresh(true);
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
