using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Amazon.AWSToolkit.Util
{
    public static class ReflectionUtils
    {
        static readonly object[] EMPTY_PARAMETERS = new object[] { };

        public static T GetpropertyValue<T>(string propertyName, object dataItem) where T : class
        {
            MethodInfo info = dataItem.GetType().GetMethod("get_" + propertyName);
            if (info == null)
                return null;

            return info.Invoke(dataItem, EMPTY_PARAMETERS) as T;
        }
    }
}
