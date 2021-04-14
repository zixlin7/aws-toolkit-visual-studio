using System;
using System.Reflection;

namespace AWSToolkit.Tests.Credentials.IO
{
    public static class ReflectionHelpers
    {
        private static readonly BindingFlags InstanceFlags =
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        private static readonly BindingFlags StaticFlags =
            BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

        /// <summary>
        /// Invoke an instance member on the object.
        /// Looks for a method, property, or field (in that order) on the object and calls it with the parameters provided.
        /// For properties and fields 0 parameters means get, and 1 parameter means set.
        /// For sets and void method calls, returns null.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="memberName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object Invoke(object obj, string memberName, params object[] parameters)
        {
            return InvokeHelper(obj.GetType(), obj, memberName, parameters);
        }

        /// <summary>
        /// Invoke a static member on the type.
        /// Looks for a method, property, or field (in that order) on the type and calls it with the parameters provided.
        /// For properties and fields 0 parameters means get, and 1 parameter means set.
        /// For sets and void method calls, returns null.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="memberName"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public static object Invoke(Type type, string memberName, params object[] parameters)
        {
            return InvokeHelper(type, null, memberName, parameters);
        }

        private static object InvokeHelper(Type type, object target, string memberName, object[] parameters)
        {
            BindingFlags flags = target == null ? StaticFlags : InstanceFlags;

            var method = type.GetMethod(memberName, flags);
            if (method == null)
            {
                var property = type.GetProperty(memberName, flags);
                if (property == null)
                {
                    var field = type.GetField(memberName, flags);
                    if (parameters.Length == 0)
                    {
                        // call the getter
                        return field.GetValue(target);
                    }
                    else if (parameters.Length == 1)
                    {
                        // call the setter
                        field.SetValue(target, parameters[0]);
                        return null;
                    }
                    else
                    {
                        throw new Exception("You can only call a field with 0 or 1 parameters.");
                    }
                }
                else
                {
                    if (parameters.Length == 0)
                    {
                        // call the getter
                        return property.GetValue(target, null);
                    }
                    else if (parameters.Length == 1)
                    {
                        // call the setter
                        property.SetValue(target, parameters[0], null);
                        return null;
                    }
                    else
                    {
                        throw new Exception("You can only call a property with 0 or 1 parameters.");
                    }
                }
            }
            else
            {
                return method.Invoke(target, parameters);
            }
        }
    }
}
