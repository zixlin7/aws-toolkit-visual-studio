using System;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class CopyAmiController : BulkChangeController<IAmazonEC2, ImageWrapper>
    {
        private string _sourceRegion;
        private ToolkitRegion _destinationRegion;
        private AccountViewModel _account;

        public CopyAmiController(string sourceRegion, ToolkitRegion destinationRegion, AccountViewModel account)
        {            
            _sourceRegion = sourceRegion;
            _destinationRegion = destinationRegion;
            _account = account;
        }

        protected override string Action => "Copy to Region";

        protected override string ConfirmMessage => string.Format("Are you sure you want to copy this image to {0}: ", _destinationRegion.DisplayName);

        protected override void PerformAction(IAmazonEC2 client, ImageWrapper instance)
        {
            // Create client for the destination region
            var ec2Client = _account.CreateServiceClient<AmazonEC2Client>(_destinationRegion);

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
                   string.Format("The copy operation has started, the destination AMI ID is {0}.", copyImageResponse.ImageId));
            }
            finally
            {
                ec2Client.Dispose();
            }
        }
    }
}
