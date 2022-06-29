using System.Collections.Generic;
using System.IO;
using Amazon.S3.Model;

namespace Amazon.S3.IO
{
    public static class S3Directory
    {
        private const string FOLDER_POSTFIX = "$folder$";

        public static string[] GetFiles(IAmazonS3 s3Client, string bucketName, string path, SearchOption searchOption, bool includeFolderFiles, HashSet<S3StorageClass> validStorageClasses)
        {
            var files = new List<string>();
            iterateListObjects(s3Client, bucketName, path, searchOption, includeFolderFiles, null, files, null, validStorageClasses);
            return files.ToArray();
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

            directories?.Sort();
            files?.Sort();
        }

        private static string iterateListObjects(IAmazonS3 s3Client, string bucketName, string path, SearchOption searchOption, bool includeFolderFiles, List<string> directories, List<string> files, List<S3Object> s3os, string nextMarker, HashSet<S3StorageClass> validStorageClasses)
        {
            ListObjectsRequest listRequest = new ListObjectsRequest()
            {
                BucketName = bucketName,
                Marker = nextMarker,
                Prefix = path
            };

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                listRequest.Delimiter = S3Path.DefaultDirectorySeparator;
            }

            var response = s3Client.ListObjects(listRequest);

            if (directories != null)
            {
                foreach (string dir in response.CommonPrefixes)
                {
                    directories.Add(dir);
                }
            }

            if (files != null || s3os != null)
            {
                foreach (var s3Object in response.S3Objects)
                {
                    if (!includeFolderFiles && (s3Object.Key.EndsWith(FOLDER_POSTFIX) || S3Path.IsDirectory(s3Object.Key)))
                    {
                        continue;
                    }

                    if (validStorageClasses != null && !validStorageClasses.Contains(s3Object.StorageClass))
                    {
                        continue;
                    }

                    files?.Add(s3Object.Key);
                    s3os?.Add(s3Object);
                }
            }

            return response.IsTruncated ? response.NextMarker : null;
        }

        public static void CreateDirectory(IAmazonS3 s3Client, string bucketName, string path)
        {
            var folderFilename = S3Path.TrimEndingDirectorySeparator(S3Path.GetLastPathComponent(path));
            path = S3Path.Combine(path, $"{folderFilename}_{FOLDER_POSTFIX}");

            s3Client.PutObject(new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = path,
                InputStream = new MemoryStream()
            });
        }
    }
}
