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
        public void ConvertUnixToDateTime(long timestamp, DateTime expectedTime)
        {
            var dateTime = DateTimeUtil.ConvertUnixToDateTime(timestamp, TimeZoneInfo.Utc);
            Assert.Equal(expectedTime, dateTime);
        }

        [Theory]
        [MemberData(nameof(DateData))]
        public void AsUnixMilliseconds(long expectedTimestamp, DateTime dateTime)
        {
            Assert.Equal(expectedTimestamp, dateTime.AsUnixMilliseconds());
        }
    }
}
