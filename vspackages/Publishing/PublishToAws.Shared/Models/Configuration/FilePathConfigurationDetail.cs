using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    public class FilePathConfigurationDetail : ConfigurationDetail, IDataErrorInfo
    {
        public static class TypeHintDataKeys
        {
            public const string CheckFileExists = "CheckFileExists";

            public const string Filter = "Filter";

            public const string Title = "Title";
        }

        private ICommand _browseCommand;

        public ICommand BrowseCommand
        {
            get => _browseCommand;
            set => SetProperty(ref _browseCommand, value);
        }

        private bool _checkFileExists;

        public bool CheckFileExists
        {
            get => _checkFileExists;
            set => SetProperty(ref _checkFileExists, value);
        }

        private string _filter;

        public string Filter
        {
            get => _filter;
            set => SetProperty(ref _filter, value);
        }

        private string _title;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        #region IDataErrorInfo

        private readonly IDictionary<string, string> _errors = new Dictionary<string, string>();

        string IDataErrorInfo.this[string columnName] => _errors.TryGetValue(columnName, out string value) ? value : string.Empty;

        string IDataErrorInfo.Error => _errors.Values.FirstOrDefault() ?? string.Empty;

        private static readonly StringConverter StringConverter = new StringConverter();

        private void ValidateProperty(string propertyName)
        {
            string valueKey = nameof(Value);
            string valueAsString = StringConverter.ConvertToString(Value);

            if (propertyName == valueKey)
            {
                _errors.Remove(valueKey);
                if (CheckFileExists && !String.IsNullOrWhiteSpace(valueAsString) && !File.Exists(valueAsString))
                {
                    _errors[valueKey] = "File must exist.";
                }
            }
        }

        #endregion

        public FilePathConfigurationDetail()
        {
            PropertyChanged += FilePathConfigurationDetail_PropertyChanged;
        }

        private void FilePathConfigurationDetail_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            ValidateProperty(e.PropertyName);
        }
    }
}
