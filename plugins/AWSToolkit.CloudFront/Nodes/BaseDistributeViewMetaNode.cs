using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CloudFront.Nodes
{
    public abstract class BaseDistributeViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnProperties
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnDeleteDistribution
        {
            get;
            set;
        }

        public void OnDeleteResponse(IViewModel focus, ActionResults results)
        {
            CloudFrontBaseDistributionViewModel distributionModel = focus as CloudFrontBaseDistributionViewModel;
            distributionModel.CloudFrontRootViewModel.RemoveDistribution(results.FocalName as string);
        }
    }
}
