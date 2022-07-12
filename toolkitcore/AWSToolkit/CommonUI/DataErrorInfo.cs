using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Amazon.AWSToolkit.Collections;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Mixin for ViewModels and Entities to provide INotifyDataErrorInfo support.  IDataErrorInfo support is provided, but INotifyDataErrorInfo is recommended.
    /// </summary>
    /// <remarks>
    /// Supports custom error objects, multiple errors per property, cross-property errors, and entity-level errors. Cross-property errors are
    /// errors that affect multiple properties. Entity-level errors are all errors registered with this object, regardless of property association.
    ///
    /// IDataErrorInfo enables data entity classes to implement custom validation rules and expose validation results to the user interface.
    /// You typically implement this interface to provide relatively simple validation logic. To provide asynchronous or server-side validation logic,
    /// implement the INotifyDataErrorInfo interface instead. In general, new entity classes should implement INotifyDataErrorInfo for the added
    /// flexibility instead of implementing IDataErrorInfo. The IDataErrorInfo support enables you to use many existing entity classes that are written
    /// for the full.NET Framework.
    ///
    /// Implementation based on <see href="https://docs.microsoft.com/en-us/dotnet/api/system.componentmodel.inotifydataerrorinfo?view=netframework-4.7.2">
    /// INotifyDataErrorInfo interface</see> documentation and the <see href="https://github.com/CommunityToolkit/dotnet/blob/main/CommunityToolkit.Mvvm/ComponentModel/ObservableValidator.cs">
    /// Mvvm Toolkit's ObservableValidator</see> implementation of the INotifyDataErrorInfo interface.  Behaviors not clearly defined in the interface
    /// documentation follow those of the ObservableValidator implementation.
    /// </remarks>
    /// <seealso href="https://social.technet.microsoft.com/wiki/contents/articles/19490.wpf-4-5-validating-data-in-using-the-inotifydataerrorinfo-interface.aspx"/>
    public class DataErrorInfo : INotifyDataErrorInfo, IDataErrorInfo
    {
        private readonly IDictionary<string, ISet<object>> _propertyNameToErrors = new Dictionary<string, ISet<object>>();

        private readonly IDictionary<object, ISet<string>> _errorToPropertyNames = new Dictionary<object, ISet<string>>();

        private readonly object _sender;

        /// <summary>
        /// Creates a new instance of DataErrorInfo.
        /// </summary>
        /// <param name="sender">Supplied as the sender in ErrorsChanged events.</param>
        public DataErrorInfo(object sender)
        {
            _sender = sender;
        }

        #region INotifyDataErrorInfo

        public bool HasErrors => _errorToPropertyNames.Count > 0;

        /// <summary>
        /// Returns errors for the given property name or all errors if property name is null or an empty string.
        /// </summary>
        /// <param name="propertyName">Name of property to return errors for or null or empty string for all errors.</param>
        /// <returns>Errors for the given property name or all errors if property name is null or an empty string.</returns>
        public IEnumerable GetErrors(string propertyName)
        {
            return string.IsNullOrEmpty(propertyName) ?
                _errorToPropertyNames.Keys :
                _propertyNameToErrors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<object>();
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected void OnErrorsChanged(string propertyName)
        {
            OnErrorsChanged(new DataErrorsChangedEventArgs(propertyName));
        }

        protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e)
        {
            ErrorsChanged?.Invoke(_sender, e);
        }

        #endregion

        #region IDataErrorInfo

        string IDataErrorInfo.this[string columnName] => GetErrors(columnName).OfType<object>().FirstOrDefault()?.ToString() ?? string.Empty;

        string IDataErrorInfo.Error => _errorToPropertyNames.Keys.FirstOrDefault()?.ToString() ?? string.Empty;

        #endregion

        public void AddError(object error, params string[] propertyNames)
        {
            if (error == null)
            {
                return;
            }

            propertyNames = NormalizePropertyNames(propertyNames);

            if (propertyNames.Length == 0)
            {
                propertyNames = new [] { string.Empty };
            }

            if (!_errorToPropertyNames.TryGetValue(error, out var properties))
            {
                properties = new HashSet<string>();
                _errorToPropertyNames[error] = properties;
            }
            properties.AddAll(propertyNames);

            foreach (var propertyName in propertyNames)
            {
                if (!_propertyNameToErrors.TryGetValue(propertyName, out var errors))
                {
                    errors = new HashSet<object>();
                    _propertyNameToErrors[propertyName] = errors;
                }
                errors.Add(error);
                OnErrorsChanged(propertyName);
            }
        }

        public void RemoveError(object error)
        {
            if (error == null)
            {
                return;
            }

            if (_errorToPropertyNames.TryGetValue(error, out var properties))
            {
                _errorToPropertyNames.Remove(error);

                foreach (var property in properties)
                {
                    var errors = _propertyNameToErrors[property];
                    if (errors.Remove(error))
                    {
                        if (errors.Count == 0)
                        {
                            _propertyNameToErrors.Remove(property);
                        }

                        OnErrorsChanged(property);
                    }
                }
            }
        }

        public void ClearErrors(params string[] propertyNames)
        {
            propertyNames = NormalizePropertyNames(propertyNames);

            if (propertyNames.Length == 0)
            {
                propertyNames = _propertyNameToErrors.Keys.ToArray();
            }

            foreach (var propertyName in propertyNames)
            {
                if (_propertyNameToErrors.TryGetValue(propertyName, out var errors))
                {
                    foreach (var error in errors)
                    {
                        var properties = _errorToPropertyNames[error];
                        if (properties.Remove(propertyName) && properties.Count == 0)
                        {
                            _errorToPropertyNames.Remove(error);
                        }
                    }

                    _propertyNameToErrors.Remove(propertyName);
                    OnErrorsChanged(propertyName);
                }
            }
        }

        private static string[] NormalizePropertyNames(params string[] propertyNames)
        {
            return propertyNames?.Select(p => p ?? string.Empty).ToArray() ?? Array.Empty<string>();
        }
    }
}
