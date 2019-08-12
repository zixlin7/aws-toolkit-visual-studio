using System.Collections.Generic;
using System.IO;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace Amazon.S3.IO
{
    public static class S3Directory
    {
        private const string FOLDER_POSTFIX = "$folder$";

        public static string[] GetDirectories(IAmazonS3 s3Client, string bucketName, string path)
        {
            var directories = new List<string>();
            iterateListObjects(s3Client, bucketName, path, SearchOption.TopDirectoryOnly, false, directories, null, null, null);
            return directories.ToArray(); ;
        }

        public static string[] GetFiles(IAmazonS3 s3Client, string bucketName, string path)
        {
            return GetFiles(s3Client, bucketName, path, SearchOption.TopDirectoryOnly);
        }

        public static string[] GetFiles(IAmazonS3 s3Client, string bucketName, string path, SearchOption searchOption)
        {
            var files = new List<string>();
            iterateListObjects(s3Client, bucketName, path, searchOption, false, null, files, null, null);
            return files.ToArray();
        }

        public static string[] GetFiles(IAmazonS3 s3Client, string bucketName, string path, SearchOption searchOption, bool includeFolderFiles, HashSet<S3StorageClass> validStorageClasses)
        {
            var files = new List<string>();
            iterateListObjects(s3Client, bucketName, path, searchOption, includeFolderFiles, null, files, null, validStorageClasses);
            return files.ToArray();
        }

        public static void GetDirectoriesAndFiles(IAmazonS3 s3Client, string bucketName, string path, out string[] directories, out string[] files)
        {
            var listDirectories = new List<string>();
            var listFiles = new List<string>();
            iterateListObjects(s3Client, bucketName, path, SearchOption.TopDirectoryOnly, false, listDirectories, listFiles, null, null);
            directories = listDirectories.ToArray();
            files = listFiles.ToArray();
        }

        public static void GetDirectoriesAndFiles(IAmazonS3 s3Client, string bucketName, string path, out string[] directories, out S3Object[] files)
        {
            var listDirectories = new List<string>();
            var listFiles = new List<S3Object>();
            iterateListObjects(s3Client, bucketName, path, SearchOption.TopDirectoryOnly, false, listDirectories, null, listFiles, null);
            directories = listDirectories.ToArray();
            files = listFiles.ToArray();
        }

        public static void GetDirectoriesAndFiles(IAmazonS3 s3Client, string bucketName, string path, out string[] directories, out S3Object[] files, ref string nextMarker)
        {
            var listDirectories = new List<string>();
            var listFiles = new List<S3Object>();
            nextMarker = iterateListObjects(s3Client, bucketName, path, SearchOption.TopDirectoryOnly, false, listDirectories, null, listFiles, nextMarker, null);
            directories = listDirectories.ToArray();
            files = listFiles.ToArray();
        }

        private static void iterateListObjects(IAmazonS3 s3Client, string bucketName, string path, SearchOption searchOption, bool includeFolderFiles, List<string> directories, List<string> files, List<S3Object> s3os, HashSet<S3StorageClass> validStorageClasses)
        {
            string nextMarker = null;
            do
            {
                nextMarker = iterateListObjects(s3Client, bucketName, path, searchOption, includeFolderFiles, directories, files, s3os, nextMarker, validStorageClasses);
            } while (!string.IsNullOrEmpty(nextMarker));

            if(directories != null)
                directories.Sort();
            if(files != null)
                files.Sort();
        }

        private static string iterateListObjects(IAmazonS3 s3Client, string bucketName, string path, SearchOption searchOption, bool includeFolderFiles, List<string> directories, List<string> files, List<S3Object> s3os, string nextMarker, HashSet<S3StorageClass> validStorageClasses)
        {
            path = cleanPath(path);
            ListObjectsRequest listRequest = new ListObjectsRequest()
            {
                BucketName = bucketName,
                Marker = nextMarker,
                Prefix = path
            };

            if (searchOption == SearchOption.TopDirectoryOnly)
                listRequest.Delimiter = "/";

            var response = s3Client.ListObjects(listRequest);


            if (directories != null)
            {
                foreach (string dir in response.CommonPrefixes)
                {
                    // remove trailing slash
                    string cleanD = dir.Substring(0, dir.Length - 1);
                    directories.Add(cleanD);
                }
            }

            if (files != null || s3os != null)
            {
                foreach (var s3Object in response.S3Objects)
                {
                    if (!includeFolderFiles && (s3Object.Key.EndsWith(FOLDER_POSTFIX) || s3Object.Key.EndsWith("/")))
                        continue;

                    if (validStorageClasses != null && !validStorageClasses.Contains(AmazonS3Util.ConvertToS3StorageClass(s3Object.StorageClass)))
                        continue;

                    if (files != null)
                        files.Add(s3Object.Key);
                    if (s3os != null)
                        s3os.Add(s3Object);
                }
            }

            if (!response.IsTruncated)
                return null;

            return response.NextMarker;
        }

        private static string cleanPath(string path)
        {
            if (path.Equals("/"))
                path = "";
            if (path.StartsWith("/"))
                path = path.Substring(1);
            if (path.Length > 0 && !path.EndsWith("/"))
                path = string.Format("{0}/", path);

            return path;
        }

        public static void CreateDirectory(IAmazonS3 s3Client, string bucketName, string path)
        {
            path = path.Replace(@"\", "/");
            if (path.StartsWith("/"))
                path = path.Substring(1);
            if (!path.EndsWith("/"))
                path += "/";

            string lastName = path;
            int pos = path.LastIndexOf("/", path.Length - 2);
            if (pos >= 0)
            {
                lastName = path.Substring(pos + 1);                
            }

            if(lastName.EndsWith("/"))
            {
                lastName = lastName.Substring(0, lastName.Length - 1);
            }

            path += string.Format("{0}_{1}", lastName, FOLDER_POSTFIX);

            var request = new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = path
            };
            request.InputStream = new MemoryStream();

            s3Client.PutObject(request);
        }
    }
}
