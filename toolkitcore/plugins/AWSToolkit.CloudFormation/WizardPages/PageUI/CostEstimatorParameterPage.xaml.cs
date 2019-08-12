﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.View.Components;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.PluginServices.Deployment;
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

        string _lastSeenAccount = string.Empty;

        public CostEstimatorParameterPage(IDictionary<string, object> previousValues)
        {
            this._previousValues = previousValues ?? new Dictionary<string, object>();
            InitializeComponent();

            DataContext = this;
            this._ctlAccountAndRegionPicker.PropertyChanged += new PropertyChangedEventHandler(_ctlAccountAndRegionPicker_PropertyChanged);
        }

        public CostEstimatorParameterPage(IAWSWizardPageController controller, IDictionary<string, object> previousValues)
            : this(previousValues)
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public void Initialize(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            this._ctlAccountAndRegionPicker.Initialize(account, region, new[] { DeploymentServiceIdentifiers.CloudFormationServiceName });
        }

        public AccountViewModel SelectedAccount => this._ctlAccountAndRegionPicker.SelectedAccount;

        public RegionEndPointsManager.RegionEndPoints SelectedRegion => this._ctlAccountAndRegionPicker.SelectedRegion;

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

        void _ctlAccountAndRegionPicker_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.NotifyPropertyChanged(e.PropertyName);
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
