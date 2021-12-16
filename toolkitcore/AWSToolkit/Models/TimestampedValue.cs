using System;
using System.Diagnostics;

namespace Amazon.AWSToolkit.Models
{
    [DebuggerDisplay("{DateTime} - {Value}")]
    public class TimestampedValue<T>
    {
        public DateTime DateTime { get; }
        public T Value { get; }

        public TimestampedValue(DateTime date, T value)
        {
            DateTime = date;
            Value = value;
        }
    }
}
