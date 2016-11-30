using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;

namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class PropertiesController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            BaseContextCommand controller = null;
            if (model is CloudFrontDistributionViewModel)
            {
                controller = new EditDistributionConfigController();
            }
            else if (model is CloudFrontStreamingDistributionViewModel)
            {
                controller = new EditStreamingDistributionConfigController();
            }

            if(controller == null)
                return new ActionResults().WithSuccess(false);

            return controller.Execute(model);
        }
    }
}
