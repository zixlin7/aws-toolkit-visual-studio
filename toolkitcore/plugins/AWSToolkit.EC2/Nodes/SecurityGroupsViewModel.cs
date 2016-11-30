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
    public class SecurityGroupsViewModel : FeatureViewModel
    {
        public SecurityGroupsViewModel(SecurityGroupsViewMetaNode metaNode, EC2ServiceViewModel viewModel)
            : base(metaNode, viewModel, "Security Groups")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Create and manage EC2 and VPC security groups.";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.security-groups.png";
            }
        }
    }
}
