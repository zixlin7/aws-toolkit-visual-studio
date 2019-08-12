using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.CloudFormation.View.Components;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for TemplateParametersPage.xaml
    /// </summary>
    public partial class TemplateParametersPage : INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TemplateParametersPage));

        IDictionary<string, object> _previousValues = null;

        public TemplateParametersPage(IDictionary<string, object> previousValues, bool disableLoadPreviousValues)
        {
            this._previousValues = previousValues ?? new Dictionary<string, object>();
            InitializeComponent();

            this._ctlLoadPreviousValues.Visibility = disableLoadPreviousValues ? Visibility.Hidden : Visibility.Visible;
        }

        public void BuildParameters(CloudFormationTemplateWrapper wrapper)
        {
            try
            {
                this._ctlMainPanel.Children.Clear();

                if (wrapper != null && wrapper.Parameters != null)
                {
                    foreach (var kvp in wrapper.Parameters.OrderBy(x => x.Key))
                    {
                        if (!kvp.Value.Hidden)
                        {
                            var parameterUI = new TemplateParameterControl(kvp.Value);
                            parameterUI.Width = double.NaN;
                            this._ctlMainPanel.Children.Add(parameterUI);

                            if (this._previousValues.ContainsKey(kvp.Key))
                                this._ctlLoadPreviousValues.IsEnabled = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Error building list of parameters", e);
            }
        }

        void parameterUI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.NotifyPropertyChanged(e.PropertyName);
        }

        public bool AllParametersValid
        {
            get
            {
                try
                {
                    foreach (TemplateParameterControl param in this._ctlMainPanel.Children)
                    {
                        if (!param.IsValid)
                            return false;
                    }

                    return true;
                }
                catch (Exception e)
                {
                    LOGGER.Error("Error determining if parametes are valid", e);
                    return false;
                }
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
