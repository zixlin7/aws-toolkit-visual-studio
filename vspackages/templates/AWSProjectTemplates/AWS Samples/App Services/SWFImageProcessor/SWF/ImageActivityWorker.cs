/*******************************************************************************
* Copyright 2009-2018 Amazon.com, Inc. or its affiliates. All Rights Reserved.
* 
* Licensed under the Apache License, Version 2.0 (the "License"). You may
* not use this file except in compliance with the License. A copy of the
* License is located at
* 
* http://aws.amazon.com/apache2.0/
* 
* or in the "license" file accompanying this file. This file is
* distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
* KIND, either express or implied. See the License for the specific
* language governing permissions and limitations under the License.
*******************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

using Amazon;
using Amazon.SimpleWorkflow;
using Amazon.SimpleWorkflow.Model;

using Amazon.S3;
using Amazon.S3.Model;

namespace $safeprojectname$.SWF
{
    /// <summary>
    /// This work polls for activities for resizing an image.
    /// </summary>
    public class ImageActivityWorker
    {
        IAmazonSimpleWorkflow swfClient = new AmazonSimpleWorkflowClient();
        IAmazonS3 s3Client = new AmazonS3Client();

        Task _task;
        CancellationToken _cancellationToken;
        VirtualConsole _console;

        public ImageActivityWorker(VirtualConsole console)
        {
            this._console = console;
        }

        /// <summary>
        /// Kick off the worker to poll and process activities
        /// </summary>
        /// <param name="cancellationToken"></param>
        public void Start(CancellationToken cancellationToken = default(CancellationToken))
        {
            this._cancellationToken = cancellationToken;
            this._task = Task.Run((Action)this.PollAndProcessTasks);
        }

        /// <summary>
        /// Main loop for the worker that polls for tasks and processes them.
        /// </summary>
        void PollAndProcessTasks()
        {
            this._console.WriteLine("Image Activity Worker Started");
            while (!_cancellationToken.IsCancellationRequested)
            {
                ActivityTask task = Poll();
                if (!String.IsNullOrEmpty(task.TaskToken))
                {
                    ActivityState activityState = ProcessTask(task.Input);
                    CompleteTask(task.TaskToken, activityState);
                }
                //Sleep to avoid aggressive polling
                Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Poll the image processing activity task list to see if work needs to be done.
        /// </summary>
        /// <returns></returns>
        ActivityTask Poll()
        {
            this._console.WriteLine("Polling for activity task ...");
            PollForActivityTaskRequest request = new PollForActivityTaskRequest()
            {
                Domain = Constants.ImageProcessingDomain,
                TaskList = new TaskList()
                {
                    Name = Constants.ImageProcessingActivityTaskList
                }
            };
            PollForActivityTaskResponse response = swfClient.PollForActivityTask(request);
            return response.ActivityTask;
        }

        /// <summary>
        /// Respond back to SWF that the activity task is complete
        /// </summary>
        /// <param name="taskToken"></param>
        /// <param name="activityState"></param>
        void CompleteTask(String taskToken, ActivityState activityState)
        {
            RespondActivityTaskCompletedRequest request = new RespondActivityTaskCompletedRequest()
            {
                Result = Utils.SerializeToJSON<ActivityState>(activityState),
                TaskToken = taskToken
            };
            RespondActivityTaskCompletedResponse response = swfClient.RespondActivityTaskCompleted(request);
            this._console.WriteLine("Activity task completed. Resized Image at: " + activityState.ResizedImageKey);
        }

        /// <summary>
        /// This method is what actually does the work of the task. It pulls down the image from S3, resizes down to size
        /// the activity task input size it should be, and the puts the new resized image back in S3 under a different key.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        ActivityState ProcessTask(string input)
        {
            ActivityState activityState = Utils.DeserializeFromJSON<ActivityState>(input);
            this._console.WriteLine(string.Format("Processing activity task (Resize Image {0}x{0})...", activityState.ImageSize));

            var getRequest = new GetObjectRequest
            {
                BucketName = activityState.StartingInput.Bucket,
                Key = activityState.StartingInput.SourceImageKey
            };

            // Get the image from S3. Response is wrapped in a using statement so the
            // stream coming back from S3 is closed.
            //
            // To keep the sample simple the source image is downloaded for each thumbnail.
            // This could be cached locally for better performance.
            using (var getResponse = this.s3Client.GetObject(getRequest))
            {
                // Resize the image
                Stream thumbnailStream = ResizeImage(getResponse.ResponseStream, activityState.ImageSize);

                activityState.ResizedImageKey = String.Format("thumbnails/{0}x{0}/{1}", activityState.ImageSize, activityState.StartingInput.SourceImageKey);
                var putRequest = new PutObjectRequest
                {
                    BucketName = activityState.StartingInput.Bucket,
                    Key = activityState.ResizedImageKey,
                    InputStream = thumbnailStream
                };
                s3Client.PutObject(putRequest);
            }
            return activityState;
        }


        #region Image Resizing Code

        private Stream ResizeImage(Stream stream, int size)
        {
            var sourceImage = Image.FromStream(stream);
            var renderImage = SizeTo(sourceImage, size, size);

            var renderStreamStream = new MemoryStream();

            var encoder = GetEncoder(ImageFormat.Jpeg);
            var encoderParameters = new EncoderParameters(1);
            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);

            renderImage.Save(renderStreamStream, encoder, encoderParameters);
            renderStreamStream.Position = 0;

            return renderStreamStream;
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            return codecs.FirstOrDefault(codec => codec.FormatID == format.Guid);
        }

        /// <summary>
        /// Resize an image, retaining aspect ratio, to ensure longest dimension is no longer than maxDimension.
        /// If the original image's max dimension is already below max, do nothing.
        /// </summary>
        /// <param name="original"></param>
        /// <param name="maxWidth"></param>
        /// <param name="maxHeight"></param>
        /// <returns></returns>
        static Image SizeTo(Image original, int maxWidth, int maxHeight)
        {
            int sourceWidth = original.Width;
            int sourceHeight = original.Height;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = ((float)maxWidth / (float)sourceWidth);
            nPercentH = ((float)maxHeight / (float)sourceHeight);

            nPercent = nPercentH < nPercentW ? nPercentH : nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            int posX = Convert.ToInt32((destWidth - (sourceWidth * nPercent)) / 2);
            int posY = Convert.ToInt32((destHeight - (sourceHeight * nPercent)) / 2);

            var b = new Bitmap(destWidth, destHeight);

            var g = Graphics.FromImage((Image)b);
            g.Clear(Color.Transparent);

            g.InterpolationMode = InterpolationMode.HighQualityBicubic;

            g.DrawImage(original, posX, posY, destWidth, destHeight);
            g.Dispose();

            return (Image)b;
        }

        #endregion
    }
}
