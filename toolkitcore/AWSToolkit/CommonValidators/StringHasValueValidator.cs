using System.Windows.Controls;
using System.Globalization;

namespace Amazon.AWSToolkit.CommonValidators
{
    public class StringHasValueValidator : ValidationRule
    {
        public string FieldName
        {
            get;
            set;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string v = value as string;
            if (v == null || v.Trim().Equals(string.Empty))
            {
                string msg = string.Format("{0} is a required field.", this.FieldName);
                return new ValidationResult(false, msg);
            }

            return new ValidationResult(true, null);
        }
    }
}
