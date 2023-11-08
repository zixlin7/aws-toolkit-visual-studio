using System;
using System.IO;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Util;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Install
{
    public static class CodeWhispererConstants
    {
        public static VersionRange LspCompatibleVersionRange = VersionRangeUtil.Create("0.0.0", "1.0.0");
        public const string Filename = "aws-lsp-codewhisperer.exe";
        public const string ManifestFilename = "manifest.json";
        public const int ManifestCompatibleMajorVersion = 0;
        public const string ManifestBaseCloudFrontUrl = "https://aws-toolkit-language-servers.amazonaws.com/codewhisperer";
        public static string ManifestCloudFrontUrl = $"{ManifestBaseCloudFrontUrl}/{ManifestCompatibleMajorVersion}/{ManifestFilename}";
        public static string LanguageServerIdentifier = "CodeWhisperer";
        public static string LspDownloadParentFolder => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "aws", "toolkits", "language-servers", "CodeWhisperer");
    }
}
