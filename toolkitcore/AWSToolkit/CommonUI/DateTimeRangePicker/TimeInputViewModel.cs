using System;
using System.ComponentModel;
using System.Globalization;

using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CommonUI.DateTimeRangePicker
{
    /// <summary>
    /// Represents a text box control that accepts a time input
    /// </summary>
    public class TimeInputViewModel : BaseModel, IDataErrorInfo
    {
        private string _timeInput;
        private DateTime _time;
        private string _format = DateTimeUtil.GetLocalSystemFormat().LongTimePattern;
        
        public string TimeInput
        {
            get => _timeInput;
            set => SetProperty(ref _timeInput, value);
              
        }
        public DateTime Time
        {
            get => _time;
            private set => SetProperty(ref _time, value);
        }
        public string Format
        {
            get => _format;
            set => SetProperty(ref _format, value);
        }

        /// <summary>
        /// Initializes input field to string representation of the given time in specified format
        /// </summary>
        public void SetTimeInput(DateTime? time)
        {
            TimeInput = time?.ToString(Format);
        }

        /// <summary>
        /// Retrieve and update Time <see cref="DateTime"/>  from input
        /// </summary>
        public void SetTime()
        {
            var result = TryGetTime(out var timeValue);
            if (result)
            {
                Time = timeValue;
            }
        }

        /// <summary>
        /// Parses time input in the format specified to retrieve <see cref="DateTime"/> value
        /// </summary>
        private bool TryGetTime(out DateTime time)
        {
            return DateTime.TryParseExact(TimeInput, Format, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out time);
        }

        #region IDataErrorInfo

        public string this[string columnName]
        {
            get
            {
                switch (columnName)
                {
                    case nameof(TimeInput):
                        if (!string.IsNullOrWhiteSpace(TimeInput) && !TryGetTime(out _))
                        {
                            return $"Invalid input. Time must be specified in the format {Format}";
                        }
                        break;
                }

                return null;
            }

        }

        public string Error => this[nameof(TimeInput)];

        #endregion
    }
}
