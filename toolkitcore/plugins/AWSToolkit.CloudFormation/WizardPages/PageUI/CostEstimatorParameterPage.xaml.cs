using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.View.Components;
using Amazon.AWSToolkit.CommonUI.Components;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Regions;
using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for CostEstimatorParameterPage.xaml
    /// </summary>
    public partial class CostEstimatorParameterPage : INotifyPropertyChanged
    {
        IDictionary<string, object> _previousValues = null;

        ILog LOGGER = LogManager.GetLogger(typeof(CostEstimatorParameterPage));

        public AccountAndRegionPickerViewModel Connection { get; }

        public CostEstimatorParameterPage(IDictionary<string, object> previousValues, ToolkitContext toolkitContext)
        {
            Connection = new AccountAndRegionPickerViewModel(toolkitContext);
            Connection.SetServiceFilter(new List<string>()
            {
                DeploymentServiceIdentifiers.ToolkitCloudFormationServiceName
            });

            this._previousValues = previousValues ?? new Dictionary<string, object>();
            InitializeComponent();

            DataContext = this;
        }

        public void BuildParameters(CloudFormationTemplateWrapper wrapper)
        {
            this._ctlMainPanel.Children.Clear();

            if (wrapper != null && wrapper.Parameters != null)
            {
                foreach (var kvp in wrapper.Parameters.OrderBy(x => x.Key))
                {
                    var parameterUI = new TemplateParameterControl(kvp.Value);
                    parameterUI.Width = double.NaN;                    
                    this._ctlMainPanel.Children.Add(parameterUI);
                    parameterUI.PropertyChanged += new PropertyChangedEventHandler(parameterUI_PropertyChanged);

                    if (this._previousValues.ContainsKey(kvp.Key))
                        this._ctlLoadPreviousValues.IsEnabled = true;
                }
            }
        }

        void ConnectionChanged(object sender, EventArgs e)
        {
            this.NotifyPropertyChanged(nameof(Connection));
        }

        void parameterUI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.NotifyPropertyChanged(e.PropertyName);
        }

        public bool AllParametersValid
        {
            get
            {
                foreach (TemplateParameterControl param in this._ctlMainPanel.Children)
                {
                    if (!param.IsValid)
                        return false;
                }

                return true;
            }
        }

        private void ResetValues_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (TemplateParameterControl param in this._ctlMainPanel.Children)
                {
                    param.Reset();
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error reseting values", ex);
            }
        }

        private void LoadPreviousValues_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (TemplateParameterControl param in this._ctlMainPanel.Children)
                {
                    if (this._previousValues.ContainsKey(param.ParameterName) && this._previousValues[param.ParameterName] != null)
                    {
                        string value = this._previousValues[param.ParameterName].ToString();
                        param.SetValue(value);
                    }
                }
            }
            catch (Exception ex)
            {
                LOGGER.Error("Error loading previous values", ex);
            }
        }
    }
}
