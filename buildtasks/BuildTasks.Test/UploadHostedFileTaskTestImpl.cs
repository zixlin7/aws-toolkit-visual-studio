using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.S3;

namespace BuildTasks.Test
{
    /// <summary>
    /// Used to test UploadHostedFileTask, providing a mock Client
    /// </summary>
    internal class UploadHostedFileTaskTestImpl : UploadHostedFileTask
    {
        public UploadHostedFileTaskTestImpl(IAmazonS3 s3Client) : base()
        {
            SetS3ClientOverride(s3Client);
        }
    }
}
