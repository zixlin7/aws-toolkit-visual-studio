using System;

namespace Amazon.AWSToolkit.CommonUI
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AssociatedIconAttribute : Attribute
    {
        public AssociatedIconAttribute(bool isIconDynamic, string iconPath)
        {
            IsIconDynamic = isIconDynamic;
            IconPath = iconPath;
        }

        public bool IsIconDynamic
        {
            get;
        }

        public string IconPath
        {
            get;
        }
    }
}
