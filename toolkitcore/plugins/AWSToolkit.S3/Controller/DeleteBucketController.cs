using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tasks;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace Amazon.AWSToolkit.S3.Controller
{
    /// <summary>
    /// The controller for Delete Bucket action.
    /// </summary>
    public class DeleteBucketController : BaseContextCommand
    {
        /// <summary>
        /// Stores the synchronization context. This is used in callback methods to
        /// update the UI using the UI thread.
        /// </summary>
        private readonly SynchronizationContext _syncContext;

        private readonly ToolkitContext _toolkitContext;
        private readonly IAWSToolkitShellProvider _shellProvider;

        /// <summary>
        /// The default constructor.
        /// </summary>
        public DeleteBucketController(ToolkitContext toolkitContext, IAWSToolkitShellProvider shellProvider)
        {
            _toolkitContext = toolkitContext;
            _shellProvider = shellProvider;
            _syncContext = System.Threading.SynchronizationContext.Current;
        }

        /// <summary>
        /// This method is invoked when the action is executed.
        /// </summary>
        /// <param name="model">The model on which the action is executed.</param>
        /// <returns>Result of the action.</returns>
        public override ActionResults Execute(IViewModel model)
        {
            if (!(model is S3BucketViewModel bucketModel))
            {
                RecordMetric(Result.Failed);
                return new ActionResults().WithSuccess(false);
            }

            return PromptAndDeleteBucket(bucketModel);
        }

        private ActionResults PromptAndDeleteBucket(S3BucketViewModel bucketModel)
        {
            try
            {
                // Display the delete confirmation prompt.
                var control = new DeleteBucketControl(bucketModel.Name);
                if (!_shellProvider.ShowModal(control, MessageBoxButton.OKCancel))
                {
                    RecordMetric(Result.Cancelled);

                    return new ActionResults()
                        .WithSuccess(false);
                }

                if (control.DeleteBucketWithObjects)
                {
                    // Objects deletion takes place in the background, we immediately exit this Action
                    DeleteBucketAndContentsAsync(bucketModel).LogExceptionAndForget();

                    return new ActionResults()
                        .WithSuccess(true);
                }
                else
                {
                    // Only the bucket is to be deleted.
                    var result = DeleteBucket(bucketModel);

                    var actionResults = new ActionResults()
                        .WithSuccess(result);

                    if (result)
                    {
                        actionResults
                            .WithFocalname(bucketModel.Name)
                            .WithShouldRefresh(true);
                    }

                    return actionResults;
                }
            }
            catch (Exception e)
            {
                ShowDeletionError(bucketModel, e);

                return new ActionResults()
                    .WithSuccess(false);
            }
        }

        private bool DeleteBucket(S3BucketViewModel bucketModel)
        {
            Result deleteResult = Result.Failed;

            try
            {
                DeleteBucketRequest request = new DeleteBucketRequest() {BucketName = bucketModel.Name};
                bucketModel.S3Client.DeleteBucket(request);

                deleteResult = Result.Succeeded;
                return true;
            }
            catch (Exception e)
            {
                deleteResult = Result.Failed;
                ShowDeletionError(bucketModel, e);
                return false;
            }
            finally
            {
                RecordMetric(deleteResult);
            }
        }

        private async Task DeleteBucketAndContentsAsync(S3BucketViewModel bucketModel)
        {
            Result deleteResult = Result.Failed;

            try
            {
                // Add this bucket to in-progress list.
                bucketModel.PendingDeletion = true;
                bucketModel.AddToBucketDeleteList();

                await AmazonS3Util.DeleteS3BucketWithObjectsAsync(
                    bucketModel.S3Client,
                    bucketModel.Name,
                    new S3DeleteBucketWithObjectsOptions {ContinueOnError = false, QuietMode = false},
                    null,
                    default(CancellationToken)
                );

                deleteResult = Result.Succeeded;
            }
            catch (Exception e)
            {
                deleteResult = Result.Failed;
                _syncContext.Post((object state) =>
                    {
                        ShowDeletionError(bucketModel, e);
                        bucketModel.PendingDeletion = false;
                    },
                    null);
            }
            finally
            {
                RecordMetric(deleteResult);

                // Remove this bucket from In-Progress list.
                bucketModel.RemoveFromBucketDeleteList();

                // Refresh the bucket list.
                _syncContext.Post((object state) => { bucketModel.S3RootViewModel.Refresh(false); },
                    null);
            }
        }

        private void ShowDeletionError(S3BucketViewModel bucketModel, Exception e)
        {
            _shellProvider.ShowError($"Error deleting bucket: {bucketModel.Name}{Environment.NewLine}{e.Message}");
        }

        private void RecordMetric(Result deleteResult)
        {
            _toolkitContext.TelemetryLogger.RecordS3DeleteBucket(new S3DeleteBucket()
            {
                AwsAccount = _toolkitContext.ConnectionManager.ActiveAccountId,
                AwsRegion = _toolkitContext.ConnectionManager.ActiveRegion.Id,
                Result = deleteResult,
            });
        }
    }
}
