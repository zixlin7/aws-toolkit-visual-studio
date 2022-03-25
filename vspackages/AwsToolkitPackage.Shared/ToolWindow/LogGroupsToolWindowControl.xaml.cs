using System;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Interaction logic for LogGroupsToolWindowControl.xaml
    /// </summary>
    public partial class LogGroupsToolWindowControl : UserControl
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
        /// Update child control if connection settings change
        /// </summary>
        public void SetChildControl(BaseAWSControl control, Func<BaseAWSControl, bool> canUpdateHostedControl)
        {
            if (canUpdateHostedControl(_childControl))
            {
                _childControl = control;
                this._logGroupsHost.Children.Clear();
                this._logGroupsHost.Children.Add(control);
            }
        }
    }
}
