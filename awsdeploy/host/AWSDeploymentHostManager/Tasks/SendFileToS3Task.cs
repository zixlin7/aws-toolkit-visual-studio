using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

using System.Threading;

using log4net;

namespace AWSDeploymentHostManager.Tasks
{
    public class SendFileToS3Task : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(SendFileToS3Task));

        public override string Operation
        {
            get
            {
                return "SendFileToS3"; 
            }
        }

        public override string Execute()
        {
            Interlocked.Increment(ref HostManager.ASyncTasksRunning);
            ThreadPool.QueueUserWorkItem(this.DoExecute);
            return GenerateResponse(TASK_RESPONSE_DEFER);
        }

        public void DoExecute(object state)
        {
            string filename = null;
            string s3url = null;
            string contentType = null;
            FilePublication pub = null;

            try
            {
                if (!this.parameters.TryGetValue("filename", out filename))
                {
                    LOGGER.Error("Parameter missing in request: filename");
                    Event.LogWarn(Operation, "Parameter missing in request: filename");
                    return;
                }

                if (!this.parameters.TryGetValue("s3url", out s3url))
                {
                    LOGGER.Error("Parameter missing in request: s3url");
                    Event.LogWarn("SendFileToS3", "Parameter missing in request: s3url");
                    return;
                }

                if (!this.parameters.TryGetValue("content-type", out contentType))
                {
                    LOGGER.Error("Parameter missing in request: content-type");
                    Event.LogWarn("SendFileToS3", "Parameter missing in request: content-type");
                    return;
                }

                LOGGER.InfoFormat("Publishing {0} to {1}.", filename, s3url);

                string s3Path = s3url.Substring(0, s3url.IndexOf('?'));

                pub = FilePublication.LoadPendingForFile(filename);

                if (pub != null)
                {
                    if (!s3Path.EndsWith(String.Format("{0}/{1}/{2}", pub.Path,HostManager.Config.Ec2InstanceId, pub.S3Name), StringComparison.InvariantCultureIgnoreCase))
                    {
                        LOGGER.ErrorFormat("S3 keyname for pending file publication for {0} did not match", filename);
                        Event.LogWarn("SendFileToS3", string.Format("S3 keyname for pending file publication for {0} did not match", filename));
                        return;
                    }

                    if (pub.Size != new FileInfo(pub.FullPath).Length)
                    {
                        LOGGER.ErrorFormat("File size for pending file publication for {0} did not match", filename);
                        Event.LogWarn("SendFileToS3", string.Format("File size for pending file publication for {0} did not match", filename));
                        return;
                    }
                    pub.SetInProcess();
                    filename = pub.FullPath;
                }
                else if (HostManager.Config.RequirePublicationToExport)
                {
                    LOGGER.ErrorFormat("No pending file publication for {0}", filename);
                    Event.LogWarn("SendFileToS3", string.Format("No pending file publication for {0}", filename));
                    return;
                }

                try
                {
                    HttpStatusCode statusCode = S3Util.UploadFile(s3url, filename, contentType);
                    if (statusCode != HttpStatusCode.OK)
                    {
                        LOGGER.WarnFormat("Got a non 200 response when updating. (Filename: {0}, ContentType {1}, Http Status: {2})",
                            filename, contentType, statusCode);
                        Event.LogWarn(Operation, String.Format("Got a non 200 response when updating. (Filename: {0}, ContentType {1}, Http Status: {2})",
                            filename, contentType, statusCode));
                        if (pub != null)
                            pub.SetPending();
                        return;
                    }
                }
                catch (Exception e)
                {
                    LOGGER.ErrorFormat("Exception uploading file. (Filename: {0}, S3Url {1}, ContentType {2}, Exception {3})",
                        filename, s3url, contentType, e.Message);
                    Event.LogWarn("SendFileToS3", string.Format("Exception uploading file. (Filename: {0}, ContentType {1}, Exception {2})",
                        filename, contentType, e.Message));
                    if (pub != null)
                        pub.SetPending();
                    return;
                }

                LOGGER.InfoFormat("SendFileToS3 Completed. (Filename: {0}, S3Url {1}, ContentType {2})",
                        filename, s3url, contentType);
                Event.LogInfo("SendFileToS3", string.Format("SendFileToS3 Completed. (Filename: {0}, ContentType {1})",
                        filename, contentType));

                if (pub != null)
                    pub.SetComplete();
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Exception uploading file. (Filename: {0}, S3Url {1}, ContentType {2}, Exception {3})",
                    filename, s3url, contentType, e);
                Event.LogWarn("SendFileToS3", string.Format("Exception uploading file. (Filename: {0}, ContentType {1}, Exception {2})",
                    filename, contentType, e.Message));
                if (pub != null)
                    pub.SetPending();
            }
            finally
            {
                Interlocked.Decrement(ref HostManager.ASyncTasksRunning);
            }
        }
    }
}
