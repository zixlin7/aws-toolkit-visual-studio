using System;
using System.ComponentModel;
using System.Linq;

namespace Amazon.AWSToolkit
{
    public static class ExtensionMethods
    {
        public static string GetDescription(this Enum @this)
        {
            var type = @this.GetType();
            var name = @this.ToString();

            var attribute =
                type.GetMember(name)
                ?.FirstOrDefault()
                ?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                ?.FirstOrDefault() as DescriptionAttribute;

            return attribute?.Description ?? name;
        }
    }
}
