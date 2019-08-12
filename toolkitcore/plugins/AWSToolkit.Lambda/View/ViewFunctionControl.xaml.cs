using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Lambda.Controller;

using log4net;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.KeyManagementService.Model;

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
            this._controller = controller;
            this._ctlDetailHeaders.Content = string.Format("Function: {0}", this._controller.Model.FunctionName);

            this._ctlFunctionInvokeComponent.Initialize(this._controller);
            this._ctlAdvancedSettingsComponent.Initialize(this._controller);
            this._ctlEventSourcesComponent.Initialize(this._controller);
            this._ctlXRayComponent.Initialize(this._controller);
            this._ctlLogsComponent.Initialize(this._controller);

            this._ctlAdvancedSettingsComponent.PropertyChanged += _ctlAdvancedSettingsComponent_PropertyChanged;
        }

        public override string Title => "Function: " + this._controller.Model.FunctionName;

        public override string UniqueId => this._controller.Model.FunctionArn;

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override void RefreshInitialData(object initialData)
        {
            onRefreshClick(this, new RoutedEventArgs());
        }

        public VpcAndSubnetWrapper SetAvailableVpcSubnets(IEnumerable<Vpc> vpcs, IEnumerable<Subnet> subnets, IEnumerable<string> selectedSubnetIds)
        {
            return this._ctlAdvancedSettingsComponent.SetAvailableVpcSubnets(vpcs, subnets, selectedSubnetIds);
        }

        public void SetAvailableSecurityGroups(IEnumerable<SecurityGroup> existingGroups, string autoSelectGroup, IEnumerable<string> selectedSecurityGroupIds)
        {
            this._ctlAdvancedSettingsComponent.SetAvailableSecurityGroups(existingGroups, autoSelectGroup, selectedSecurityGroupIds);
        }

        public void SetAvailableDLQTargets(IList<string> topicArns, IList<string> queueArns, string selectedTargetArn)
        {
            this._ctlAdvancedSettingsComponent.SetAvailableDLQTargets(topicArns, queueArns, selectedTargetArn);
        }

        public IEnumerable<SubnetWrapper> SelectedSubnets => _ctlAdvancedSettingsComponent.SelectedSubnets;

        public IEnumerable<SecurityGroupWrapper> SelectedSecurityGroups => _ctlAdvancedSettingsComponent.SelectedSecurityGroups;

        public bool SubnetsSpanVPCs => _ctlAdvancedSettingsComponent.SubnetsSpanVPCs;

        public void SetAvailableKMSKeys(IEnumerable<KeyListEntry> keys, IEnumerable<AliasListEntry> aliases)
        {
            _ctlAdvancedSettingsComponent.SetAvailableKMSKeys(keys, aliases, _controller.Model.KMSKeyArn);
        }

        public KeyListEntry SelectedKMSKey => _ctlAdvancedSettingsComponent.SelectedKMSKey;

        public string SelectedDLQTargetArn => _ctlAdvancedSettingsComponent.SelectedDLQTargetArn;

        // catches updates in the vpc, security group and envvar controls that have bubbled up
        private void _ctlAdvancedSettingsComponent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(e.PropertyName);
        }

        private void onApplyChangesClick(object sender, RoutedEventArgs evnt)
        {
            try
            {
                this._controller.UpdateConfiguration();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error updating Lambda function configuration", e);
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
                LOGGER.Error("Error refreshing Lambda function", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error refreshing function: " + e.Message);
            }
        }

        private void onTabSelectionChange(object sender, SelectionChangedEventArgs e)
        {

        }

        private void PreviewIntTextInput(object sender, TextCompositionEventArgs e)
        {
            int i;
            e.Handled = !int.TryParse(e.Text, out i);
        }
    }
}
