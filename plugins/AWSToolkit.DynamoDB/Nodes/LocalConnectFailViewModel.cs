using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.DynamoDB.Nodes
{
    public class LocalConnectFailViewModel : AbstractViewModel
    {
        public LocalConnectFailViewModel(IViewModel parent, int port)
            : base(new ErrorMetaNode(), parent, string.Format("Failed to connect to DynamoDB Local at http://localhost:{0}", port))
        {
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.Resources.warning.png";
            }
        }
    }
}
