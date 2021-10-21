using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Toolkit abstraction around custom Aws images that are used across the toolkit
    /// </summary>
    public class ToolkitImages
    {
        public static ImageSource Aws => ImageSourceFactory.GetImageSource(AwsImageResourcePath.Aws.Path);

        public static ImageSource AppRunner =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.AppRunner.Path);

        public static ImageSource CloudFormation =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.CloudFormation.Path);

        public static ImageSource CloudFront => ImageSourceFactory.GetImageSource(AwsImageResourcePath.CloudFront.Path);

        public static ImageSource CloudWatch => ImageSourceFactory.GetImageSource(AwsImageResourcePath.CloudWatch.Path);

        public static ImageSource CodeArtifact =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.CodeArtifact.Path);

        public static ImageSource CodeCommit => ImageSourceFactory.GetImageSource(AwsImageResourcePath.CodeCommit.Path);

        public static ImageSource DynamoDb => ImageSourceFactory.GetImageSource(AwsImageResourcePath.DynamoDb.Path);

        public static ImageSource Ec2 => ImageSourceFactory.GetImageSource(AwsImageResourcePath.Ec2.Path);

        public static ImageSource ElasticBeanstalk =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.ElasticBeanstalk.Path);

        public static ImageSource ElasticContainerRegistry =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.ElasticContainerRegistry.Path);

        public static ImageSource ElasticContainerService =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.ElasticContainerService.Path);

        public static ImageSource IdentityAndAccessManagement =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.IdentityAndAccessManagement.Path);

        public static ImageSource Kinesis => ImageSourceFactory.GetImageSource(AwsImageResourcePath.Kinesis.Path);

        public static ImageSource Lambda => ImageSourceFactory.GetImageSource(AwsImageResourcePath.Lambda.Path);

        public static ImageSource Rds => ImageSourceFactory.GetImageSource(AwsImageResourcePath.Rds.Path);

        public static ImageSource SimpleDb => ImageSourceFactory.GetImageSource(AwsImageResourcePath.SimpleDb.Path);

        public static ImageSource SimpleNotificationService =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.SimpleNotificationService.Path);

        public static ImageSource SimpleQueueService =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.SimpleQueueService.Path);

        public static ImageSource SimpleStorageService =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.SimpleStorageService.Path);

        public static ImageSource VirtualPrivateCloud =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.VirtualPrivateCloud.Path);


        public static ImageSource CloudFormationStack =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.CloudFormationStack.Path);

        public static ImageSource CloudFormationTemplate =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.CloudFormationTemplate.Path);

        public static ImageSource CloudFrontDownloadDistribution =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.CloudFrontDownloadDistribution.Path);

        public static ImageSource CloudFrontStreamingDistribution =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.CloudFrontStreamingDistribution.Path);

        public static ImageSource CodeArtifactRepository =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.CodeArtifactRepository.Path);

        public static ImageSource CodeCommitRepository =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.CodeCommitRepository.Path);

        public static ImageSource DynamoDbTable =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.DynamoDbTable.Path);

        public static ImageSource Ec2Ami => ImageSourceFactory.GetImageSource(AwsImageResourcePath.Ec2Ami.Path);

        public static ImageSource Ec2ElasticIpAddress =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.Ec2ElasticIpAddress.Path);

        public static ImageSource Ec2Instances =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.Ec2Instances.Path);

        public static ImageSource Ec2KeyPairs =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.Ec2KeyPairs.Path);

        public static ImageSource Ec2SecurityGroup =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.Ec2SecurityGroup.Path);

        public static ImageSource Ec2Volumes => ImageSourceFactory.GetImageSource(AwsImageResourcePath.Ec2Volumes.Path);

        public static ImageSource EcrRepository =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.EcrRepository.Path);

        public static ImageSource EcsTaskDefinition =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.EcsTaskDefinition.Path);

        public static ImageSource ElasticBeanstalkApplication =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.ElasticBeanstalkApplication.Path);

        public static ImageSource ElasticBeanstalkEnvironment =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.ElasticBeanstalkEnvironment.Path);

        public static ImageSource ElasticContainerServiceCluster =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.ElasticContainerServiceCluster.Path);

        public static ImageSource IamRole => ImageSourceFactory.GetImageSource(AwsImageResourcePath.IamRole.Path);

        public static ImageSource IamUser => ImageSourceFactory.GetImageSource(AwsImageResourcePath.IamUser.Path);

        public static ImageSource IamUserGroup =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.IamUserGroup.Path);

        public static ImageSource RdsDbInstances =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.RdsDbInstances.Path);

        public static ImageSource RdsSecurityGroup =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.RdsSecurityGroup.Path);

        public static ImageSource RdsSubnetGroups =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.RdsSubnetGroups.Path);

        public static ImageSource SimpleDbTable =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.SimpleDbTable.Path);

        public static ImageSource SnsTopic => ImageSourceFactory.GetImageSource(AwsImageResourcePath.SnsTopic.Path);

        public static ImageSource SqsQueue => ImageSourceFactory.GetImageSource(AwsImageResourcePath.SqsQueue.Path);

        public static ImageSource VpcInternetGateway =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.VpcInternetGateway.Path);

        public static ImageSource VpcNetworkAccessControlList =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.VpcNetworkAccessControlList.Path);

        public static ImageSource VpcRouteTables =>
            ImageSourceFactory.GetImageSource(AwsImageResourcePath.VpcRouteTables.Path);

        public static ImageSource VpcVpcs => ImageSourceFactory.GetImageSource(AwsImageResourcePath.VpcVpcs.Path);

        public static ImageSource Megaphone => ImageSourceFactory.GetImageSource(AwsImageResourcePath.Megaphone.Path);
    }
}
