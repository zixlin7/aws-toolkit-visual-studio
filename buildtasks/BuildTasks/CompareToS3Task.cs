using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Drawing;
using System.Xml;

using Microsoft.Build.Utilities;

using Amazon.S3;
using Amazon.S3.Model;

using BuildCommon;

using LitJson;
using System.Linq;
using System.Drawing.Imaging;

namespace BuildTasks
{
    /// <summary>
    /// Recursively compares the contents of a local folder to a bucket
    /// and root keypath in S3.
    /// </summary>
    public class CompareToS3Task : S3TaskBase
    {
        public string S3Path { get; set; }

        private string _localPath;

        public string LocalPath
        {
            get { return _localPath; }
            set { _localPath = value.EndsWith("\\") ? value : value + "\\"; }
        }

        /// <summary>
        /// Collection of filenames, with paths relative to LocalPath, that will be excluded
        /// from the upload.
        /// </summary>
        /// <remarks>
        /// The paths should be separated using a semi-colon character (MSBuild will do this for you 
        /// if you use the @(propertyname) syntax).
        /// </remarks>
        public string FileExceptions
        {
            get; set;
        }

        /// <summary>
        /// Collection of objects keys to ignore, relative to S3Path.
        /// </summary>
        /// <remarks>
        /// The paths should be separated using a semi-colon character (MSBuild will do this for you 
        /// if you use the @(propertyname) syntax).
        /// </remarks>
        public string KeyExceptions { get; set; }

        protected HashSet<string> ExceptionsToHash(string exceptions)
        {
            return Utilities.Split(exceptions, ";", StringComparer.Ordinal, (x) =>
            {
                var path = x.StartsWith(LocalPath) ? x.Substring(LocalPath.Length) : x;
                return path.Replace('\\', '/');
            });
        }

        public override bool Execute()
        {
            CheckWaitForDebugger();

            var ret = true;
            try
            {
                var fileExceptions = ExceptionsToHash(FileExceptions);
                var keyExceptions = ExceptionsToHash(KeyExceptions);   // assumption here that S3Path <-> LocalPath
                var comp = Compare(fileExceptions, keyExceptions);
                if (comp.DifferencesExist)
                    throw new Exception("Found differences between hosted files and local");

                Log.LogMessage("No differences found between S3 and local. {0} files examined.", comp.S3Items.Count);
            }
            catch (Exception e)
            {
                Log.LogErrorFromException(e);
                ret = false;
            } 

            return ret;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileExclusions">Set of filenames, relative to LocalPath, to be ignored</param>
        /// <param name="keyExclusions">Set of filenames, relative to S3Path, to be ignored</param>
        /// <param name="verifyS3Items"></param>
        /// <param name="verifyLocalItems"></param>
        /// <returns></returns>
        public Comparison Compare(HashSet<string> fileExclusions, HashSet<string> keyExclusions, bool verifyS3Items = true, bool verifyLocalItems = true)
        {
            var comp = new Comparison(Bucket, 
                                      S3Path, 
                                      S3Client, 
                                      LocalPath, 
                                      this, 
                                      verifyS3Items, 
                                      verifyLocalItems,
                                      keyExclusions,
                                      fileExclusions);
            return comp;
        }
    }

    public abstract class Item
    {
        public abstract string RelativePath { get; }
        public abstract MemoryStream Contents { get; }
        public string FileName { get { return System.IO.Path.GetFileName(RelativePath); } }
        public string Extension { get { return System.IO.Path.GetExtension(RelativePath); } }
    }

    public class S3Item : Item
    {
        private static char[] etagTrimChars = new char[] { '\"' };

        public string Bucket { get; private set; }
        public string Key { get; private set; }
        public override string RelativePath { get { return Key.Replace(Utilities.KeySeparator, Utilities.PathSeparator); } }
        public override MemoryStream Contents
        {
            get
            {
                var ms = new MemoryStream();
                using (var response = Client.GetObject(new GetObjectRequest { BucketName = Bucket, Key = Key }))
                {
                    response.ResponseStream.CopyTo(ms);
                }
                ms.Position = 0;
                return ms;
            }
        }

        public long Size { get; private set; }
        public string Etag { get; private set; }
        public string Path
        {
            get
            {
                // https://aws-vs-toolkit.s3.amazonaws.com/VersionInfo.xml
                string path = string.Format("https://{0}.s3.amazon.com/{1}", Bucket, Key);
                return path;
            }
        }
        public string MD5
        {
            get
            {
                string md5 = null;
                if (!string.IsNullOrEmpty(Etag)
                    && !Etag.Contains("-"))
                {
                    md5 = Etag.Trim(etagTrimChars);
                }

                if (string.IsNullOrEmpty(md5))
                {
                    md5 = FileMD5Util.GenerateMD5Hash(Path);
                }

                return md5;
            }
        }
        private IAmazonS3 Client { get; set; }

