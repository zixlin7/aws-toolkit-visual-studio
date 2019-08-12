using System.Windows.Controls;
using System.Windows.Data;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.Validators
{
    public class ApplicationNameValidator : ValidationRule
    {
        public ApplicationNameValidator()
        {
            this.ValidationStep = System.Windows.Controls.ValidationStep.UpdatedValue;
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var binding = value as BindingExpression;
            var page = binding.DataItem as ApplicationPage;

            if (!page.IsApplicationNameValid)
            {
                return new ValidationResult(false, "The application name length must be between 1 and 100 characters.");
            }

            return new ValidationResult(true, null);
        }
    }
}
