using System;
using log4net.Util;
using log4net.Util.TypeConverters;

namespace Amazon.AWSToolkit.Util
{
    /// <summary>
    /// Converter to convert a string specified in the log4net config to its numeric value
    /// </summary>
    public class NumericLog4NetConverter : IConvertFrom
    {
        public bool CanConvertFrom(Type sourceType)
        {
            return typeof(string) == sourceType;
        }

        public object ConvertFrom(object source)
        {
            var pattern =  (string)source;
            var patternString = new PatternString(pattern);
            var value = patternString.Format();

            return int.Parse(value);
        }
    }
}
