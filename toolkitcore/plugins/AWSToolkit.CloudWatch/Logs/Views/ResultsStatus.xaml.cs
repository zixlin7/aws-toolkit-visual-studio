using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.CloudWatch.Logs.Models;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Views
{
    /// <summary>
    /// Indicates status of results eg. error, no results
    /// </summary>
    public partial class ResultsStatus : UserControl
    {
        public ResultsStatus()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage), typeof(string), typeof(ResultsStatus),
                new PropertyMetadata(null));

        public static readonly DependencyProperty FilterTextProperty =
            DependencyProperty.Register(
                nameof(FilterText), typeof(string), typeof(ResultsStatus),
                new PropertyMetadata(null));


        public static readonly DependencyProperty LoadingLogsProperty =
            DependencyProperty.Register(
                nameof(LoadingLogs), typeof(bool), typeof(ResultsStatus),
                new PropertyMetadata(null));

        public static readonly DependencyProperty HasInitializedProperty =
            DependencyProperty.Register(
                nameof(HasInitialized), typeof(bool), typeof(ResultsStatus),
                new PropertyMetadata(null));

        public static readonly DependencyProperty NoResultSuggestionsProperty =
            DependencyProperty.Register(
                nameof(NoResultSuggestions), typeof(ObservableCollection<Suggestion>), typeof(ResultsStatus),
                new PropertyMetadata(new ObservableCollection<Suggestion>()));


        public string ErrorMessage
        {
            get => (string) GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public bool LoadingLogs
        {
            get => (bool) GetValue(LoadingLogsProperty);
            set => SetValue(LoadingLogsProperty, value);
        }

        public bool HasInitialized
        {
            get => (bool) GetValue(HasInitializedProperty);
            set => SetValue(HasInitializedProperty, value);
        }

        public string FilterText
        {
            get => (string) GetValue(FilterTextProperty);
            set => SetValue(FilterTextProperty, value);
        }

        public ObservableCollection<Suggestion> NoResultSuggestions
        {
            get => (ObservableCollection<Suggestion>) GetValue(NoResultSuggestionsProperty);
            set => SetValue(NoResultSuggestionsProperty, value);
        }
    }
}
