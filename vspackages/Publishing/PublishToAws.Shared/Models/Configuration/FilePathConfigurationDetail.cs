using System.Windows.Input;

namespace Amazon.AWSToolkit.Publish.Models.Configuration
{
    public class FilePathConfigurationDetail : ConfigurationDetail
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
    }
}
