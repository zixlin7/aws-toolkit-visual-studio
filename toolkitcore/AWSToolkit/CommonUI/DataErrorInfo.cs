using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Mixin for ViewModels and Entities to provide INotifyDataErrorInfo support.  IDataErrorInfo support is provided, but INotifyDataErrorInfo is recommended.
    /// </summary>
    /// <remarks>
    /// Supports custom error objects, multiple errors per property, cross-property errors, and entity-level errors. Cross-property errors are
    /// errors that affect multiple properties. You can associate these errors with one or all of the affected properties, or you can treat them
    /// as entity-level errors. Entity-level errors are errors that either affect multiple properties or affect the entire entity without
    /// affecting a particular property.
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
        private static readonly string EntityLevelErrorKey = string.Empty;

        private readonly IDictionary<string, ISet<object>> _errorInfo = new Dictionary<string, ISet<object>>();

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

        public bool HasErrors => _errorInfo.Any(pair => pair.Value.Any());

        public IEnumerable GetErrors(string propertyName) => _errorInfo.TryGetValue(NormalizePropertyName(propertyName), out var error)
            ? error : Enumerable.Empty<object>();

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        protected void RaiseErrorsChanged(string propertyName)
        {
            ErrorsChanged?.Invoke(_sender, new DataErrorsChangedEventArgs(propertyName));
        }

        #endregion

        #region IDataErrorInfo

        string IDataErrorInfo.this[string columnName] => GetErrors(columnName).OfType<object>().FirstOrDefault()?.ToString() ?? string.Empty;

        string IDataErrorInfo.Error =>
            _errorInfo.TryGetValue(EntityLevelErrorKey, out var values) && values.Any() ?
                values.First()?.ToString() :
                _errorInfo.Values.FirstOrDefault()?.FirstOrDefault()?.ToString()
                ?? string.Empty;

        #endregion

        public void AddError(object error, params string[] propertyNames)
        {
            if (error == null)
            {
                return;
            }

            if (propertyNames == null || !propertyNames.Any())
            {
                propertyNames = new[] { EntityLevelErrorKey };
            }

            foreach (var propertyName in propertyNames.Select(NormalizePropertyName))
            {
                if (!_errorInfo.TryGetValue(propertyName, out var errors))
                {
                    // Once a property is added and given an errors set, don't ever remove it to simplify code,
                    // avoid race conditions, and be consistent with ObservableValidator.
                    errors = new HashSet<object>();
                    _errorInfo[propertyName] = errors;
                }

                errors.Add(error);
                RaiseErrorsChanged(propertyName);
            }
        }

        public void RemoveError(object error)
        {
            if (error == null)
            {
                return;
            }

            foreach (var pair in _errorInfo)
            {
                if (pair.Value.Remove(error))
                {
                    RaiseErrorsChanged(pair.Key);
                }
            }
        }

        public void ClearErrors(params string[] propertyNames)
        {
            if (propertyNames == null || !propertyNames.Any())
            {
                propertyNames = _errorInfo.Keys.ToArray();
            }

            foreach (var propertyName in propertyNames.Select(NormalizePropertyName))
            {
                if (_errorInfo.TryGetValue(propertyName, out var errors) && errors.Any())
                {
                    errors.Clear();
                    RaiseErrorsChanged(propertyName);
                }
            }
        }

        private static string NormalizePropertyName(string propertyName) => propertyName ?? EntityLevelErrorKey;
    }
}
