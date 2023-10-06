using System;
using System.IO;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Util;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    public static class LspConstants
    {
        public static VersionRange LspCompatibleVersionRange = VersionRangeUtil.Create("1.0.0", "2.0.0");
        public const string FileName = "aws-lsp-codewhisperer-binary-win.exe";
        public static string LspDownloadParentFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "aws/toolkits/language-server/");
    }
}
