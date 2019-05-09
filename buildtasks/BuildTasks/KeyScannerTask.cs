using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using BuildCommon;

namespace BuildTasks
{
    public class KeyScannerTask : BuildTaskBase
    {
        /// <summary>
        /// Token used for temporary coded added to our AWS tools that should not be in the final product.
        /// </summary>
        public const string NONE_SHIPPABLE_CODE_MARKER = "REMOVE_BEFORE_RELEASE";

        /// <summary>
        /// The root folder containing the files and folders to be scanned.
        /// </summary>
        public string Folder { get; set; }

        /// <summary>
        /// If specified, the filename patterns to be inspected in each folder. By default
        /// all files are inspected (FilePattern = "**").
        /// </summary>
        public string FilePattern { get; set; }

        /// <summary>
        /// Collection of files that will be scanned but any private key violations 
        /// that are discovered will be masked. Useful for dummy private keys in
        /// documentation files.
        /// </summary>
        public string PrivateKeyExceptions { get; set; }

        /// <summary>
        /// Collection of filenames, with paths relative to Folder, that will be excluded
        /// from the scan. The scanner expects that paths will be separated using a semi-colon
        /// character (MSBuild will do this for you if you use the @(propertyname) syntax).
        /// </summary>
        public string FileExceptions { get; set; }

        /// <summary>
        /// List of folders to be excluded fom the scan.
        /// </summary>
        public string FolderExceptions {get;set;}

        /// <summary>
        /// Set true to parallelize the scanning
        /// </summary>
        public bool ParallelScan { get; set; }

        public KeyScannerTask()
        {
            FilePattern = "**";
        }

        /// <summary>
        /// Fully resolved and \-terminated root path that we will scan below. Appending
        /// \ from the start makes relative path computations cleaner.
        /// </summary>
        private string ScanRootFolder { get; set; }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            if (string.IsNullOrEmpty(Folder))
                throw new ArgumentException("Expected Folder to be set to the root folder to scan beneath.");
            if (string.IsNullOrEmpty(FilePattern))
                throw new ArgumentException("FilePattern override resulted in an empty set");

            var resolvedPath = Path.GetFullPath(Folder);
            // makes relative path construction simpler
            ScanRootFolder = resolvedPath.EndsWith("\\") ? resolvedPath : resolvedPath + "\\";
            if (!Directory.Exists(ScanRootFolder))
                throw new ArgumentException("Folder resolved to a path that does not exist: " + ScanRootFolder);

            Log.LogMessage(MessageImportance.Normal, "Scanning {0} with file pattern {1}", ScanRootFolder, FilePattern);

            // configure exceptions collections
            accesskeyExceptions = Utilities.Split(defaultAccesskeyExceptions, Environment.NewLine, StringComparer.OrdinalIgnoreCase, l => l.Replace("*", "[A-Z0-9]*"));
            privatekeyExceptions = Utilities.Split(PrivateKeyExceptions, ";", StringComparer.OrdinalIgnoreCase);
            relativepathExceptions = Utilities.Split(FileExceptions, ";", StringComparer.OrdinalIgnoreCase);
            folderExceptions = Utilities.Split(FolderExceptions, ";", StringComparer.OrdinalIgnoreCase);
            extensionExceptions = Utilities.Split(defaultExtensionExceptions, ";", StringComparer.OrdinalIgnoreCase);

            // configure regex instances: one scans for access and private keys, other only for access keys
            accessAndPrivateKeysRegex = new Regex(accessAndPrivateKeysPattern.Replace("${find-keys.accesskey.exceptions}", defaultAccesskeyExceptions), RegexOptions.Compiled);
            accessKeysRegex = new Regex(accessKeysPattern.Replace("${find-keys.accesskey.exceptions}", defaultAccesskeyExceptions), RegexOptions.Compiled);

            examinedFileHashes = new HashSet<string>();
            var allFiles = GetFiles(ScanRootFolder, FilePattern).ToList();
            filesChecked = 0;

            // configure parallelism: -1 means no limit, 1 means use only one thread (serial)
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = ParallelScan ? -1 : 1
            };

            Parallel.ForEach(allFiles, parallelOptions, TestFile);

            if (filesChecked != allFiles.Count)
                throw new Exception(string.Format(CultureInfo.InvariantCulture,
                                                  "Total files to scan = {0}, total files scanned = {1}", 
                                                  allFiles.Count, 
                                                  filesChecked));

            Log.LogMessage(MessageImportance.Normal, "Finished checking {0} files", filesChecked);

