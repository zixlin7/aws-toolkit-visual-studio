using System;
using System.IO;

using Amazon.AWSToolkit.AwsServices;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.VsSdk.Common;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Watches/Listens to <see cref="RunningDocumentEvents"/>
    /// </summary>
    public class DocumentEventWatcher : IDisposable
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly RunningDocumentEvents _documentEvents;

        public DocumentEventWatcher(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _documentEvents = new RunningDocumentEvents();
            _documentEvents.Opened += OnDocumentOpened;
        }

        private void OnDocumentOpened(string filePath)
        {
            var filename = Path.GetFileName(filePath);
            var fileExt = Path.GetExtension(filePath);
            var isTemplate = filename.EndsWith(ToolkitFileTypes.CloudFormationTemplateExtension);

            // record a file edit metric when a file with `.template` extension is opened, distinguish serverless template files
            if (isTemplate)
            {
                var isServerless = string.Equals(filename, Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME,
                    StringComparison.InvariantCultureIgnoreCase);
                RecordTemplateFileEdit(isServerless, fileExt);
            }
        }

        /// <summary>
        /// Records a file edit metric for .template files
        /// </summary>
        /// <param name="isServerless">whether the file is a serverless template file</param>
        /// <param name="fileExtension">denotes the extension of the file</param>
        private void RecordTemplateFileEdit(bool isServerless, string fileExtension)
        {
            _toolkitContext.TelemetryLogger.RecordFileEditAwsFile(new FileEditAwsFile()
            {
                AwsAccount = MetadataValue.NotApplicable,
                AwsRegion = MetadataValue.NotApplicable,
                Result = Result.Succeeded,
                AwsFiletype = isServerless ? AwsFiletype.Serverless : AwsFiletype.Cloudformation,
                FilenameExt = fileExtension
            });
        }

        public void Dispose()
        {
            _documentEvents.Opened -= OnDocumentOpened;
            _documentEvents.Dispose();
        }
    }
}
