using System;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class CopyAmiController : BulkChangeController<IAmazonEC2, ImageWrapper>
    {
        private string _sourceRegion;
        private RegionEndPointsManager.RegionEndPoints _destinationRegion;
        private AccountViewModel _account;

        public CopyAmiController(string sourceRegion, RegionEndPointsManager.RegionEndPoints destinationRegion, AccountViewModel account)
        {            
            _sourceRegion = sourceRegion;
            _destinationRegion = destinationRegion;
            _account = account;
        }

        protected override string Action => "Copy to Region";

        protected override string ConfirmMessage => string.Format("Are you sure you want to copy this image to {0}: ",_destinationRegion.DisplayName);

        protected override void PerformAction(IAmazonEC2 client, ImageWrapper instance)
        {
            // Create client for the destination region
            var endpoint = _destinationRegion.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);
            var config = new AmazonEC2Config();
            endpoint.ApplyToClientConfig(config);

            AmazonEC2Client ec2Client = new AmazonEC2Client(_account.Credentials, config);
            
            try
            {
                var copyImageRequest = new CopyImageRequest
                {
                    ClientToken = Guid.NewGuid().ToString(),
                    Description = instance.Description,
                    Name = instance.Name,
                    SourceImageId = instance.ImageId,
                    SourceRegion = _sourceRegion
                };
               var copyImageResponse = ec2Client.CopyImage(copyImageRequest);
               ToolkitFactory.Instance.ShellProvider.ShowMessage(
                   "Copy to Region",
                   string.Format("The copy operation has started, the destination AMI ID is {0}.",copyImageResponse.ImageId));
            }
            finally
            {
                ec2Client.Dispose();
            }
        }
    }
}
