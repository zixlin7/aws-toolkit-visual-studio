using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CommonUI
{
    public delegate void PropertySourceChange(object sender, bool forceShow, System.Collections.IList propertyObjects);
    public interface IPropertySupport
    {        
        event PropertySourceChange OnPropertyChange;
    }
}
