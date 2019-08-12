using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class DeregisterAMIController : BulkChangeController<IAmazonEC2, ImageWrapper>
    {
        protected override string Action => "De-register";

        protected override string ConfirmMessage => "Are you sure you want to de-register the image(s):";

        protected override void PerformAction(IAmazonEC2 ec2Client, ImageWrapper image)
        {
            ec2Client.DeregisterImage(new DeregisterImageRequest() { ImageId = image.NativeImage.ImageId });
        }
    }
}
