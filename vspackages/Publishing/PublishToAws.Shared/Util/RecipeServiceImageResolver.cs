using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Publish.Util
{
    public class RecipeServiceImageResolver
    {
        /// <summary>
        /// Retrieves appropriate image/icon source<see cref="ImageSource"/> for the specified deployment recipe service
        /// </summary>
        /// <param name="service"></param>
        public static ImageSource GetServiceImage(string service)
        {
            switch (service)
            {
                case "Amazon Elastic Container Service":
                    return ToolkitImages.ElasticContainerService;
                case "Amazon S3":
                    return ToolkitImages.SimpleStorageService;
                case "AWS App Runner":
                    return ToolkitImages.AppRunner;
                case "AWS Elastic Beanstalk":
                    return ToolkitImages.ElasticBeanstalk;
                default:
                    return ToolkitImages.Aws;
            }
        }
    }
}
