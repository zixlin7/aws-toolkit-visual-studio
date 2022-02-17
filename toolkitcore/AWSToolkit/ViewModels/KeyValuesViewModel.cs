using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Models;

namespace Amazon.AWSToolkit.ViewModels
{
    public class KeyValuesViewModel : BaseModel, INotifyDataErrorInfo
    {
        private ObservableCollection<KeyValue> _collection;
        private string _batchAssignments = string.Empty;

        public string BatchAssignments
        {
            get => _batchAssignments;
            set
            {
                if (_batchAssignments == value) return;

                _batchAssignments = value;
                NotifyPropertyChanged(nameof(BatchAssignments));
                Collection = new ObservableCollection<KeyValue>(ParseKeyValues(value));
            }
        }

        public ObservableCollection<KeyValue> Collection
        {
            get => _collection;
            set
            {
                if (!IsKeyValuesEquivalent(value))
                {
                    UnListenForChanges(_collection);
                    _collection = value;
                    ListenForChanges(_collection);
                    NotifyPropertyChanged(nameof(Collection));
                    UpdateBatchAssignmentsText();
                    IdentifyDuplicateKeys();
                }
            }
        }

        public ICommand AddKeyValue { get; }
        public ICommand RemoveKeyValue { get; }

        public KeyValuesViewModel()
        {
            AddKeyValue = CreateAddCommand();
            RemoveKeyValue = CreateRemoveCommand();
            Collection = new ObservableCollection<KeyValue>();
        }

        private ICommand CreateAddCommand()
        {
            var command = new RelayCommand((param) =>
            {
                _collection.Add(new KeyValue());
            });

            return command;
        }

        private ICommand CreateRemoveCommand()
        {
            var command = new RelayCommand((param) =>
            {
                if (param is KeyValue kv)
                {
                    _collection.Remove(kv);
                }
            });

            return command;
        }

        private bool IsKeyValuesEquivalent(ObservableCollection<KeyValue> value)
        {
            if (_collection == value)
            {
                return true;
            }

            if (_collection == null || value == null)
            {
                return false;
            }

            if (_collection.Count != value.Count)
            {
                return false;
            }

            return Enumerable.Range(0, _collection.Count)
                .All(index => _collection[index].Equals(value[index]));
        }

        private void UnListenForChanges(ObservableCollection<KeyValue> keyValues)
        {
            if (keyValues == null)
            {
                return;
            }

            keyValues.CollectionChanged -= KeyValues_CollectionChanged;

            foreach (var keyValue in keyValues)
            {
                keyValue.PropertyChanged -= KeyValue_PropertyChanged;
            }
        }

        private void ListenForChanges(ObservableCollection<KeyValue> keyValues)
        {
            if (keyValues == null)
            {
                return;
            }

            foreach (var keyValue in keyValues)
            {
                keyValue.PropertyChanged += KeyValue_PropertyChanged;
            }

            keyValues.CollectionChanged += KeyValues_CollectionChanged;
        }

        private void KeyValues_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var keyValue in e.OldItems.OfType<KeyValue>())
                {
                    keyValue.PropertyChanged -= KeyValue_PropertyChanged;
                }
            }

            if (e.NewItems != null)
            {
                foreach (var keyValue in e.NewItems.OfType<KeyValue>())
                {
                    keyValue.PropertyChanged += KeyValue_PropertyChanged;
                }
            }

            UpdateBatchAssignmentsText();
            IdentifyDuplicateKeys();
        }

        private void KeyValue_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            UpdateBatchAssignmentsText();
            if (e.PropertyName == nameof(KeyValue.Key))
            {
                IdentifyDuplicateKeys();
            }
        }

        private void UpdateBatchAssignmentsText()
        {
            BatchAssignments = string.Join(Environment.NewLine,
                Collection.Select(KeyValueConversion.ToAssignmentString)) + Environment.NewLine;
        }

        private void IdentifyDuplicateKeys()
        {
            foreach (var keyGroup in Collection.GroupBy(keyValue => keyValue.Key))
            {
                bool isDuplicate = keyGroup.Count() > 1;
                keyGroup
                    .ToList()
                    .ForEach(keyValue => keyValue.IsDuplicateKey = isDuplicate);
            }

            RaiseErrorsChanged();
        }

        private static IEnumerable<KeyValue> ParseKeyValues(string batchAssignmentString)
        {
            return batchAssignmentString
                .Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                .Where(assignmentString => !string.IsNullOrWhiteSpace(assignmentString))
                .Select(KeyValueConversion.FromAssignmentString);
        }

        #region INotifyDataErrorInfo

        public bool HasErrors => Collection?.Any(kv => kv.HasErrors) ?? false;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            var errors = new List<object>();

            foreach (var keyValue in Collection)
            {
                errors.AddRange(keyValue.GetErrors(null).OfType<object>());
            }

            return errors.Distinct();
        }

        private void RaiseErrorsChanged()
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(null));
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(BatchAssignments)));
        }

        #endregion
    }
}