        public S3Item(string bucket, S3Object s3o, IAmazonS3 client)
        {
            Bucket = bucket;
            Key = s3o.Key;
            Size = s3o.Size;
            Etag = s3o.ETag;
            Client = client;
        }

        public override string ToString()
        {
            return string.Format("{0} - {1} ({2})", Bucket, Key, MD5);
        }
    }

    public class FileItem : Item
    {
        public DirectoryInfo BaseDir { get; private set; }
        public string Path { get; private set; }
        public override string RelativePath { get { return Utilities.GetRelativePath(Path, BaseDir.FullName); } }
        public override MemoryStream Contents
        {
            get
            {
                MemoryStream ms = new MemoryStream();
                using (var stream = File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    stream.CopyTo(ms);
                }
                ms.Position = 0;
                return ms;
            }
        }
        public string MD5
        {
            get
            {
                return FileMD5Util.GenerateMD5Hash(Path);
            }
        }

        public FileItem(string path, DirectoryInfo baseDir)
        {
            Path = path;
            BaseDir = baseDir;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Path, MD5);
        }
    }

    public class Comparison
    {
        public Task Task { get; set; }
        public List<S3Item> S3Items { get; set; }
        public List<FileItem> LocalItems { get; set; }

        public List<string> DifferingPaths { get; set; }
        public List<string> S3Only { get; set; }
        public List<string> LocalOnly { get; set; }

        public HashSet<string> KeysToIgnore { get; private set; }
        public HashSet<string> FilesToIgnore { get; private set; }

        public bool DifferencesExist
        {
            get
            {
                return (
                    DifferingPaths.Count > 0 ||
                    S3Only.Count > 0 ||
                    LocalOnly.Count > 0);
            }
        }

        public Comparison(string bucket, 
                          string s3path, 
                          IAmazonS3 s3client,
                          string localPath,
                          Task task,
                          bool verifyS3Items, 
                          bool verifyLocalItems,
                          HashSet<string> keysToIgnore = null,
                          HashSet<string> filesToIgnore = null)
        {
            var localPathInfo = new DirectoryInfo(localPath);
            if (!localPathInfo.Exists)
                throw new ArgumentException(string.Format("LocalPath {0} does not exist", localPath));

            Task = task;
            DifferingPaths = new List<string>();
            S3Only = new List<string>();
            LocalOnly = new List<string>();
            S3Items = new List<S3Item>();
            LocalItems = new List<FileItem>();

            KeysToIgnore = keysToIgnore ?? new HashSet<string>(StringComparer.Ordinal);
            foreach (var extraKey in extraKeysToIgnore)
            {
                KeysToIgnore.Add(extraKey);
            }

            FilesToIgnore = filesToIgnore ?? new HashSet<string>(StringComparer.Ordinal);
            foreach (var extraFile in extraFileNamesToIgnore)
            {
                FilesToIgnore.Add(extraFile);
            }

            S3Items = GetAllS3Items(bucket, s3path, s3client).ToList();
            LocalItems = GetAllFiles(localPathInfo).ToList();

            CalculateDifferences();

            if (verifyS3Items && !VerifyItems(S3Items))
                throw new Exception("Found issues with S3 files");
            if (verifyLocalItems && !VerifyItems(LocalItems))
                throw new Exception("Found issues with local files");
        }

        private static readonly HashSet<string> directorySuffixes = new HashSet<string>
        {
            "$folder$",
            "/"
        };
        private static readonly HashSet<string> extraKeysToIgnore = new HashSet<string>
        {
            "cloudformation-hostmanager/AWSDeploymentHostManagerDownload.zip"
        };
        private static HashSet<string> extraFileNamesToIgnore = new HashSet<string> { };

