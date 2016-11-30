using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CloudFormation.Controllers;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for StackEventControl.xaml
    /// </summary>
    public partial class StackEventControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(StackEventControl));

        ViewStackController _controller;
        Guid _lastTextFilterChangeToken;

        public StackEventControl()
        {
            InitializeComponent();
        }

        public void Initialize(ViewStackController controller)
        {
            this._controller = controller;
        }

        void onEventTextFilterChange(object sender, TextChangedEventArgs e)
        {
            // This is a check so we don't get a second load when the DataContext
            // is set
            if (!this.IsEnabled)
                return; 
            this._lastTextFilterChangeToken = Guid.NewGuid();

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
