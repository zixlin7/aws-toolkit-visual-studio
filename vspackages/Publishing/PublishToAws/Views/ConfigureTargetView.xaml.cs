using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// View panel that is shown while a user is configuring their publish target.
    /// Data bound to <see cref="PublishToAwsDocumentViewModel"/>
    /// </summary>
    public partial class ConfigureTargetView : UserControl
    {
        private PublishToAwsDocumentViewModel _viewModel;

        public ConfigureTargetView()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            _viewModel = DataContext as PublishToAwsDocumentViewModel;

            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
                InitializeConfigurationDetailsView();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            }

            Loaded -= OnLoaded;
            Unloaded -= OnUnloaded;
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PublishToAwsDocumentViewModel.ConfigurationDetails))
            {
                InitializeConfigurationDetailsView();
            }
        }

        /// <summary>
        /// Defines Grouping and Sorting for how the configuration properties are presented
        /// </summary>
        private void InitializeConfigurationDetailsView()
        {
            var configurationDetails = _viewModel?.ConfigurationDetails;
            if (configurationDetails != null)
            {
                var collectionView = CollectionViewSource.GetDefaultView(configurationDetails);

                collectionView.GroupDescriptions.Clear();
                collectionView.GroupDescriptions.Add(
                    new PropertyGroupDescription(nameof(ConfigurationDetail.Category)));

                collectionView.SortDescriptions.Clear();
                collectionView.SortDescriptions.Add(new SortDescription(nameof(ConfigurationDetail.Category),
                    ListSortDirection.Ascending));
                collectionView.SortDescriptions.Add(new SortDescription(nameof(ConfigurationDetail.Name),
                    ListSortDirection.Ascending));

                collectionView.Filter = ConfigurationDetailsFilter;
            }
        }

        /// <summary>
        /// Determines which <see cref="ConfigurationDetail"/> objects to show in the view
        /// </summary>
        private bool ConfigurationDetailsFilter(object obj)
        {
            // Show configuration details flagged as visible
            return obj is ConfigurationDetail detail && detail.Visible;
        }
    }
}
