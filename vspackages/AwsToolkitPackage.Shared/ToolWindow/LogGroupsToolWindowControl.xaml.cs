using System;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Interaction logic for LogGroupsToolWindowControl.xaml
    /// </summary>
    public partial class LogGroupsToolWindowControl : UserControl, IDisposable
    {
        private BaseAWSControl _childControl;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogGroupsToolWindowControl"/> class.
        /// </summary>
        public LogGroupsToolWindowControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Update child control and disposes previous control if connection settings change
        /// </summary>
        public void SetChildControl(BaseAWSControl control, Func<BaseAWSControl, bool> canUpdateHostedControl)
        {
            if (canUpdateHostedControl(_childControl))
            {
                var oldControl = _childControl;
                _childControl = control;
                this._logGroupsHost.Children.Clear();
                this._logGroupsHost.Children.Add(control);
                DisposeControl(oldControl);
            }
        }

        public void Dispose()
        {
            this._logGroupsHost.Children.Clear();
            DisposeControl(this._childControl);
        }

        /// <summary>
        /// Disposes child controls if they are disposable
        /// </summary>
        /// <param name="control"></param>
        private void DisposeControl(BaseAWSControl control)
        {
            if (control != null && control is IDisposable disposableControl)
            {
                disposableControl.Dispose();
            }
        }
    }
}
