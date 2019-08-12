using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.DesignData
{
    public class ApplicationEnvironmentsDesignTimeContext
    {
        readonly ObservableCollection<DeployedApplicationModel> _deployedApplications = new ObservableCollection<DeployedApplicationModel>();

        public ICollection<DeployedApplicationModel> ExistingDeployments => _deployedApplications;

        public ApplicationEnvironmentsDesignTimeContext()
        {
            var app1 = new DeployedApplicationModel("myMvcApplication");
            app1.Environments.Add(new EnvironmentDescription
                    {
                        EnvironmentName = "Development",
                        CNAME = "http://development.mymvcapp.elasticbeanstalk.com",
                        Status = EnvironmentStatus.Ready
                    });
            app1.Environments.Add(new EnvironmentDescription
                    {
                        EnvironmentName = "Staging",
                        CNAME = "http://staging.mymvcapp.elasticbeanstalk.com",
                        Status = EnvironmentStatus.Ready
                    });
            app1.Environments.Add(new EnvironmentDescription
                    {
                        EnvironmentName = "Prod",
                        CNAME = "http://mymvcapp.elasticbeanstalk.com",
                        Status = EnvironmentStatus.Ready
                    });

            var app2 = new DeployedApplicationModel("myOtherWebApplication");
            app2.Environments.Add(new EnvironmentDescription
                    {
                        EnvironmentName = "Development",
                        CNAME = "http://development.mymvcapp.elasticbeanstalk.com",
                        Status = EnvironmentStatus.Ready
                    });

            ExistingDeployments.Add(app1);
            ExistingDeployments.Add(app2);
        }   
    }
}
