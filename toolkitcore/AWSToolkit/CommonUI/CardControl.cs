using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Amazon.AWSToolkit.CommonUI
{
    public class CardControl : Selector
    {
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            ChangeVisibility(e.RemovedItems, Visibility.Collapsed);
            ChangeVisibility(e.AddedItems, Visibility.Visible); // Not derived from MultiSelector, so only one SelectedItem
        }

        private void ChangeVisibility(IEnumerable items, Visibility visibility)
        {
            if (items != null)
            {
                foreach (var item in items)
                {
                    var uiElement = GetItemContainerAsUiElement(item);
                    if (uiElement != null)
                    {
                        uiElement.Visibility = visibility;
                    }
                }
            }
        }

        private UIElement GetItemContainerAsUiElement(object item)
        {
            return (IsItemItsOwnContainer(item) ? item : ItemContainerGenerator.ContainerFromItem(item)) as UIElement;
        }
    }
}
