using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Amazon.AWSToolkit.CommonUI.Converters
{
    /// <summary>
    /// Converter to format lambda function State, LastUpdatedStatus
    /// and their associated fields in the view function panel
    /// eg. values: [`State`, `StateReasonCode`, `StateReason`]
    /// output: State [StateReasonCode: StateReason]
    /// </summary>
    public class LambdaStateConverter : IMultiValueConverter
    {
        /// <summary>
        /// Method to convert and format the array of values
        /// </summary>
        /// <param name="values">Expected input: [State, StateReasonCode, StateReason]</param>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Any(x => x == DependencyProperty.UnsetValue) || values.Length < 3)
                return DependencyProperty.UnsetValue;

            var state = $"{values[0]}";
            var stateReasonCode = $"{values[1]}";
            var stateReason = $"{values[2]}";

            if (string.IsNullOrEmpty(state))
            {
                return "N/A";
            }

            if (string.IsNullOrEmpty(stateReasonCode) || string.IsNullOrEmpty(stateReason))
            {
                return state;
            }

            var output = $"{state} [{stateReasonCode}: {stateReason}]";
            return output;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}