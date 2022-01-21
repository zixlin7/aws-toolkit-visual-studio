using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Responsible for providing UI with a DataTemplate to be used as the "editor" control/UI
    /// for a configuration detail type.
    ///
    /// Each DataTemplate has access to an <see cref="ConfigurationDetail"/> object.
    /// </summary>
    public class PropertyTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// The editor control that renders nothing in the space where an editor would otherwise be, then renders any children it has
        /// </summary>
        public DataTemplate ParentEditor { get; set; }

        /// <summary>
        /// Represents the editor control for a numeric value
        /// </summary>
        public DataTemplate NumericEditor { get; set; }

        /// <summary>
        /// Represents the editor control for a text value
        /// </summary>
        public DataTemplate TextEditor { get; set; }

        /// <summary>
        /// Represents the editor control for a boolean value
        /// </summary>
        public DataTemplate BooleanEditor { get; set; }

        /// <summary>
        /// Represents a UI for properties the Toolkit doesn't know how to handle
        /// </summary>
        public DataTemplate UnsupportedTypeEditor { get; set; }

        /// <summary>
        /// Represents the editor control for an enum value
        /// </summary>
        public DataTemplate EnumEditor { get; set; }

        /// <summary>
        /// Represents the editor control for IAM Role properties
        /// </summary>
        public DataTemplate IamRoleEditor { get; set; }

        public DataTemplate VpcEditor { get; set; }

        public DataTemplate Ec2InstanceTypeEditor { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is ConfigurationDetail configurationDetail))
            {
                return null;
            }

            if (configurationDetail.Type == DetailType.Unsupported)
            {
                return UnsupportedTypeEditor;
            }

            if (configurationDetail.Type == DetailType.Blob)
            {
                if (configurationDetail.TypeHint == ConfigurationDetail.TypeHints.IamRole)
                {
                    return IamRoleEditor;
                }

                if (configurationDetail.TypeHint == ConfigurationDetail.TypeHints.Vpc)
                {
                    return VpcEditor;
                }

                // TODO : Add custom editors here based on configurationDetail.TypeHint

                // Show nothing -- the child details will be rendered by default
                return ParentEditor;
            }

            if (configurationDetail.ValueMappings?.Any() ?? false)
            {
                return EnumEditor;
            }

            if (configurationDetail.Type == DetailType.String)
            {
                if (configurationDetail.TypeHint == ConfigurationDetail.TypeHints.InstanceType)
                {
                    return Ec2InstanceTypeEditor;
                }

                return TextEditor;
            }

            if (configurationDetail.Type == DetailType.Integer || configurationDetail.Type == DetailType.Double)
            {
                return NumericEditor;
            }

            if (configurationDetail.Type == DetailType.Boolean)
            {
                return BooleanEditor;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
