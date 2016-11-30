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
    public class RouteTableViewModel : FeatureViewModel
    {
        public RouteTableViewModel(RouteTableViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Route Tables")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Create and associate route tables with subnets.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.route-table.png";
            }
        }
    }
}
