using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Navigator
{
    public interface IContextCommand
    {
        ActionResults Execute(IViewModel model);
    }
}
