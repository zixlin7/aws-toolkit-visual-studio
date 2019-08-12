using System.Windows.Controls;
using System.Windows.Data;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.Validators
{
    public class CNameValidator : ValidationRule
    {
        public CNameValidator()
        {
            this.ValidationStep = System.Windows.Controls.ValidationStep.UpdatedValue;
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var binding = value as BindingExpression;
            var page = binding.DataItem as ApplicationPage;
            var testValue = page.CName;

            if (testValue == null || testValue.Length < 4 || testValue.Length > 63)
            {
                return new ValidationResult(false, "The cname prefix length must be between 4 and 63 characters.");
            }

            if (testValue[0] == '-')
                return new ValidationResult(false, "The cname prefix can not start with a hypen.");

            if (testValue[testValue.Length - 1] == '-')
                return new ValidationResult(false, "The cname prefix can not end with a hypen.");

            foreach (var c in testValue)
            {
                if (!char.IsLetterOrDigit(c) && c != '-')
                {
                    return new ValidationResult(false, "The cname prefix can only contain letters, numbers, and hyphens.");
                }
            }

            return new ValidationResult(true, null);
        }
    }
}
