using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Models
{
    /// <summary>
    /// Represents a Key-Value pairing
    /// </summary>
    public class KeyValue : BaseModel, IEquatable<KeyValue>, INotifyDataErrorInfo
    {
        private string _key = string.Empty;
        private string _value = string.Empty;
        private bool _isDuplicateKey;

        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public bool IsDuplicateKey
        {
            get => _isDuplicateKey;
            set
            {
                SetProperty(ref _isDuplicateKey, value);
                RaiseErrorsChanged();
            }
        }

        public KeyValue() : this(string.Empty, string.Empty)
        {
        }

        public KeyValue(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public KeyValue(KeyValue keyValue)
        {
            Key = keyValue.Key;
            Value = keyValue.Value;
        }

        #region IEquatable

        public bool Equals(KeyValue other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return _key == other._key && _value == other._value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((KeyValue) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_key != null ? _key.GetHashCode() : 0) * 397) ^ (_value != null ? _value.GetHashCode() : 0);
            }
        }

        #endregion

        #region INotifyDataErrorInfo

        public bool HasErrors => IsDuplicateKey;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            var errors = new List<string>();

            if (propertyName == null || propertyName == nameof(Key))
            {
                if (IsDuplicateKey)
                {
                    errors.Add($"Duplicate Key: {Key}");
                }
            }

            return errors;
        }

        private void RaiseErrorsChanged()
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(null));
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Key)));
        }

        #endregion
    }
}
