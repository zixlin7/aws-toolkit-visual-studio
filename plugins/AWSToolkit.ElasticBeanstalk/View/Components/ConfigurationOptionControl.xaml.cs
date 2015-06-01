﻿using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

using Amazon.AWSToolkit.ElasticBeanstalk.Model;
//using Amazon.Common.Extensions;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    public enum ConfigurationOptionDescriptionValueTypes
    {
        Scalar, List, Boolean, CommaSeparatedList, Unknown
    }

    /// <summary>
    /// Interaction logic for ConfigurationOptionControl.xaml
    /// </summary>
    public partial class ConfigurationOptionControl
    {
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
                if (ConfigModel == null) 
                    throw new InvalidOperationException("Cannot get OptionDescription when ConfigModel is not configured");
                var description = ConfigModel.GetDescription(PropertyNamespaceName, PropertySystemName);


                // TODO: Delete when the namespace has been fully propagated.
                if(description == null && string.Equals(PropertyNamespaceName, "aws:elasticbeanstalk:container:dotnet:apppool"))
                {
                    if (string.Equals("Target Runtime", PropertySystemName))
                        PropertyNamespaceName = "aws:elasticbeanstalk:container:dotnet:targetruntime";
                    else
                        PropertyNamespaceName = "aws:elasticbeanstalk:container:dotnet:enable32bitapps";

                    return OptionDescription;
                }

                return description;
            }
        }

        public string PropertySystemName { get; set; }
        public string PropertyNamespaceName { get; set; }

        public string Value
        {
            get
            {
                if (ConfigModel == null) 
                    throw new InvalidOperationException(string.Format("Cannot get Value for {0}:{1} when ConfigModel is not configured", 
                                                                     PropertyNamespaceName, 
                                                                     PropertySystemName));
                return ConfigModel.GetValue(PropertyNamespaceName, PropertySystemName);
            }
            set
            {
                if (ConfigModel == null) 
                    throw new InvalidOperationException(string.Format("Cannot set Value for {0}:{1} when ConfigModel is not configured", 
                                                                     PropertyNamespaceName, 
                                                                     PropertySystemName));
                ConfigModel.SetValue(PropertyNamespaceName, PropertySystemName, value);
            }
        }

        public ConfigurationOptionDescriptionValueTypes ValueType
        {
            get
            {
                if (OptionDescription == null)
                    return ConfigurationOptionDescriptionValueTypes.Unknown;
                return GetValueType(OptionDescription);
            }
        }

        #endregion

        #region Overrides

        protected override void OnRender(DrawingContext drawingContext)
        {
            // In design mode, do not atempt to set interface
            if (!IsDesignMode)
            {
                UpdateInterface();
            }
            base.OnRender(drawingContext);
        }

        #endregion

        #region Constructor

        public ConfigurationOptionControl()
        {
            this.DataContextChanged += onDataContextChanged;
            InitializeComponent();
        }

        #endregion

        #region Event handlers

        private void CheckInputChecked(object sender, RoutedEventArgs e)
        {
            ConfigModel.SetValue(PropertyNamespaceName, PropertySystemName, true.ToString().ToLower());
        }
        private void CheckInputUnchecked(object sender, RoutedEventArgs e)
        {
            ConfigModel.SetValue(PropertyNamespaceName, PropertySystemName, false.ToString().ToLower());
        }
        private void CheckInputIndeterminate(object sender, RoutedEventArgs e)
        {
        }

        private void ComboInputSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.AddedItems.Count == 1)
                ConfigModel.SetValue(PropertyNamespaceName, PropertySystemName, e.AddedItems[0]);
            else
                ConfigModel.SetValue(PropertyNamespaceName, PropertySystemName, ComboInput.SelectedValue);
        }

        private void TextInputTextChanged(object sender, TextChangedEventArgs e)
        {
            ConfigModel.SetValue(PropertyNamespaceName, PropertySystemName, TextInput.Text);
        }

        private void ListInputChanged(object sender, RoutedEventArgs e)
        {
            StringBuilder sb = new StringBuilder();

            foreach (CheckBox cb in ListValues.Children)
            {
                if (!cb.IsChecked.GetValueOrDefault())
                    continue;

                if (sb.Length > 0)
                    sb.Append(",");
                sb.Append(cb.Content.ToString());
            }
            ConfigModel.SetValue(PropertyNamespaceName, PropertySystemName, sb.ToString());
        }

        private void onDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            INotifyPropertyChanged notify = this.DataContext as INotifyPropertyChanged;
            if (notify == null)
                return;

            notify.PropertyChanged += onPropertyChanged;
        }

        private void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (string.Equals(e.PropertyName, this.PropertySystemName))
            {
                UpdateInterfaceValue();
            }
        }


        #endregion

        #region Private methods

        private static ConfigurationOptionDescriptionValueTypes GetValueType(ConfigurationOptionDescription self)
        {
            if (self == null) throw new ArgumentNullException("self");

            ConfigurationOptionDescriptionValueTypes valueType;
            if (!Enum.IsDefined(typeof(ConfigurationOptionDescriptionValueTypes), self.ValueType.ToString()))
            {
                valueType = ConfigurationOptionDescriptionValueTypes.Unknown;
            }
            else
            {
                valueType = (ConfigurationOptionDescriptionValueTypes)Enum.Parse(typeof(ConfigurationOptionDescriptionValueTypes), self.ValueType);
            }

            return valueType;
        }

        private void UpdateInterfaceValue()
        {
            if (ValueType == ConfigurationOptionDescriptionValueTypes.Boolean)
            {
                bool isChecked;
                if(!bool.TryParse(Value, out isChecked))
                {
                    bool.TryParse(OptionDescription.DefaultValue, out isChecked);
                }
                if(CheckInput.IsChecked != isChecked)
                {
                    CheckInput.IsChecked = isChecked;
                }
            }
            else if (ValueType == ConfigurationOptionDescriptionValueTypes.Scalar || ValueType == ConfigurationOptionDescriptionValueTypes.CommaSeparatedList)
            {
                if (OptionDescription.ValueOptions != null && OptionDescription.ValueOptions.Count > 0)
                {
                    if (!object.Equals(ComboInput.SelectedValue, Value))
                    {
                        ComboInput.SelectedValue = Value;
                    }
                }
                else
                {
                    if (!object.Equals(TextInput.Text, Value))
                    {
                        TextInput.Text = Value;
                    }
                }
            }
            else if (ValueType == ConfigurationOptionDescriptionValueTypes.List)
            {
                string[] selected;
                if (Value == null)
                    selected = new string[0];
                else
                    selected = Value.Split(',');

                foreach (CheckBox cb in this.ListValues.Children)
                {
                    cb.IsChecked = selected.Contains(cb.Content.ToString());
                }
            }
            else if (ValueType == ConfigurationOptionDescriptionValueTypes.Unknown)
            {
                TextInput.Text = Value;
                TextInput.IsReadOnly = true;
            }
        }

        private bool _controlSet = false;
        private void UpdateInterface()
        {
            // If there is no valid context, return
            if (ConfigModel == null || OptionDescription == null)
            {
                ResetControls();
                return;
            }

            if (_controlSet)
            {
                return;
            }

            if (ValueType == ConfigurationOptionDescriptionValueTypes.Boolean)
            {
                // CheckInput should be used
                CheckInput.Visibility = Visibility.Visible;
            }
            else if (ValueType == ConfigurationOptionDescriptionValueTypes.Scalar || ValueType == ConfigurationOptionDescriptionValueTypes.CommaSeparatedList)
            {
                if (OptionDescription.ValueOptions != null && OptionDescription.ValueOptions.Count > 0)
                {
                    // ComboInput should be used
                    ComboInput.Visibility = Visibility.Visible;

                    // Populate choices
                    ComboInput.Items.Clear();
                    OptionDescription.ValueOptions.ForEach(x => ComboInput.Items.Add(x));
                }
                else
                {
                    // TextInput should be used
                    TextInput.Visibility = Visibility.Visible;

                    // Set validation (and required self-binding)
                    Binding binding = new Binding
                    {
                        Source = TextInput,
                        Path = new PropertyPath("Tag"),
                        Mode = BindingMode.OneWayToSource,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    };
                    binding.ValidationRules.Add(new ConfigurationOptionDescriptionValidationRule(OptionDescription));
                    TextInput.SetBinding(TextBox.TextProperty, binding);
                }
            }
            else if (ValueType == ConfigurationOptionDescriptionValueTypes.List)
            {
                ListValues.Children.Clear();
                ListValues.Visibility = Visibility.Visible;
                foreach (var item in OptionDescription.ValueOptions.OrderBy(x => x))
                {
                    var cb = new CheckBox();
                    cb.Margin = new Thickness(2);
                    cb.Content = item;
                    ListValues.Children.Add(cb);
                }
            }
            else
            {
                TextInput.Visibility = Visibility.Visible;
                TextInput.IsReadOnly = true;
                TextInput.Text = Value;
            }

            this.Visibility = Visibility.Visible;

            // Set current values
            UpdateInterfaceValue();
            
            // Attach event handlers
            AttachEventHandlers(true);

            _controlSet = true;
        }

        private void ResetControls()
        {
            this.Visibility = Visibility.Hidden;
            TextInput.Visibility = Visibility.Hidden;
            ComboInput.Visibility = Visibility.Hidden;
            CheckInput.Visibility = Visibility.Hidden;
            ListValues.Visibility = Visibility.Hidden;

            AttachEventHandlers(false);
        }

        private void AttachEventHandlers(bool attach)
        {
            if (attach)
            {
                // Attach to events
                TextInput.TextChanged += TextInputTextChanged;
                ComboInput.SelectionChanged += ComboInputSelectionChanged;
                CheckInput.Checked += CheckInputChecked;
                CheckInput.Unchecked += CheckInputUnchecked;
                CheckInput.Indeterminate += CheckInputIndeterminate;

                foreach (CheckBox cb in this.ListValues.Children)
                {
                    cb.Checked += ListInputChanged;
                }
            }
            else
            {
                // Detach
                TextInput.TextChanged -= TextInputTextChanged;
                ComboInput.SelectionChanged -= ComboInputSelectionChanged;
                CheckInput.Checked -= CheckInputChecked;
                CheckInput.Unchecked -= CheckInputUnchecked;
                CheckInput.Indeterminate -= CheckInputIndeterminate;
            }
        }

        #endregion

        #region Public methods

        public void ResetControl()
        {
            _controlSet = false;
            UpdateInterface();
        }

        #endregion
    }

    internal class ConfigurationOptionDescriptionValidationRule : ValidationRule
    {
        #region Properties

        public ConfigurationOptionDescription OptionDescription { get; private set; }

        #endregion

        #region Constructor

        public ConfigurationOptionDescriptionValidationRule(ConfigurationOptionDescription optionDescription)
        {
            if (optionDescription == null) throw new ArgumentNullException("optionDescription");

            OptionDescription = optionDescription;
        }

        #endregion

        #region Overrides

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            return new ValidationResult(IsValid((value ?? string.Empty).ToString(), cultureInfo), "foo!");
        }

        #endregion

        #region Private methods

        private bool IsValid(string value, CultureInfo cultureInfo)
        {
            if (OptionDescription.MaxLength > 0)
            {
                if (value.Length > OptionDescription.MaxLength)
                    return false;
                return true;
            }

            if (OptionDescription.Regex != null)
            {
                Match match = Regex.Match(value, OptionDescription.Regex.Pattern);
                if (!match.Success)
                    return false;
            }

            if (OptionDescription.MinValue != OptionDescription.MaxValue)
            {
                int intValue;
                if (int.TryParse(value, out intValue))
                {
                    if (intValue < OptionDescription.MinValue)
                        return false;
                    if (intValue > OptionDescription.MaxValue)
                        return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
        
        #endregion
    }
}
