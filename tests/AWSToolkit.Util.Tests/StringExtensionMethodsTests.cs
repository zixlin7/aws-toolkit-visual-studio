using System.Linq;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class StringExtensionMethodsTests
    {
        private const string ValidArn = "arn:aws:service:us-east-1:012345678901:resource-type:resource-id";

        private const string MixedCaseArn = "ArN:U:GlAd:ThAt:U:ReAd:ThIs";

        private const string JustTheProtocolArn = "arn:";

        private const string WarnAsArn = "WARN:YouWillNotSeePoorlyFormattedMessagesLikeThis";
        private const string WarnAsArnExpected = "W" + StringExtensionMethods.RedactedText;

        [Theory]
        [InlineData(ValidArn, StringExtensionMethods.RedactedText)]
        [InlineData(MixedCaseArn, StringExtensionMethods.RedactedText)]
        [InlineData(JustTheProtocolArn, StringExtensionMethods.RedactedText)]
        [InlineData(WarnAsArn, WarnAsArnExpected)]
        public void RedactsArns(string input, string expected)
        {
            Assert.Equal(expected, input.RedactArns());
            Assert.Equal(Embed(expected), Embed(input.RedactArns()));
        }

        private const string ValidAwsAccountId = "012345678901";

        private const string AlmostValidAwsAccountId = "0123F5678901";

        [Theory]
        [InlineData(ValidAwsAccountId, StringExtensionMethods.RedactedText)]
        [InlineData(AlmostValidAwsAccountId, AlmostValidAwsAccountId)]
        public void RedactsAwsAccoundIds(string input, string expected)
        {
            Assert.Equal(expected, input.RedactAwsAccountId());
            Assert.Equal(Embed(expected), Embed(input.RedactAwsAccountId()));
        }

        private const string ValidGuid = "01234567-ABCD-abcd-0a1B-012345678901";

        private const string AllXPseudoGuid = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX";

        private const string AllDecimalGuid = "01234567-0123-0123-0123-012345678901";

        private const string AllA2FCharsGuid = "ABCDEFAB-ABCD-abcd-AbCd-ABCDEFabcdef";

        [Theory]
        [InlineData(ValidGuid, StringExtensionMethods.RedactedText)]
        [InlineData(AllXPseudoGuid, AllXPseudoGuid)]
        [InlineData(AllDecimalGuid, StringExtensionMethods.RedactedText)]
        [InlineData(AllA2FCharsGuid, StringExtensionMethods.RedactedText)]
        public void RedactsGuids(string input, string expected)
        {
            Assert.Equal(expected, input.RedactGuids());
            Assert.Equal(Embed(expected), Embed(input.RedactGuids()));
        }

        private const string ValidMixedPii = ValidAwsAccountId + " then " + ValidArn + " and " + ValidGuid;
        private const string ValidMixedPiiExpected =
            StringExtensionMethods.RedactedText + " then " + StringExtensionMethods.RedactedText + " and " + StringExtensionMethods.RedactedText;

        [Theory]
        [InlineData(ValidMixedPii, ValidMixedPiiExpected)]
        public void RedactsAll(string input, string expected)
        {
            Assert.Equal(expected, input.RedactAll());
            Assert.Equal(Embed(expected), Embed(input.RedactAll()));
        }

        [Fact]
        public void RedactInAnyOrderYieldsConsistentResults()
        {
            var allTheThingz = "This " + ValidAwsAccountId + " is not a " + ValidArn + " nor a " + ValidGuid + " is it?";
            var expected =
                "This " + StringExtensionMethods.RedactedText +
                " is not a " + StringExtensionMethods.RedactedText +
                " nor a " + StringExtensionMethods.RedactedText + " is it?";

            Assert.Equal(expected, allTheThingz.RedactArns().RedactAwsAccountId().RedactGuids());
            Assert.Equal(expected, allTheThingz.RedactArns().RedactGuids().RedactAwsAccountId());
            Assert.Equal(expected, allTheThingz.RedactGuids().RedactArns().RedactAwsAccountId());
            Assert.Equal(expected, allTheThingz.RedactGuids().RedactAwsAccountId().RedactArns());
            Assert.Equal(expected, allTheThingz.RedactAwsAccountId().RedactGuids().RedactArns());
            Assert.Equal(expected, allTheThingz.RedactAwsAccountId().RedactArns().RedactGuids());
        }

        private string Embed(string msg)
        {
            return $"Embedded {msg} this\r\nin {msg} some text\r\n a {msg} few times.";
        }

        [Fact]
        public void SplitByLengthByLengthArgWhenStringLongerThanLengthArg()
        {
            using (var result = new string('X', 35).SplitByLength(10).GetEnumerator())
            {
                for (int i = 0; i < 3; ++i)
                {
                    Assert.True(result.MoveNext());
                    Assert.Equal(new string('X', 10), result.Current);
                }

                Assert.True(result.MoveNext());
                Assert.Equal(new string('X', 5), result.Current);

                Assert.False(result.MoveNext());
            }
        }

        [Fact]
        public void SplitByLengthReturnsSameStringWhenStringShorterThanLengthArg()
        {
            var input = "This is less than 50 chars.";
            var result = input.SplitByLength(50).ToArray();

            Assert.Single(result);
            Assert.Equal(input, result[0]);
        }
    }
}
