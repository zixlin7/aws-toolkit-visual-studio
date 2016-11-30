using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.EC2.Validators
{
    public class InstanceNameValidator : ValidationRule
    {
        public InstanceNameValidator()
        {
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            var testValue = value as string;
            if (!string.IsNullOrEmpty(testValue) && testValue.StartsWith("aws:", StringComparison.OrdinalIgnoreCase))
                return new ValidationResult(false, "The text 'aws:' is reserved and may not be used as a prefix here");
                
            return new ValidationResult(true, null);
        }
    }
}
