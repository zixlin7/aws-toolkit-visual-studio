using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

using Newtonsoft.Json;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    [DebuggerDisplay("{Id} (List)")]
    public class ListConfigurationDetail : ConfigurationDetail
    {
        private ICommand _editCommand;

        private IList<string> _valueDeserialized;

        private ICollection<ListItem> _items;

        private ICollection<ListItem> _selectedItems;

        public ICommand EditCommand
        {
            get => _editCommand;
            set => SetProperty(ref _editCommand, value);
        }

        public ICollection<ListItem> Items
        {
            get => _items;
            set => SetProperty(ref _items, value);
        }

        public ICollection<ListItem> SelectedItems
        {
            get => _selectedItems;
            set => SetProperty(ref _selectedItems, value);
        }

        public ListConfigurationDetail()
        {
            PropertyChanged += ListConfigurationDetail_PropertyChanged;
        }

        private void ListConfigurationDetail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Value):
                    DeserializeValue();
                    TryUpdateSelectedItems();
                    break;
                case nameof(ValueMappings):
                    UpdateItems();
                    TryUpdateSelectedItems();
                    break;
            }
        }

        private void UpdateItems()
        {
            Items = new ObservableCollection<ListItem>(ValueMappings.Keys.Select(
                k => new ListItem(k, ValueMappings[k])));
        }

        private void TryUpdateSelectedItems()
        {
            // Value and ValueMappings are updated out of order and ValueMappings is updated twice, so need to defensively wait until
            // both properties have loaded stably before attempting to set SelectedItems.
            if (Items?.Count > 0 && _valueDeserialized != null)
            {
                SelectedItems = new ObservableCollection<ListItem>();

                foreach (var value in _valueDeserialized)
                {
                    // For the limited number of items passed at this time, this is sufficient, but could be optimized if/when greater numbers are expected
                    var item = Items.FirstOrDefault(i => i.Value == value);
                    if (item != null)
                    {
                        SelectedItems.Add(item);
                    }
                }
            }
        }

        private void DeserializeValue()
        {
            try
            {
                if (!(Value is string json))
                {
                    // Newtonsoft objects are coming back from the client - work with devex to make this always json
                    json = Value?.ToString() ?? string.Empty;
                }

                _valueDeserialized = JsonConvert.DeserializeObject<List<string>>(json ?? string.Empty) ?? new List<string>();
            }
            catch (Exception e)
            {
                // Bad json. Don't log -- this can be frequently called.

                // Deploy Tool should be sending a validation message to overwrite this, but in situations where
                // that does not happen, users at least know there is a problem with this setting.
                ValidationMessage = $"AWS Toolkit was unable to process this setting: {e.Message}";

                // Assume that an empty list was received, rather than leaving the state unassigned (null).
                // This way, users can get un-stuck instead of waiting for a deploy tool bug to get fixed.
                _valueDeserialized = new List<string>();

                Debug.Assert(!Debugger.IsAttached,
                    "Invalid publish settings json",
                    $"The deploy CLI might be producing bad JSON. Toolkit will assume it received an empty list for setting: {this.Name}\n\n{e.Message}");
            }
        }

        public void UpdateListValues()
        {
            Value = JsonConvert.SerializeObject(SelectedItems.Select(i => i.Value).ToArray());
        }

        public class ListItem : IEquatable<ListItem>
        {
            public bool Equals(ListItem other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return DisplayName == other.DisplayName && Value == other.Value;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((ListItem) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((DisplayName != null ? DisplayName.GetHashCode() : 0) * 397) ^ (Value != null ? Value.GetHashCode() : 0);
                }
            }

            public string DisplayName { get; }

            public string Value { get; }

            public ListItem(string displayName, string value)
            {
                DisplayName = displayName;
                Value = value;
            }
        }

        public override string GetSummaryValue()
        {
            if (SelectedItems == null) { return string.Empty; }

            return string.Join($",{Environment.NewLine}", SelectedItems.Select(x => x.DisplayName));
        }
    }
}
