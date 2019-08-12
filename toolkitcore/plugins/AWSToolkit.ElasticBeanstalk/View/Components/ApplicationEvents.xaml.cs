using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;

using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for ApplicationEvents.xaml
    /// </summary>
    public partial class ApplicationEvents
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(ApplicationEvents));
        bool _turnedOffAutoScroll;
        Guid _lastTextFilterChangeToken;
        ApplicationStatusController _controller;

        public ApplicationEvents()
        {
            InitializeComponent();
        }

        public void Initialize(ApplicationStatusController controller)
        {
            this._controller = controller;
        }

        private void onLoad(object sender, RoutedEventArgs e)
        {
            if (!this._turnedOffAutoScroll)
            {
                DataGridHelper.TurnOffAutoScroll(this._ctlDataGrid);
                this._turnedOffAutoScroll = true;
            }
        }

        void onTextFilterChange(object sender, TextChangedEventArgs e)
        {
            // This is a check so we don't get a second load when the DataContext
            // is set
            if (!this.IsEnabled)
                return; this._lastTextFilterChangeToken = Guid.NewGuid();

            ThreadPool.QueueUserWorkItem(this.asyncRefresh,
                new LoadState(this._lastTextFilterChangeToken, false, false));
        }

        void asyncRefresh(object state)
        {
            if (!(state is LoadState))
                return;
            LoadState loadState = (LoadState)state;

            try
            {
                if (loadState.LastTextFilterChangeToken != Guid.Empty)
                    Thread.Sleep(Constants.TEXT_FILTER_IDLE_TIMER);

                if (this._controller != null && (loadState.LastTextFilterChangeToken == Guid.Empty || this._lastTextFilterChangeToken == loadState.LastTextFilterChangeToken))
                    this._controller.ReapplyFilter();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error applying filter", e);
            }
        }
    }
}
