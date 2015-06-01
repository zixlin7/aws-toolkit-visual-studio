using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Navigator
{
    public abstract class BaseContextCommand : IContextCommand
    {
        public abstract ActionResults Execute(IViewModel model);
    }
}
