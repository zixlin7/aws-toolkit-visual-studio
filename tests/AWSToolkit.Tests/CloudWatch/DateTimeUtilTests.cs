using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.CloudWatch.Util;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch
{
    public class DateTimeUtilTests
    {
        public static IEnumerable<object[]> DateData = new List<object[]>
        {
            new object[] { 1647022937000, new DateTime(2022, 03, 11, 18, 22, 17, DateTimeKind.Utc) },
            new object[] { 1589505830000, new DateTime(2020, 05, 15, 01, 23, 50, DateTimeKind.Utc) },
            new object[] { 1345482645000, new DateTime(2012, 08, 20, 17, 10, 45, DateTimeKind.Utc) }
        };


        [Theory]
        [MemberData(nameof(DateData))]
        public void ConvertUnixToLocalTimeStamp(long timestamp, DateTime expectedTime)
        {
            var localTime = DateTimeUtil.ConvertUnixToLocalTimeStamp(timestamp);
            //convert to utc for compatibility with different test environments
            var utcTime = localTime.ToUniversalTime();
            Assert.Equal(expectedTime, utcTime);
        }

        [Theory]
        [MemberData(nameof(DateData))]
        public void ConvertLocalToUnixTimeStamp(long expectedTimestamp, DateTime dateTime)
        {
            Assert.Equal(expectedTimestamp, DateTimeUtil.ConvertLocalToUnixMilliseconds(dateTime));
        }
    }
}
