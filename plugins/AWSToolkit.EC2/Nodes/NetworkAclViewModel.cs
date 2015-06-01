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
    public class NetworkAclViewModel : FeatureViewModel
    {
        public NetworkAclViewModel(NetworkAclViewMetaNode metaNode, VPCRootViewModel viewModel)
            : base(metaNode, viewModel, "Network ACLs")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Create and associate network acls with subnets.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.networkacl.png";
            }
        }
    }
}