        private void CalculateDifferences()
        {
            List<S3Item> keys = S3Items;
            List<FileItem> files = LocalItems;

            var keyPaths = keys.Select(k => k.RelativePath);
            var filePaths = files.Select(f => f.RelativePath);
            var keysOnly = keyPaths.Except(filePaths, StringComparer.Ordinal).ToList();
            var filesOnly = filePaths.Except(keyPaths, StringComparer.Ordinal).ToList();
            var common = keyPaths.Intersect(filePaths).ToList();

            if (keys.Count != files.Count)
            {
                Task.Log.LogMessage("Difference in number of keys ({0}) and files ({1})", keys.Count, files.Count);
                Task.Log.LogMessage("Unique S3 keys: " + string.Join(", ", keysOnly));
                Task.Log.LogMessage("Unique files: " + string.Join(", ", filesOnly));

                S3Only.AddRange(keysOnly);
                LocalOnly.AddRange(filesOnly);
            }

            var keysDictionary = keys.ToDictionary(s3i => s3i.RelativePath, s3i => s3i, StringComparer.Ordinal);
            var filesDictionary = files.ToDictionary(fi => fi.RelativePath, fi => fi, StringComparer.Ordinal);

            foreach (var path in common)
            {
                S3Item key = null;
                FileItem file = null;
                if (!keysDictionary.TryGetValue(path, out key) || key == null ||
                    !filesDictionary.TryGetValue(path, out file) || file == null)
                {
                    Task.Log.LogMessage("Unable to get S3Item ({0}) or FileItem ({1}) for path {2}", key, file, path);
                    continue;
                }

                var keyMd5 = key.MD5;
                var fileMd5 = file.MD5;

                if (!string.Equals(keyMd5, fileMd5, StringComparison.OrdinalIgnoreCase))
                {
                    Task.Log.LogMessage("Mismatch between S3 and file for path {0}", path);
                    Task.Log.LogMessage("Remote: {0}, Local: {1}", keyMd5, fileMd5);
                    DifferingPaths.Add(path);
                }
            }
        }
        private bool VerifyItems(IEnumerable<Item> items)
        {
            bool isGood = true;
            foreach (var item in items)
            {
                isGood &= Verify(item);
            }

            return isGood;
        }
        private bool Verify(Item item)
        {
            bool isGood = true;
            string key = item.RelativePath;
            string extension = item.Extension.ToLower();
            MemoryStream contents = item.Contents;

            try
            {
                switch (extension)
                {
                    case ".xml":
                        var doc = new XmlDocument();
                        doc.Load(contents);
                        break;
                    case ".json":
                    case ".schema":
                    case ".template":
                        string json = Encoding.UTF8.GetString(contents.ToArray());
                        object obj = JsonMapper.ToObject(json);
                        break;
                    case ".png":
                        var image = Image.FromStream(contents);
                        if (image.RawFormat.Guid != ImageFormat.Png.Guid)
                            throw new InvalidDataException(string.Format(
                                "Expected RawFormat PNG, but key [{0}] is {1}", key, image.RawFormat));
                        break;
                    case ".txt":
                    case ".html":
                        // not really sure what we can test for 'validity' here. Not zero length maybe?
                        break;
                    default:
                        if (!key.Equals("ping-check", StringComparison.OrdinalIgnoreCase) && !key.EndsWith(".zip"))
                            throw new InvalidDataException("Unexpected extension");
                        break;
                }
            }
            catch (Exception e)
            {
                Task.Log.LogMessage("Issue verifying S3 item with key {0}: {1}", key, e);
                isGood = false;
            }

            return isGood;
        }

        private IEnumerable<S3Item> GetAllS3Items(string bucket, string s3path, IAmazonS3 s3client)
        {
            var request = new ListObjectsRequest
            {
                BucketName = bucket,
                Delimiter = s3path
            };

            ListObjectsResponse response;
            do
            {
                response = s3client.ListObjects(request);

                foreach (var obj in response.S3Objects)
                {
                    var key = obj.Key;
                    if (directorySuffixes.Any(suffix => key.EndsWith(suffix)) ||
                        KeysToIgnore.Contains(key))
                        continue;

                    if (FilesToIgnore.Contains(key))
                        continue;

                    var s3item = new S3Item(bucket, obj, s3client);
                    yield return s3item;
                }
                request.Marker = response.NextMarker;
            } while (response.IsTruncated);
        }

        // returns all files under the supplied path, filtering out those we are
        // configured to ignore. The filtering comparison is done by relative
        // path comparison.
        private IEnumerable<FileItem> GetAllFiles(DirectoryInfo rootPath)
        {
            var files = rootPath.GetFiles("*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var path = file.FullName;
                // relativize the path to match up with what we get thru file exclusions
                var relativeFilename = path.Substring(rootPath.FullName.Length).Replace('\\','/');
                if (FilesToIgnore.Contains(relativeFilename))
                    continue;

                var fileItem = new FileItem(path, rootPath);
                yield return fileItem;
            }
        }

    }
}
