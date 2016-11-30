using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;

using Amazon.EC2;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class InternetGatewayViewModel : FeatureViewModel
    {
        public InternetGatewayViewModel(InternetGatewayViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Internet Gateways")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Create and associate internet gateways to vpcs";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.internet-gateway.png";
            }
        }
    }
}
