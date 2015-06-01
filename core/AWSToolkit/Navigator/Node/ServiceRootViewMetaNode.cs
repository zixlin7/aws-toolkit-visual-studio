using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class ServiceRootViewMetaNode : AbstractMetaNode, IServiceRootViewMetaNode
    {
        public override bool SupportsEndPoint
        {
            get
            {
                return true;
            }
        }

        public abstract ServiceRootViewModel CreateServiceRootModel(AccountViewModel account);

        public abstract string MarketingWebSite
        {
            get;
        }
    }
}
