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
    public class EC2VolumesViewModel : FeatureViewModel
    {
        public EC2VolumesViewModel(EC2VolumesViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "Volumes")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Create and manage EC2 volumes";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.volume.png";
            }
        }
    }
}
