using System;
using System.Windows.Controls;
using log4net;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Controller;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for InvokeLambdaFunctionControl.xaml
    /// </summary>
    public partial class InvokeLambdaFunctionControl : BaseAWSControl
    {
        ILog _logger = LogManager.GetLogger(typeof(InvokeLambdaFunctionControl));

        InvokeLambdaFunctionController _controller;
        public InvokeLambdaFunctionControl(InvokeLambdaFunctionController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = controller.Model;
        }

        public override string Title => "Invoke Lambda Function";

        public override bool Validated()
        {
            if(this._controller.Model.SelectedRegion == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Region must be selected.");
                return false;
            }
            if (this._controller.Model.SelectedFunction == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Function must be selected.");
                return false;
            }
            if (this._controller.Model.SelectedEventType == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Event type must be selected.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.InvokeFunction();
                return true;
            }
            catch(Exception e)
            {
                this._logger.Error("Error invoking function", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error invoking function: " + e.Message);
                return false;
            }
        }

        private void _ctlRegion_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                this._controller.LoadFunctions();
            }
            catch (Exception ex)
            {
                this._logger.Error("Error loading functions for region", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error loading functions for region: " + ex.Message);
            }
        }
    }
}
