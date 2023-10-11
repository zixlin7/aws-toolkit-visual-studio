using System;
using System.IO;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Util;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    public static class CodeWhispererConstants
    {
        public static VersionRange LspCompatibleVersionRange = VersionRangeUtil.Create("1.0.0", "2.0.0");
        public const string Filename = "dexp-runtime-server-build-configuration-win.exe";
        public const string ManifestFilename = "lspManifest.json";
        public const int ManifestCompatibleMajorVersion = 0;
        public static string LspDownloadParentFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "aws", "toolkits", "language-server");
    }
}
