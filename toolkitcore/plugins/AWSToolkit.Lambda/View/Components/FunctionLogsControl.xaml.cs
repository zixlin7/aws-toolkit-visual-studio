using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;

using log4net;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for FunctionLogsControl.xaml
    /// </summary>
    public partial class FunctionLogsControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(FunctionLogsControl));

        ViewFunctionController _controller;

        public FunctionLogsControl()
        {
            InitializeComponent();
        }

        public void Initialize(ViewFunctionController controller)
        {
            this._controller = controller;
        }

        private void GetLogStream_OnClick(object sender, RoutedEventArgs evnt)
        {
            evnt.Handled = true;

            var btn = evnt.Source as Button;
            if (btn == null)
                return;
            
            var wrapper = btn.DataContext as LogStreamWrapper;
            if (wrapper == null)
                return;

            try
            {
                this._controller.DownloadLog(wrapper.LogStreamName);
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to download log stream", e);
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Failed to download log stream: {0}", e.Message));
            }
        }

        private void DeleteLogStream_OnClick(object sender, RoutedEventArgs evnt)
        {
            evnt.Handled = true;

            var btn = evnt.Source as Button;
            if (btn == null)
                return;

            var wrapper = btn.DataContext as LogStreamWrapper;
            if (wrapper == null)
                return;

            var message = string.Format("Are you sure you want to delete the log stream {0}?", wrapper.LogStreamName);
            if (!ToolkitFactory.Instance.ShellProvider.Confirm("Delete Log Stream", message))
                return;

            try
            {
                this._controller.DeleteLog(wrapper);
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to download log stream", e);
                ToolkitFactory.Instance.ShellProvider.ShowError(string.Format("Failed to download log stream: {0}", e.Message));
            }
        }

    }
}
