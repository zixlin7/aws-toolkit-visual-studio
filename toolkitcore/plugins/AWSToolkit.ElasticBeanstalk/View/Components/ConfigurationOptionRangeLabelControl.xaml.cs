using System;
using System.ComponentModel;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Interaction logic for ConfigurationOptionRangeLabelControl.xaml
    /// </summary>
    public partial class ConfigurationOptionRangeLabelControl
    {
        public ConfigurationOptionRangeLabelControl()
        {
            InitializeComponent();
        }

        #region Properties

        private bool IsDesignMode
        {
            get
            {
                return System.ComponentModel.DesignerProperties.GetIsInDesignMode(this);
            }
        }

        public EnvironmentConfigModel ConfigModel
        {
            get
            {
                return this.DataContext as EnvironmentConfigModel;
            }
        }

        public ConfigurationOptionDescription OptionDescription
        {
            get
            {
                if (ConfigModel == null) throw new InvalidOperationException("Cannot get OptionDescription when ConfigModel is not configured");
                return ConfigModel.GetDescription(PropertyNamespaceName, PropertySystemName);
            }
        }

        public string PropertySystemName { get; set; }
        public string PropertyNamespaceName { get; set; }
        
        #endregion

        protected override void OnRender(DrawingContext drawingContext)
        {
            // In design mode, do not atempt to set interface
            if (!IsDesignMode && this.OptionDescription != null && 
                this.OptionDescription.MinValue != this.OptionDescription.MaxValue)
            {
                TextInput.Text = string.Format("({0} - {1})", this.OptionDescription.MinValue, this.OptionDescription.MaxValue);
            }
            base.OnRender(drawingContext);
        }

    }
}
