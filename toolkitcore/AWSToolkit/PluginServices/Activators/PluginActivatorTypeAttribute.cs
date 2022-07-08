using System;

namespace Amazon.AWSToolkit.PluginServices.Activators
{
    /// <summary>
    /// Used to define the plugin activator classes.
    /// This allows the Toolkit to locate plugin activators without reflecting across all types.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class PluginActivatorTypeAttribute : Attribute
    {
        public readonly Type PluginActivatorType;

        public PluginActivatorTypeAttribute(Type pluginActivatorType)
        {
            PluginActivatorType = pluginActivatorType;
        }
    }
}
