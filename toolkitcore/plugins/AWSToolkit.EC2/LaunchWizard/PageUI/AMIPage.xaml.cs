using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Threading;


using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Utils;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageUI
{
    /// <summary>
    /// Interaction logic for AMIPage.xaml; loosely 'derived' from full-page
    /// ViewAMIsControl editor view
    /// </summary>
    public partial class AMIPage : INotifyPropertyChanged
    {
        const string LOADING_TEXT = "Loading, please wait...";
        Guid _lastTextFilterChangeToken;
        readonly CommonImageFilters _initialFilter;

        public delegate void ImageListRefreshCallback(bool fullRefresh);

        public AMIPage()
        {
            InitializeComponent();
            this._ctlCommonFilters.ItemsSource = CommonImageFilters.AllFilters;
            this._ctlPlatformFilters.ItemsSource = PlatformPicker.AllPlatforms;
        }

        public AMIPage(IAWSWizardPageController controller, CommonImageFilters initialFilter)
            : this()
        {
            this.PageController = controller;
            this._initialFilter = initialFilter;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void BindModel(ViewAMIsModel model)
        {
            this.DataContext = model;
        }

        public ImageListRefreshCallback OnRequestImageListRefresh { get; set; }

        public ImageWrapper SelectedAMI
        {
            get
            {
                var selection = _amiSelector.SelectedItem;
                if (selection != null)
                    return selection as ImageWrapper;

                return null;
            }
        }

        public string SelectedAMIID
        {
            get
            {
                var selectedAmi = SelectedAMI;
                if (selectedAmi != null)
                    return (selectedAmi as ImageWrapper).ImageId;

                return string.Empty;
            }
        }

        public void SetSelectedAMI(ImageWrapper ami)
        {
            DataGridHelper.SelectAndScrollIntoView(this._amiSelector, ami);
        }

        void AmiSelector_SelectionChanged(object sender, RoutedEventArgs evnt)
        {
            try
            {
                NotifyPropertyChanged("ami");
            }
            catch (Exception e)
            {
                PageController.HostingWizard.Logger.Error(GetType().FullName + ", caught exception on ami selection change", e);
            }
        }

        void OnAmiSelectorLoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        void onFilterChanged(object sender, SelectionChangedEventArgs e)
        {
            // This is a check so we don't get a second load when the DataContext
            // is set
            if (!this.IsEnabled)
                return;

            ThreadPool.QueueUserWorkItem(this.asyncRefresh, new LoadState(true, false));
        }

        void onTextFilterChange(object sender, TextChangedEventArgs e)
        {
            // This is a check so we don't get a second load when the DataContext
            // is set
            if (!this.IsEnabled)
                return;

            this._lastTextFilterChangeToken = Guid.NewGuid();
            ThreadPool.QueueUserWorkItem(this.asyncRefresh, new LoadState(this._lastTextFilterChangeToken, false, false));
        }

        void asyncRefresh(object state)
        {
            if (!(state is LoadState))
                return;
            LoadState loadState = (LoadState)state;
            if (loadState.DisplayWaitState)
                displayWaitState();

            try
            {
                if (loadState.LastTextFilterChangeToken != Guid.Empty)
                    Thread.Sleep(750);

                System.Diagnostics.Debug.Assert(OnRequestImageListRefresh != null, "Refresh request delegate not hooked up in page controller");
                if (loadState.LastTextFilterChangeToken == Guid.Empty || this._lastTextFilterChangeToken == loadState.LastTextFilterChangeToken)
                {
                    this.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        OnRequestImageListRefresh(loadState.FullRefresh);
                    }));
                }
            }
            finally
            {
                if (loadState.DisplayWaitState)
                    clearWaitState();
            }
        }

        void displayWaitState()
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.IsEnabled = false;
                this._ctlLoadingMessage.Text = LOADING_TEXT;
            }));
        }

        void clearWaitState()
        {
            this.Dispatcher.BeginInvoke((Action)(() =>
            {
                this.IsEnabled = true;
                this._ctlLoadingMessage.Text = "";
            }));
        }

        class LoadState
        {
            public LoadState(bool displayWaitState, bool fullRefresh)
            {
                this.LastTextFilterChangeToken = Guid.Empty;
                this.DisplayWaitState = displayWaitState;
                this.FullRefresh = fullRefresh;
            }

            public LoadState(Guid lastTextFilterChangeToken, bool displayWaitState, bool fullRefresh)
            {
                this.LastTextFilterChangeToken = lastTextFilterChangeToken;
                this.DisplayWaitState = displayWaitState;
                this.FullRefresh = fullRefresh;
            }

            public Guid LastTextFilterChangeToken
            {
                get;
            }

            public bool DisplayWaitState
            {
                get;
            }

            public bool FullRefresh
            {
                get;
            }
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            if (this._initialFilter != null)
            {
                this._ctlCommonFilters.SelectedValue = this._initialFilter;
            }
        }
    }
}
