using System;
using System.Windows.Controls;
using System.Windows.Data;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.Validators
{
    public class EnvironmentNameValidator : ValidationRule
    {
        public EnvironmentNameValidator()
        {
            this.ValidationStep = System.Windows.Controls.ValidationStep.UpdatedValue;
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var binding = value as BindingExpression;
            var page = binding.DataItem as ApplicationPage;

            if(!page.IsEnvironmentNameValid)
            {
                return new ValidationResult(false, "The environment name length must be between 4 and 23 characters and can only contain letters, numbers, and hyphens.");
            }

            return new ValidationResult(true, null);
        }
    }
}
