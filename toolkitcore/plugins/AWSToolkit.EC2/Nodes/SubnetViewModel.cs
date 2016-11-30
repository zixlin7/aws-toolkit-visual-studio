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
    public class SubnetViewModel : FeatureViewModel
    {
        public SubnetViewModel(SubnetViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Subnets")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Create and manage subnets.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.subnet.png";
            }
        }
    }
}
