using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Themes;

namespace Amazon.AWSToolkit.CommonUI
{
    [TemplatePart(Name = PartAvailableListBox, Type = typeof(ListBox))]
    [TemplatePart(Name = PartSelectedListBox, Type = typeof(ListBox))]
    [TemplatePart(Name = PartAddButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartAddAllButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartRemoveButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartRemoveAllButton, Type = typeof(ButtonBase))]
    public class DualListBox : ListBox
    {
        private const string PartAvailableListBox = "PART_AvailableListBox";
        private const string PartSelectedListBox = "PART_SelectedListBox";
        private const string PartAddButton = "PART_AddButton";
        private const string PartAddAllButton = "PART_AddAllButton";
        private const string PartRemoveButton = "PART_RemoveButton";
        private const string PartRemoveAllButton = "PART_RemoveAllButton";

        private ListBox _availableListBox;
        private ListBox _selectedListBox;
        private ButtonBase _addButton;
        private ButtonBase _addAllButton;
        private ButtonBase _removeButton;
        private ButtonBase _removeAllButton;

        private static readonly DependencyPropertyKey AvailableItemsPropertyKey =
            DependencyProperty.RegisterReadOnly(
                nameof(AvailableItems),
                typeof(IList),
                typeof(DualListBox),
                new FrameworkPropertyMetadata((IList) null));

        internal static readonly DependencyProperty AvailableItemsProperty = AvailableItemsPropertyKey.DependencyProperty;

        private IList AvailableItems
        {
            get => (IList) GetValue(AvailableItemsProperty);
            set => SetValue(AvailableItemsPropertyKey, value);
        }

        static DualListBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DualListBox),
                new FrameworkPropertyMetadata(typeof(DualListBox)));
        }

        public DualListBox()
        {
            AvailableItems = new ObservableCollection<object>();
            SelectionMode = SelectionMode.Extended;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _availableListBox = GetTemplatePart<ListBox>(PartAvailableListBox);
            _selectedListBox = GetTemplatePart<ListBox>(PartSelectedListBox);
            _addButton = GetTemplatePart<ButtonBase>(PartAddButton);
            _addAllButton = GetTemplatePart<ButtonBase>(PartAddAllButton);
            _removeButton = GetTemplatePart<ButtonBase>(PartRemoveButton);
            _removeAllButton = GetTemplatePart<ButtonBase>(PartRemoveAllButton);

            _addButton.Click += PART_AddButton_OnClick;
            _addAllButton.Click += PART_AddAllButton_OnClick;
            _removeButton.Click += PART_RemoveButton_OnClick;
            _removeAllButton.Click += PART_RemoveAllButton_OnClick;
        }

        private T GetTemplatePart<T>(string partName) where T : class
        {
            return Template.FindName(partName, this) as T;
        }

        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);

            AvailableItems.RemoveAll(e.OldItems);
            AvailableItems.AddAll(e.NewItems);
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == ListBox.SelectionModeProperty && e.NewValue is SelectionMode &&
                ((SelectionMode) e.NewValue) == SelectionMode.Single)
            {
                throw new InvalidEnumArgumentException(
                    $"{nameof(DualListBox)} does not support {nameof(SelectionMode)}.{nameof(SelectionMode.Single)}.  Consider using a {nameof(ComboBox)} instead.");
            }

            base.OnPropertyChanged(e);
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            AvailableItems.AddAll(e.RemovedItems);
            AvailableItems.RemoveAll(e.AddedItems);
        }

        private IEnumerable ToSafeEnumerable(ICollection collection)
        {
            object[] array = new object[collection.Count];
            collection.CopyTo(array, 0);
            return array;
        }

        private void PART_AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedItems.AddAll(ToSafeEnumerable(_availableListBox.SelectedItems));
        }

        private void PART_AddAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            _availableListBox.SelectAll();
            SelectedItems.AddAll(ToSafeEnumerable(_availableListBox.SelectedItems));
        }

        private void PART_RemoveButton_OnClick(object sender, RoutedEventArgs e)
        {
            SelectedItems.RemoveAll(ToSafeEnumerable(_selectedListBox.SelectedItems));
        }

        private void PART_RemoveAllButton_OnClick(object sender, RoutedEventArgs e)
        {
            _selectedListBox.SelectAll();
            SelectedItems.RemoveAll(ToSafeEnumerable(_selectedListBox.SelectedItems));
        }
    }
}
