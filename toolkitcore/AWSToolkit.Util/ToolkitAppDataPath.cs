using System;
using System.IO;

namespace Amazon.AWSToolkit
{
    public class ToolkitAppDataPath
    {
        public static string FolderPath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AWSToolkit");

        public static string Join(string path)
        {
            return Path.Combine(FolderPath, path);
        }
    }
}
