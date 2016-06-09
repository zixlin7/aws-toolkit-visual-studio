using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;

using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Model;

using log4net;

namespace Amazon.AWSToolkit.Lambda.View
{
    /// <summary>
    /// Interaction logic for ViewFunction.xaml
    /// </summary>
    public partial class ViewFunctionControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ViewFunctionControl));

        ViewFunctionController _controller;

        public ViewFunctionControl(ViewFunctionController controller)
        {
            InitializeComponent();
            this.FillAdvanceSettingsComboBoxes();

            this._ctlRuntime.ItemsSource = RuntimeOption.ALL_OPTIONS;

            this._controller = controller;
            this._ctlDetailHeaders.Content = string.Format("Function: {0}", this._controller.Model.FunctionName);


            this._ctlEventSourcesComponent.Initialize(this._controller);
            this._ctlLogsComponent.Initialize(this._controller);
        }

        public override string Title
        {
            get { return "Function: " + this._controller.Model.FunctionName; }
        }

        public override string UniqueId
        {
            get
            {
                return this._controller.Model.FunctionArn;
            }
        }

        public override bool SupportsBackGroundDataLoad
        {
            get { return true; }
        }

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void RefreshInitialData(object initialData)
        {
            onRefreshClick(this, new RoutedEventArgs());
        }

        private void onApplyChangesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.UpdateConfiguration();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error updating lambda function configuration", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error updating function configuration: " + e.Message);
            }
        }

        private void onUploadChangesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                bool updated = this._controller.UploadNewFunctionSource();
                if (updated)
                    this._controller.Refresh();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error uploading new function source", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error uploading new function source: " + e.Message);
            }
        }

        private void onRefreshClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.Refresh();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error refreshing lambda function", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing function: " + e.Message);
            }
        }

        private void _ctlTimeout_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ViewFunctionModel.IsValidTimeout(e.Text);
        }



        void FillAdvanceSettingsComboBoxes()
        {
            this._ctlMemory.Items.Clear();

            foreach (var value in LambdaUtilities.GetValidMemorySizes())
            {
                this._ctlMemory.Items.Add(value);
            }
        }

        private void onTabSelectionChange(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
