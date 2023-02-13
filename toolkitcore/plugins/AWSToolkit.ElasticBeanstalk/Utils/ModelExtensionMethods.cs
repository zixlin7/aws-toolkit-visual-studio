using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Utils
{
    public static class ModelExtensionMethods
    {
        /// <summary>
        /// Maps the AWS SDK model of a Beanstalk Environment to a Toolkit model.
        /// </summary>
        public static BeanstalkEnvironmentModel AsBeanstalkEnvironmentModel(
            this EnvironmentDescription environmentDescription)
        {
            var model = new BeanstalkEnvironmentModel(
                id: environmentDescription.EnvironmentId,
                name: environmentDescription.EnvironmentName,
                applicationName:environmentDescription.ApplicationName,
                cname: environmentDescription.CNAME);

            return model;
        }
    }
}