            return true;
        }

        private bool FileExamined(string file, string contentsHash64)
        {
            bool firstEncounter;
            try
            {
                lock (examinedFileHashesLock)
                {
                    firstEncounter = examinedFileHashes.Add(contentsHash64);
                }
            }
            catch (Exception e)
            {
                Log.LogError("Exception caught while adding hash value of [{0}] for file [{1}]: {2}",
                             contentsHash64, 
                             file, 
                             e.Message);
                throw;
            }

            return firstEncounter;
        }

        private string MakeRelativeToScanRoot(string path)
        {
            if (!path.StartsWith(ScanRootFolder, StringComparison.OrdinalIgnoreCase))
                throw new Exception("Path to test is not under scan root: " + path);

            return path.Substring(ScanRootFolder.Length);
        }

        private void TestFile(string file)
        {
            Log.LogMessage(MessageImportance.Low, "Testing file " + file);
            Interlocked.Increment(ref filesChecked);

            var relativePath = MakeRelativeToScanRoot(file); // CommonFunctions.GetRelativePath(gitRoot, file);
            var extension1 = Path.GetExtension(file);
            var extension2 = Utils.GetMultiExtensions(file);

            // skip files by extension
            if (extensionExceptions.Contains(extension1) || extensionExceptions.Contains(extension2))
            {
                Log.LogMessage(MessageImportance.Normal, "Skipping file {0} because of extension", file);
                return;
            }
            // skip files by relative-to-root paths
            if (relativepathExceptions.Contains(relativePath))
            {
                Log.LogMessage(MessageImportance.Normal, "Skipping file {0} because of relative path exception", file);
                return;
            }
            if (relativePath.Contains(@"\bin\") || relativePath.Contains(@"\obj\"))
            {
                Log.LogMessage(MessageImportance.Normal, "Skipping file {0} because it is in a build output and the source would have been scanned.", file);
                return;
            }

            // intended for vsts codebase, where node_modules can appear at scan root, without leading \, or deeper in
            // the hierarchy with a lead \
            if (relativePath.Contains(@"node_modules\"))
            {
                Log.LogMessage(MessageImportance.Normal, "Skipping file {0} because it is a 3rd party node import.", file);
                return;
            }

            if (relativePath.Contains(@"\Microsoft.VisualStudio."))
            {
                Log.LogMessage(MessageImportance.Normal, "Skipping file {0} Microsoft assemblies and xml docs.", file);
                return;
            }

            // get file contents
            string contents;
            try
            {
                Log.LogMessage(MessageImportance.Low, "Reading file " + file);
                contents = File.ReadAllText(file);
            }
            catch (Exception e)
            {
                Log.LogError("Issue opening file {0}: {1}", file, e);
                throw;
            }

            // Search to see if there is any temporary code added that should have been removed before releasing to the public.
            if(contents.ToUpper().Contains(NONE_SHIPPABLE_CODE_MARKER))
            {
                throw new Exception(NONE_SHIPPABLE_CODE_MARKER + " marker found in file which must be removed before releasing code: " + file);
            }

            // skip files if it was already examined
            var bytes = Encoding.UTF8.GetBytes(contents);
            var hash = MD5.Create().ComputeHash(bytes);
            var hash64 = Convert.ToBase64String(hash);
            var firstEncounter = FileExamined(file, hash64);

            if (!firstEncounter)
            {
                Log.LogMessage(MessageImportance.Normal, "Skipping file {0} because a file with the same hash value has already been examined", file);
                return;
            }

            // pick regex to use
            Regex regex;
            if (!privatekeyExceptions.Contains(relativePath))
                regex = accessAndPrivateKeysRegex;
            else
                regex = accessKeysRegex;

            // find matches
            var matches = FindMatches(regex, contents);
            var accessKeyMatches = matches.SafeGet("accesskey");
            var privateKeyMatches = matches.SafeGet("privatekey");
            var validKeys = FindValidAccessKeys(accessKeyMatches);

            // act on access keys
            if (file.EndsWith(accessKeyCanaryFile, StringComparison.OrdinalIgnoreCase))
            {
                // canary file has to have at least one match
                if (validKeys.Count == 0)
                    throw new Exception("No access keys found in canary file " + file);
            }
            else
            {
                // non-canary file cannot have a match
                if (validKeys.Count > 0)
                    throw new Exception(string.Format("File {0} contains keys [{1}]", file, string.Join(", ", validKeys)));
            }

            // act on private keys
            if (file.EndsWith(privateKeyCanaryFile, StringComparison.OrdinalIgnoreCase) ||
                file.EndsWith(privateCredentialsCanaryFile, StringComparison.OrdinalIgnoreCase))
            {
                // canary file has to have at least one match
                if (privateKeyMatches.Count == 0)
                    throw new Exception("No private key found in canary file " + file);
            }
            else
            {
                // non-canary file cannot have a match
                if (privateKeyMatches.Count > 0)
                    throw new Exception(string.Format("File {0} contains private keys [{1}]", file, string.Join(", ", privateKeyMatches)));
            }
        }

        // get all files in directory, except for hidden folders
        private IEnumerable<string> GetFiles(string dir, string pattern)
        {
            var files = Directory.GetFiles(dir, pattern, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
                yield return file;

            var dirs = Directory.GetDirectories(dir);
            foreach (var subDir in dirs)
            {
                var dirInfo = new DirectoryInfo(subDir);
                var isHidden = (dirInfo.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden;
                if (isHidden)
                    continue;

                var skip = false;
                foreach(var e in folderExceptions)
                {
                    if (MakeRelativeToScanRoot(dirInfo.ToString()).StartsWith(e, StringComparison.OrdinalIgnoreCase))
                    {
                        Log.LogMessage(MessageImportance.Normal, "Skipping folder {0} because it is under \"{1}\"", dirInfo.ToString(), e);
                        skip = true;
                    }
                }
                if (skip) continue;

                var foundFiles = GetFiles(subDir, pattern);
                foreach (var file in foundFiles)
                    yield return file;
            }
        }

        private static List<string> FindValidAccessKeys(List<string> matches)
        {
            var validKeys = new List<string>();

            foreach (var match in matches)
            {
                var subMatches = FindMatches(invalidAccessKeyPattern, match);
                var invalidMatches = subMatches.SafeGet("accesskeyfake");
                if (invalidMatches.Count == 0)
                    validKeys.Add(match);
            }

            return validKeys;
        }

        private static Dictionary<string, List<string>> FindMatches(string pattern, string input)
        {
            var regex = new Regex(pattern, RegexOptions.None);
            return FindMatches(regex, input);
        }
        private static Dictionary<string, List<string>> FindMatches(Regex regex, string input)
        {
            Match match = regex.Match(input);
            var matches = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            if (match == Match.Empty)
            {
                //this.Log(Level.Info, "No matches found");
            }
            else
            {
                for (int i = 1; i < match.Groups.Count; i++)
                {
                    string str = regex.GroupNameFromNumber(i);
                    //this.Log(Level.Verbose, "Setting property '{0}' to '{1}'.", new object[] { str, match.Groups[str].Value });
                    var value = match.Groups[str].Value;

                    if (string.IsNullOrEmpty(value))
                        continue;

                    List<string> items = matches.SafeGet(str);
                    items.Add(value);
                }
            }

            return matches;
        }

        private HashSet<string>
            accesskeyExceptions,
            privatekeyExceptions,
            folderExceptions,
            relativepathExceptions,
            extensionExceptions;

        private Regex
            accessAndPrivateKeysRegex,
            accessKeysRegex;

        private HashSet<string> examinedFileHashes;
        private object examinedFileHashesLock = new object();
        private int filesChecked;

        #region Constants

        private const string defaultAccesskeyExceptions = "DELIDELIDELI*|EKIEJ7TN3ZWSVWB4*|9QAAAAAAAAAAAAAAAAJ6";
        private const string defaultExtensionExceptions = ".exe;.dll;.tlog;.zip;.pdb;.trx;.db;.suo;.patch;.vsix;.msi;.bak;.trx.xml;.nupkg;.unitypackage;.lock.json;.NetCore.dll-Help.xml;.examples.json;.GeneratedSamples.cs";
        private const string accessAndPrivateKeysPattern = "[^A-Z0-9](?'accesskey'(?!${find-keys.accesskey.exceptions})[A-Z0-9]{20})(?<!EXAMPLE)[^A-Z0-9]|(?'privatekey'(BEGIN RSA PRIVATE KEY)|(BEGIN CERTIFICATE))";
        private const string accessKeysPattern = "[^A-Z0-9](?'accesskey'(?!${find-keys.accesskey.exceptions})[A-Z0-9]{20})(?<!EXAMPLE)[^A-Z0-9]";

        private const string invalidAccessKeyPattern = "(?'accesskeyfake'(([A-Z]{20}))|([0-9]{20}))";
        private const string accessKeyCanaryFile = "access-key-canary.txt";
        private const string privateKeyCanaryFile = "private-key-canary.txt";
        private const string privateCredentialsCanaryFile = "private-credentials-canary.txt";

        #endregion
    }
}
