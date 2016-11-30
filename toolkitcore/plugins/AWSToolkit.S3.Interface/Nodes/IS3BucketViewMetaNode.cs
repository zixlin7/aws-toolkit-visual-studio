using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public interface IS3BucketViewMetaNode : IMetaNode
    {
        ActionHandlerWrapper.ActionHandler OnBrowse { get; set; }
        ActionHandlerWrapper.ActionHandler OnDelete { get; set; }
        ActionHandlerWrapper.ActionHandler OnEditPolicy { get; set; }
        ActionHandlerWrapper.ActionHandler OnProperties { get; set; }
        ActionHandlerWrapper.ActionHandler OnViewMultipartUploads { get; set; }
    }
}
