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
    public class ElasticIPsViewModel : FeatureViewModel
    {
        public ElasticIPsViewModel(ElasticIPsViewMetaNode metaNode, EC2ServiceViewModel viewModel)
            : base(metaNode, viewModel, "Elastic IPs")
        {
        }

        public override string ToolTip
        {
            get
            {
                return "Create and associate elastic ips to EC2 instances";
            }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.elastic-ip.png";
            }
        }
    }
}
