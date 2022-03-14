using System;

namespace Amazon.AWSToolkit.CloudWatch.Util
{
    public static class DateTimeUtil
    {
        public static DateTime ConvertUnixToDateTime(long unixMilliseconds, TimeZoneInfo timezone)
        {
            var offset = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
            var timezoneOffset = TimeZoneInfo.ConvertTime(offset, timezone);
            return timezoneOffset.DateTime;
        }

        public static long AsUnixMilliseconds(this DateTime currentTime)
        {
            var offset = new DateTimeOffset(currentTime);
            return offset.ToUnixTimeMilliseconds();
        }
    }
}
