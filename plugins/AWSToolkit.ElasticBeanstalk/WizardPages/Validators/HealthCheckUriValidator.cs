using System;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.Validators
{
    public class HealthCheckUriValidator : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var testValue = value as string;

            if (!string.IsNullOrEmpty(testValue))
            {
                if (Uri.IsWellFormedUriString(string.Format("http://test.elasticbeanstalk.com{0}", testValue),
                                              UriKind.Absolute))
                    return new ValidationResult(true, null);
            }

            return new ValidationResult(false, "The relative Uri specified for the health check page is not valid or is empty.");
        }
    }
}
