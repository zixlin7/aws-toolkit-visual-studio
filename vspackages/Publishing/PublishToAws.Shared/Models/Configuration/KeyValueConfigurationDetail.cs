using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

using Amazon.AWSToolkit.Models;
using Amazon.AWSToolkit.ViewModels;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    [DebuggerDisplay("{Id} (Key Values)")]
    public class KeyValueConfigurationDetail : ConfigurationDetail
    {
        private ICommand _editCommand;
        private readonly KeyValuesViewModel _keyValues = new KeyValuesViewModel();

        public ICommand EditCommand
        {
            get => _editCommand;
            set => SetProperty(ref _editCommand, value);
        }

        public KeyValuesViewModel KeyValues => _keyValues;

        public KeyValueConfigurationDetail()
        {
            PropertyChanged += KeyValueConfigurationDetail_PropertyChanged;
        }

        private void KeyValueConfigurationDetail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Value))
            {
                UpdateKeyValuesCollection();
            }
        }

        private void UpdateKeyValuesCollection()
        {
            try
            {
                if (!(Value is string json))
                {
                    // TODO : Newsonsoft objects are coming back from the client - work with devex to make this always json
                    json = Value?.ToString() ?? string.Empty;
                }

                var keyValues = KeyValuesConversion.FromJson(json);

                KeyValues.Collection = new ObservableCollection<KeyValue>(keyValues);
            }
            catch (Exception e)
            {
                // Bad json. Don't log -- this can be frequently called.
                Debug.Assert(!Debugger.IsAttached,
                    "Invalid publish settings json",
                    $"The deploy CLI might be producing bad JSON - {e.Message}");

                KeyValues.Collection = new ObservableCollection<KeyValue>();
            }
        }

        public void SetKeyValues(ICollection<KeyValue> keyValues)
        {
            Value = KeyValuesConversion.ToJson(keyValues);
        }
    }
}
