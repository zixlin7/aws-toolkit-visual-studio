using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2KeyPairsViewModel : FeatureViewModel
    {
        public EC2KeyPairsViewModel(EC2KeyPairsViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "Key Pairs")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Manage EC2 key pairs and store private keys";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.key-pairs.gif";
            }
        }
    }
}
