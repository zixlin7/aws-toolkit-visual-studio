using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards
{
    public static class ProjectCreatorUtilities
    {

        public static void CreateFromStream(Dictionary<string, string> replacementsDictionary, Stream stream, string relativePath)
        {
            var localZipFile = Path.GetTempFileName() + ".zip";
            using (Stream output = File.OpenWrite(localZipFile))
                stream.CopyTo(output);

            try
            {
                CreateFromZipFile(replacementsDictionary, localZipFile, relativePath);
            }
            finally
            {
                File.Delete(localZipFile);
            }
        }


        public static void CreateFromUrl(Dictionary<string, string> replacementsDictionary, string url, string relativePath)
        {
            var localZipFile = Path.GetTempFileName() + ".zip";
            WebClient client = new WebClient();
            client.DownloadFile(new Uri(url), localZipFile);
            try
            {
                CreateFromZipFile(replacementsDictionary, localZipFile, relativePath);
            }
            finally
            {
                File.Delete(localZipFile);
            }
        }

        public static void CreateFromZipFile(Dictionary<string, string> replacementsDictionary, string zipFile, string relativePath)
        {
            var projectFolder = replacementsDictionary["$destinationdirectory$"] as string;
            if (!string.IsNullOrEmpty(relativePath))
                projectFolder = Path.Combine(projectFolder, relativePath);

            var zip = new FastZip();
            zip.ExtractZip(zipFile, projectFolder, FastZip.Overwrite.Always, null, null, null, true);
        }
    }
}
