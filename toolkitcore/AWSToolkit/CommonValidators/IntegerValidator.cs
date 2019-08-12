using System.Windows.Controls;
using System.Globalization;

namespace Amazon.AWSToolkit.CommonValidators
{
    public class IntegerValidator : ValidationRule
    {

        public IntegerValidator()
        {
            this.Min = int.MinValue;
            this.Max = int.MaxValue;
        }

        public string FieldName
        {
            get;
            set;
        }

        public int Min
        {
            get;
            set;
        }

        public int Max
        {
            get;
            set;
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int currentValue;

            // Is a number?
            if (!int.TryParse((string)value, out currentValue))
            {
                return new ValidationResult(false, "Not a number.");
            }

            // Is in range?
            if ((currentValue < this.Min) || (currentValue > this.Max))
            {
                string msg = string.Format("{0} must be between {1} and {2}.", this.FieldName, this.Min, this.Max);
                return new ValidationResult(false, msg);
            }

            // Number is valid
            return new ValidationResult(true, null);
        }
    }
}
