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
    public class EC2AMIsViewModel : FeatureViewModel
    {
        public EC2AMIsViewModel(EC2AMIsViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "AMIs")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "View and manage Amazon Machine Images and create new EC2 instances from Amazon Machine Images";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.ami.png";
            }
        }
    }
}
