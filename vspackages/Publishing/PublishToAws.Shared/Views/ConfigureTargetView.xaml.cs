using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// View panel that is shown while a user is configuring their publish target.
    /// Data bound to <see cref="PublishToAwsDocumentViewModel"/>
    /// </summary>
    public partial class ConfigureTargetView : UserControl
    {
        public ConfigureTargetView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// When user clicks on a category, scroll the settings list if necessary to make that category
        /// the top-most visible category.
        /// </summary>
        private void Category_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(sender is FrameworkElement senderElement)) { return; }

            var categoryGrouping = senderElement.DataContext;
            if (categoryGrouping == null) { return; }

            var categoryElement = SettingsList.ItemContainerGenerator.ContainerFromItem(categoryGrouping) as FrameworkElement;
            if (categoryElement == null)  { return; }

            // Scroll so that categoryElement is displayed as high in the list as possible
            var offset = categoryElement.TranslatePoint(new Point(), SettingsListScrollViewer);
            SettingsListScrollViewer.ScrollToVerticalOffset(SettingsListScrollViewer.VerticalOffset + offset.Y);
        }

        /// <summary>
        /// When the settings list is scrolled, make the top-most visible category the visually "selected" one
        /// in the navigation bar.
        /// </summary>
        private void SettingsListScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!(sender is ScrollViewer scrollViewer)) { return; }

            FrameworkElement settingGroupElement = GetFirstVisibleSettingGroupElement(scrollViewer);
            if (settingGroupElement != null)
            {
                CategoryNavigator.SelectedItem = settingGroupElement.DataContext;
            }
        }

        private FrameworkElement GetFirstVisibleSettingGroupElement(ScrollViewer scrollViewer)
        {
            foreach (var item in SettingsList.Items)
            {
                if (!(SettingsList.ItemContainerGenerator.ContainerFromItem(item) is FrameworkElement settingsElement)) { continue; }
                if (settingsElement.DataContext == null) { continue; }
                
                var scrolledOffset = settingsElement.TranslatePoint(new Point(0, settingsElement.ActualHeight), scrollViewer);

                // The first visible item will have a positive Y-offset
                // (Previous items will have a negative offset, because they are "above" what is displayed in the scroll viewer).
                if (scrolledOffset.Y > 0)
                {
                    return settingsElement;
                }
            }

            return null;
        }
    }
}
