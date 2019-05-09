using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using LitJson;
using System.Diagnostics;

namespace BuildCommon
{
    public static class Utils
    {
        public const string CoreDependencyName = "Core";

        public const string MergeBuildOutputFile = "merge-build-outputs.json";

        public static void WriteToJson(string filename, object data)
        {
            var json = JsonMapper.ToJson(data);
            WriteToFile(filename, json);
        }

        public static void WriteToFile(string fileName, string content)
        {
            if (!Directory.Exists("Deployment"))
                Directory.CreateDirectory("Deployment");

            File.WriteAllText(@".\Deployment\" + fileName, content);
        }

        public static T ReadFromJson<T>(string filename)
        {
            var json = ReadFromFile(filename);
            T data = JsonMapper.ToObject<T>(json);
            return data;
        }

        public static string ReadFromFile(string filename)
        {
            var content = File.ReadAllText(@".\Deployment\" + filename);
            return content;
        }

        /// <summary>
        /// Walks a filename to build the full set of dotted extensions.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetMultiExtensions(string path)
        {
            string filename = Path.GetFileName(path);
            var fullExtension = new StringBuilder();

            string extension;
            do
            {
                extension = Path.GetExtension(filename);
                filename = Path.GetFileNameWithoutExtension(filename);
                fullExtension.Insert(0, extension);
            } while (!string.IsNullOrEmpty(extension));

            return fullExtension.ToString();
        }

        public static string Run(string file, string fullArgs, string workingDir, bool ignoreError = false)
        {
            var si = new ProcessStartInfo
            {
                FileName = file,
                Arguments = fullArgs,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            string error;
            string output;
            int exitCode;
            using (var p = Process.Start(si))
            {
                output = p.StandardOutput.ReadToEnd();
                error = p.StandardError.ReadToEnd();
                exitCode = p.ExitCode;

                if (!p.HasExited)
                {
                    var separator = new string('-', 10);
                    using (var sw = new StringWriter())
                    {
                        sw.WriteLine("Process is stuck!");
                        sw.WriteLine("StdOut");
                        sw.WriteLine(separator);
                        sw.WriteLine(output);
                        sw.WriteLine(separator);
                        sw.WriteLine("ErrOut");
                        sw.WriteLine(separator);
                        sw.WriteLine(error);
                        sw.WriteLine(separator);
                        var message = sw.ToString();

                        try
                        {
                            p.Kill();
                        }
                        catch (Exception) { }
                        throw new InvalidOperationException(message);
                    }
                }

                if (exitCode != 0 && !ignoreError)
                {
                    var separator = new string('-', 10);
                    using (var sw = new StringWriter())
                    {
                        sw.WriteLine("Error invoking process!");
                        sw.WriteLine("File: {0}", file);
                        sw.WriteLine("Args: {0}", fullArgs);
                        sw.WriteLine("Dir:  {0}", workingDir);
                        sw.WriteLine("Exit code = {0}", exitCode);
                        sw.WriteLine("StdOut");
                        sw.WriteLine(separator);
                        sw.WriteLine(output);
                        sw.WriteLine(separator);
                        sw.WriteLine("ErrOut");
                        sw.WriteLine(separator);
                        sw.WriteLine(error);
                        sw.WriteLine(separator);
                        var message = sw.ToString();
                        throw new InvalidOperationException(message);
                    }
                }
            }
            return output;
        }
    }
}
