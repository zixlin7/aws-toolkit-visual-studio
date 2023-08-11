using System.Collections;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Collections;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Behaviors
{
    /// <summary>
    /// Provides attachable behaviors to ListBox controls.
    /// </summary>
    public static class ListBox
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ListBox));

        #region SelectedItems behavior

        private const string _selectedItemsPropertyName = "SelectedItems";

        /// <summary>
        /// Makes ListBox.SelectedItems property bindable.
        /// </summary>
        /// <remarks>
        /// The ListBox.SelectedItems property is a CLR property, not a DependencyProperty and is therefore not
        /// capable of being a Binding target.  This behavior allows a binding to be set on the ListBox to simulate
        /// as if the SelectedItems list was bindable.  A binding on this property will keep an IList in the view
        /// model up to date with the SelectedItems list.
        ///
        /// Currently, only one way binding to source is supported today.  This class could be upgraded in the future
        /// to support two way binding if an INotifyCollectionChanged source is passed into the binding.
        /// </remarks>
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.RegisterAttached(
            _selectedItemsPropertyName,
            typeof(IList),
            typeof(ListBox),
            new PropertyMetadata(SelectedItems_PropertyChanged));

        public static IList GetSelectedItems(DependencyObject d)
        {
            return (IList) d.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject d, IList value)
        {
            d.SetValue(SelectedItemsProperty, value);
        }

        private static void SelectedItems_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listbox = d as System.Windows.Controls.ListBox;
            if (listbox == null)
            {
                _logger.Error("DependencyObject d is either null or not a ListBox");
                return;
            }

            listbox.SelectionChanged -= Listbox_SelectionChanged;
            listbox.SelectionChanged += Listbox_SelectionChanged;
        }

        private static void Listbox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = sender as System.Windows.Controls.ListBox;
            if (listbox == null)
            {
                _logger.Error("Object sender is either null or not a ListBox");
                return;
            }

            var binding = listbox.GetBindingExpression(SelectedItemsProperty);
            if (binding == null)
            {
                _logger.Error($"BindingExpression must be set on {nameof(ListBox)}.{_selectedItemsPropertyName}");
                return;
            }

            var source = binding.ResolvedSource;
            var sourceType = source?.GetType();
            var list = sourceType?.GetProperty(binding.ResolvedSourcePropertyName).GetValue(source) as IList;
            if (list == null)
            {
                _logger.Error($"{sourceType?.Name}.{binding.ResolvedSourcePropertyName} is either null or not an IList");
                return;
            }

            list.RemoveAll(e.RemovedItems);
            list.AddAll(e.AddedItems);

            binding.ValidateWithoutUpdate();
        }
        #endregion
    }
}
