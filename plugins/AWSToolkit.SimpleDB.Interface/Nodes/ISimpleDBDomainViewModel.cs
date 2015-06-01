using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.SimpleDB;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SimpleDB.Nodes
{
    public interface ISimpleDBDomainViewModel : IViewModel
    {
        string Domain { get; }
        IAmazonSimpleDB SimpleDBClient { get; }
    }
}
