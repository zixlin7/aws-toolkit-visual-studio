using System.Linq;
using System.Windows;
using System.Windows.Controls;

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
        /// Represents the editor control for an enum value
        /// </summary>
        public DataTemplate EnumEditor { get; set; }

        /// <summary>
        /// Represents the editor control for IAM Role properties
        /// </summary>
        public DataTemplate IamRoleEditor { get; set; }

        public DataTemplate VpcEditor { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is ConfigurationDetail configurationDetail))
            {
                return null;
            }

            if (configurationDetail.Type == typeof(object))
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

            if (configurationDetail.Type == typeof(string))
            {
                return TextEditor;
            }

            if (configurationDetail.Type == typeof(int) || configurationDetail.Type == typeof(double))
            {
                return NumericEditor;
            }

            if (configurationDetail.Type == typeof(bool))
            {
                return BooleanEditor;
            }

            return base.SelectTemplate(item, container);
        }
    }
}
