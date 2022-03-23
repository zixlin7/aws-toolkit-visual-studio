using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Amazon.AWSToolkit
{
    public static class StringExtensionMethods
    {
        public const string RedactedText = "[REDACTED]";

        // Should consider having a global regex timeout defined and maybe automagically enforced everywhere
        // https://en.wikipedia.org/wiki/ReDoS
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(5);

        // ARNs have many variations which can lead to overly complicated regexes that may still not capture
        // them all, so just taking a greedy approach here and consume everything until a whitespace is hit. Be careful
        // changing this regex as ARNs can contain both AWS account IDs and GUIDs in the resource ID component.
        // Don't create a situation where the order of redact operations can change the final result.
        private static readonly Regex ArnRegex = new Regex(@"arn:\S*", RegexOptions.IgnoreCase, RegexTimeout);

        private static readonly Regex AwsAccountIdRegex = new Regex(@"\d{12}", RegexOptions.None, RegexTimeout);

        private static readonly Regex GuidRegex = new Regex(@"[0-9a-f]{8}-([0-9a-f]{4}-){3}([0-9a-f]{12}|" + Regex.Escape(RedactedText) + ")",
            RegexOptions.IgnoreCase, RegexTimeout);

        // Must have explicit type initializer to avoid beforefieldinit shenanigans that allow static methods to
        // be called before static field initializers are complete.  Non-deterministic NullReferenceExceptions, oh my!
        // https://csharpindepth.com/Articles/BeforeFieldInit
        static StringExtensionMethods() { }

        public static string RedactAll(this string @this)
        {
            return RedactAwsAccountId(RedactGuids(RedactArns(@this)));
        }

        public static string RedactGuids(this string @this)
        {
            return GuidRegex.Replace(@this, RedactedText);
        }

        public static string RedactArns(this string @this)
        {
            return ArnRegex.Replace(@this, RedactedText);
        }

        public static string RedactAwsAccountId(this string @this)
        {
            return AwsAccountIdRegex.Replace(@this, RedactedText);
        }

        public static IEnumerable<string> SplitByLength(this string @this, int length)
        {
            for (int i = 0;; i += length)
            {
                if (i + length > @this.Length)
                {
                    yield return @this.Substring(i);
                    break;
                }

                yield return @this.Substring(i, length);
            }
        }
    }
}
