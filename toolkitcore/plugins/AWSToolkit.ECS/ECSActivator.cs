using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.ECS.Controller;

namespace Amazon.AWSToolkit.ECS
{
    public class ECSActivator : AbstractPluginActivator, IAWSECS
    {
        public override string PluginName => "ECS";



        public override void RegisterMetaNodes()
        {

        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSECS))
                return this;

            return null;
        }

        public void PublishContainerToAWS(Dictionary<string, object> seedProperties)
        {
            if (seedProperties == null)
                seedProperties = new Dictionary<string, object>();

            var controller = new PublishContainerToAWSController();
            controller.Execute(seedProperties);
        }
    }
}
