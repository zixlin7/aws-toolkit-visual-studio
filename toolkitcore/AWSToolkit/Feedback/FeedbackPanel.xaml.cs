using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Feedback
{
    /// <summary>
    /// Interaction logic for FeedbackPanel.xaml
    /// </summary>
    public partial class FeedbackPanel : BaseAWSControl
    {
        private readonly string _marker;

        public FeedbackPanel() : this("")
        {
        }

        public FeedbackPanel(string sourceMarker)
        {
            _marker = sourceMarker;

            InitializeComponent();
            Unloaded += OnUnloaded;
            DataContextChanged += OnDataContextChanged;
            DataContext = new FeedbackPanelViewModel();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= OnUnloaded;
            DataContextChanged -= OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is FeedbackPanelViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= OnViewModelChanged;
            }

            if (e.NewValue is FeedbackPanelViewModel newViewModel)
            {
                newViewModel.PropertyChanged += OnViewModelChanged;
            }
        }

        public override bool SupportsDynamicOKEnablement => true;

        public override string Title => CreateTitle("Feedback for AWS Toolkit for Visual Studio", _marker);

        public override string AcceptButtonText => "Submit";

        public override bool Validated()
        {
            return ViewModel.FeedbackSentiment.HasValue && !ViewModel.IsFeedbackCommentAboveLimit();
        }
        private FeedbackPanelViewModel ViewModel => DataContext as FeedbackPanelViewModel;

        private void OnViewModelChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ViewModel.FeedbackComment))
            {
                ViewModel.UpdateRemainingCharacters();
            }

            NotifyPropertyChanged(e.PropertyName);
        }


        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink link)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()));
                e.Handled = true;
            }
        }

        private string CreateTitle(string title, string marker)
        {
            if (!string.IsNullOrWhiteSpace(marker))
            {
                return $"{title} ({marker})";
            }

            return title;
        }
    }
}
