using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.Validators
{
    public class DeploymentVersionLabelValidator : ValidationRule
    {
        public DeploymentVersionLabelValidator()
        {
            this.ValidationStep = System.Windows.Controls.ValidationStep.UpdatedValue;
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null)
                return new ValidationResult(false, null);

            var binding = value as BindingExpression;
            var page = binding.DataItem as ApplicationOptionsPage;

            if (page == null || !page.IsInitialized || page.VersionFetchPending)
                return new ValidationResult(false, null);

            var isValid = false;
            string msg = null;

            var version = page.DeploymentVersionLabel;
            if (!string.IsNullOrEmpty(version))
            {
                if (page.ExistingVersionLabels != null && page.ExistingVersionLabels.Any())
                {
                    isValid = page.ExistingVersionLabels.All(v => string.Compare(v.VersionLabel, version, StringComparison.OrdinalIgnoreCase) != 0);
                    if (!isValid)
                        msg = "Version label has been used previously for this application";
                }
                else
                    isValid = true;
            }
            else
                msg = "A version label must be supplied";

            return isValid ? ValidationResult.ValidResult : new ValidationResult(false, msg);
        }
    }
}
