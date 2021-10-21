using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.Publish.Models
{
    /// <summary>
    /// Responsible for providing UI with a DataTemplate to be used as the "editor" control/UI
    /// for a published resource's data which may or may not contain a link
    ///
    /// </summary>
    public class PublishResourceTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextEditor { get; set; }

        public DataTemplate LinkEditor { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (!(item is string resourceData))
            {
                return null;
            }

            if (resourceData.StartsWith("http"))
            {
                return LinkEditor;
            }

            return TextEditor;
        }
    }
}
