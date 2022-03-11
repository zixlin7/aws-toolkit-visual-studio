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
            new object[] { 1647022937000, new DateTime(2022, 03, 11, 10, 22, 17, DateTimeKind.Local) },
            new object[] { 1589505830000, new DateTime(2020, 05, 14, 18, 23, 50, DateTimeKind.Local) },
            new object[] { 1345482645000, new DateTime(2012, 08, 20, 10, 10, 45, DateTimeKind.Local) }
        };


        [Theory]
        [MemberData(nameof(DateData))]
        public void ConvertUnixToLocalTimeStamp(long timestamp, DateTime expectedTime)
        {
            Assert.Equal(expectedTime, DateTimeUtil.ConvertUnixToLocalTimeStamp(timestamp));
        }

        [Theory]
        [MemberData(nameof(DateData))]
        public void ConvertLocalToUnixTimeStamp(long expectedTimestamp, DateTime dateTime)
        {
            Assert.Equal(expectedTimestamp, DateTimeUtil.ConvertLocalToUnixMilliseconds(dateTime));
        }
    }
}
