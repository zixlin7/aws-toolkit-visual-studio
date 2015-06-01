using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2.Model
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class AssociatedIconAttribute : System.Attribute
    {
        public AssociatedIconAttribute(bool isIconDynamic, string iconPath)
        {
            IsIconDynamic = isIconDynamic;
            IconPath = iconPath;
        }

        public bool IsIconDynamic
        {
            get;
            private set;
        }

        public string IconPath
        {
            get;
            private set;
        }
    }
}
