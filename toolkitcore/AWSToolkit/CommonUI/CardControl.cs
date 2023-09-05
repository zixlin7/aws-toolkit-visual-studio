using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Amazon.AWSToolkit.CommonUI
{
    public class CardControl : Selector
    {
        public CardControl()
        {
            Loaded += CardControl_Loaded;
        }

        private void CardControl_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= CardControl_Loaded;

            ChangeVisibility(Items, Visibility.Collapsed);
            ChangeVisibility(SelectedItem, Visibility.Visible);
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            ChangeVisibility(e.NewItems, Visibility.Collapsed);
            ChangeVisibility(SelectedItem, Visibility.Visible);
        }

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
                    ChangeVisibility(item, visibility);
                }
            }
        }

        private void ChangeVisibility(object item, Visibility visibility)
        {
            var uiElement = GetItemContainerAsUiElement(item);
            if (uiElement != null)
            {
                uiElement.Visibility = visibility;
            }
        }

        private UIElement GetItemContainerAsUiElement(object item)
        {
            return (IsItemItsOwnContainer(item) ? item : ItemContainerGenerator.ContainerFromItem(item)) as UIElement;
        }
    }
}
