﻿namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2VolumesViewModel : FeatureViewModel
    {
        public EC2VolumesViewModel(EC2VolumesViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "Volumes")
        {
        }

        public override string ToolTip => "Create and manage EC2 volumes";

        protected override string IconName => "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.volume.png";
    }
}
