using System;

namespace Amazon.AWSToolkit.CloudWatch.Util
{
    public static class DateTimeUtil
    {
        public static DateTime ConvertUnixToLocalTimeStamp(long unixMilliseconds)
        {
            var offset = DateTimeOffset.FromUnixTimeMilliseconds(unixMilliseconds);
            var dateTime = offset.DateTime;
            return dateTime.ToLocalTime();
        }

        public static long ConvertLocalToUnixMilliseconds(DateTime currentTime)
        {
            var offset = new DateTimeOffset(currentTime);
            return offset.ToUnixTimeMilliseconds();
        }
    }
}
