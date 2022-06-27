using System.Collections.Generic;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Util
{
    public class StringUtilsTests
    {
        public static IEnumerable<object[]> SanitizeFileNameData = new List<object[]>
        {
            new object[] { "", "" },
            new object[] {"d8bf82ba-ff0b-4786-9fca-f4299454d618", "d8bf82ba-ff0b-4786-9fca-f4299454d618" },
            new object[] { "2022/05/16/[$LATEST]b13c8a4e69384b7581d5cf436953d9c6", "2022_05_16_[$LATEST]b13c8a4e69384b7581d5cf436953d9c6" },
            new object[] { "ecs/sample-app/0614525d1971441693f946e0ba484c43", "ecs_sample-app_0614525d1971441693f946e0ba484c43" },
            new object[] { "ecs/sample-app/0614525d1971441693f946e0ba484c43", "ecs_sample-app_0614525d1971441693f946e0ba484c43" },
            new object[] { "abcdefgh<ijkl>mnop", "abcdefgh_ijkl_mnop"}
        };

        [Theory]
        [MemberData(nameof(SanitizeFileNameData))]
        public void SanitizeFileName(string fileName, string expectedFileName)
        {
            var sanitizedFileName = StringUtils.SanitizeFilename(fileName);

            Assert.Equal(expectedFileName, sanitizedFileName);
        }



        public static IEnumerable<object[]> NormalizeLineEndingData = new List<object[]>
        {
            new object[] { "", "" },
            new object[] { "hello\\", "hello\\" },
            new object[] { "hello\t", "hello\t" },
            new object[] { "hello\r", "hello\r" },
            new object[] { "hello\r\n", "hello"},
            new object[] { "hello\n", "hello"},
            new object[] { "hello", "hello" },
        };

        [Theory]
        [MemberData(nameof(NormalizeLineEndingData))]
        public void NormalizeLineEnding(string inputText, string expectedText)
        {
            var normalizedText = StringUtils.NormalizeLineEnding(inputText);

            Assert.Equal(expectedText, normalizedText);
        }
    }
}
