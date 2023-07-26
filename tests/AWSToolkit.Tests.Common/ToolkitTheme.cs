using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

using Amazon.AWSToolkit.Themes;

namespace Amazon.AWSToolkit.Tests.Common
{
    public class ToolkitTheme
    {
        /// <summary>
        /// Used by tests that instantiate UserControls which reference theme related resources.
        /// Tests don't typically have an Application.Current reference, so we create one for the
        /// purpose of the tests that need it.
        /// </summary>
        public static void SetupBaseTheme()
        {
            var app = new Application();

            var resourceDictionary = new ResourceDictionary
            {
                [ToolkitThemeResourceKeys.ButtonStyle] = new Style() { TargetType = typeof(Button) },
                [ToolkitThemeResourceKeys.FlowDocumentStyle] = new Style() { TargetType = typeof(FlowDocument) },
                [ToolkitThemeResourceKeys.ExpanderStyle] = new Style() { TargetType = typeof(Expander) },
            };

            app.Resources.MergedDictionaries.Add(resourceDictionary);
        }
    }
}
