using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public interface IIAMRootViewModel : IServiceRootViewModel
    {
        IAmazonIdentityManagementService IAMClient { get; }

    }
}
