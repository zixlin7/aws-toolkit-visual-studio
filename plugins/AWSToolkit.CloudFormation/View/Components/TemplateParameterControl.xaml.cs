using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for TemplateParameterControl.xaml
    /// </summary>
    public partial class TemplateParameterControl : INotifyPropertyChanged
    {
        // This parameter is used so that we don't show error message during the first attempt at setting a value.
        // Otherwise as soon as they got in the text box there would be an error of it being blank
        bool hasEverLostFocus; 

        CloudFormationTemplateWrapper.TemplateParameter _parameter;

        public TemplateParameterControl(CloudFormationTemplateWrapper.TemplateParameter parameter)
            : this(parameter, false)
        {
        }

        public TemplateParameterControl(CloudFormationTemplateWrapper.TemplateParameter parameter, bool readOnly)
        {
            InitializeComponent();
            this._parameter = parameter;
            this.DataContext = this._parameter;
            string parameterValue = string.IsNullOrEmpty(parameter.OverrideValue) ? parameter.DefaultValue : parameter.OverrideValue;

            if (readOnly)
            {
                this._ctlValue.Style = this._ctlValue.FindResource("awsSelectableTextBoxStyle") as Style;
            }
            else if(parameter.AllowedValues != null && parameter.AllowedValues.Length > 0)
            {               
                this._ctlListValue.Visibility = Visibility.Visible;
                this._ctlListValue.ItemsSource = parameter.AllowedValues;
                this._ctlListValue.Text = parameterValue;
            }
            else if (parameter.NoEcho)
            {
                this._ctlPasswordClearTextValue.Text = this._ctlPasswordValue.Password = parameterValue;

                this._ctlValue.Visibility = System.Windows.Visibility.Hidden;
                this._ctlPasswordGrid.Visibility = System.Windows.Visibility.Visible;
            }
            else
            {
                this._ctlValue.Text = parameterValue;
            }

            if (!string.IsNullOrEmpty(this._parameter.Description))
                this._ctlDescription.Text = this._parameter.Description;

            if (!string.IsNullOrEmpty(this._parameter.ConstraintDescription))
            {
                if (string.IsNullOrEmpty(this._ctlDescription.Text))
                    this._ctlDescription.Text = this._parameter.ConstraintDescription;
                else
                    this._ctlDescription.Text += "\n" + this._parameter.ConstraintDescription;
            }
        }

        public string ParameterName
        {
            get { return this._parameter.Name; }
        }

        public void SetValue(string value)
        {
            this._parameter.OverrideValue = value;
            if (this._parameter.NoEcho)
            {
                this._ctlPasswordValue.Password = value;
                this._ctlPasswordClearTextValue.Text = value;
            }

            this.hasEverLostFocus = true;
            checkValid();
        }

        public void Reset()
        {
            this.hasEverLostFocus = false;
            this._errorMessage = null;
            this._ctlDescription.Visibility = System.Windows.Visibility.Visible;
            this._ctlErrorMessage.Visibility = System.Windows.Visibility.Hidden;

            this._parameter.OverrideValue = this._parameter.DefaultValue;

            if (this._parameter.NoEcho)
            {
                this._ctlPasswordValue.Password = this._parameter.DefaultValue;
                this._ctlPasswordClearTextValue.Text = this._parameter.DefaultValue;
            }
        }

        public bool IsValid
        {
            get
            {
                string errorMessage;
                bool isValue = this._parameter.IsValid(out errorMessage);

                if(this.hasEverLostFocus)
                    this.ErrorMessage = errorMessage;

                return isValue;
            }
        }

        string _errorMessage;
        public string ErrorMessage
        {
            get 
            { 
                return this._errorMessage; 
            }
            private set
            {
                if (!string.Equals(this._errorMessage, value))
                {
                    this._errorMessage = value;
                    NotifyPropertyChanged("ErrorMessage");
                }
            }
        }

        private void _ctlValue_LostFocus(object sender, RoutedEventArgs e)
        {
            this.hasEverLostFocus = true;
            this.checkValid();
        }

        private void _ctlPasswordValue_PasswordChanged(object sender, RoutedEventArgs e)
        {
            this._parameter.OverrideValue = this._ctlPasswordValue.Password;
            if (this.hasEverLostFocus)
                this.checkValid();
        }


        bool _disablePasswordChangeEvent;
        private void _ctlValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            // flag to see if the user is coming back in to fix an early validation error
            if (this.hasEverLostFocus)
                this.checkValid();

            if (!this._disablePasswordChangeEvent)
            {
                this._disablePasswordChangeEvent = true;
                try
                {
                    this._ctlPasswordClearTextValue.Text = this._ctlPasswordValue.Password;
                }
                finally
                {
                    this._disablePasswordChangeEvent = false;
                }
            }
        }

        private void _ctlValue_PasswordClearTextChanged(object sender, TextChangedEventArgs e)
        {
            // flag to see if the user is coming back in to fix an early validation error
            if (this.hasEverLostFocus)
                this.checkValid();
            
            if (!this._disablePasswordChangeEvent)
            {
                this._disablePasswordChangeEvent = true;
                try
                {
                    this._ctlPasswordValue.Password = this._ctlPasswordClearTextValue.Text;
                }
                finally
                {
                    this._disablePasswordChangeEvent = false;
                }
            }
        }

        private void ClearPassword_Checked(object sender, RoutedEventArgs e)
        {
            if (this._ctlShowClearText.IsChecked.GetValueOrDefault())
            {
                this._ctlPasswordClearTextValue.Visibility = System.Windows.Visibility.Visible;
                this._ctlPasswordValue.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this._ctlPasswordClearTextValue.Visibility = System.Windows.Visibility.Hidden;
                this._ctlPasswordValue.Visibility = System.Windows.Visibility.Visible;
            }
        }

        void checkValid()
        {
            bool isValid = this.IsValid;

            if (isValid)
            {
                this._ctlErrorMessage.Text = string.Empty;
                this._ctlDescription.Visibility = System.Windows.Visibility.Visible;
                this._ctlErrorMessage.Visibility = System.Windows.Visibility.Hidden;
            }
            else
            {
                this._ctlErrorMessage.Text = this.ErrorMessage;
                this._ctlDescription.Visibility = System.Windows.Visibility.Hidden;
                this._ctlErrorMessage.Visibility = System.Windows.Visibility.Visible;
            }
        }




    }
}
