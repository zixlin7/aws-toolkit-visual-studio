using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit;

using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;
using Amazon.AWSToolkit.CommonUI;
using System.Threading;

namespace Amazon.AWSToolkit.S3.Controller
{
    /// <summary>
    /// The controller for Delte Bucket action.
    /// </summary>
    public class DeleteBucketController : BaseContextCommand
    {
        /// <summary>
        /// Stores the synchronization context. This is used in callback methods to
        /// update the UI using the UI thread.
        /// </summary>
        private System.Threading.SynchronizationContext _syncContext;

        /// <summary>
        /// The default constructor.
        /// </summary>
        public DeleteBucketController()
        {
            _syncContext = System.Threading.SynchronizationContext.Current;
        }

        /// <summary>
        /// This method is invoked when the action is executed.
        /// </summary>
        /// <param name="model">The model on which the action is executed.</param>
        /// <returns>Result of the action.</returns>
        public override ActionResults Execute(IViewModel model)
        {
            S3BucketViewModel bucketModel = model as S3BucketViewModel;
            if (bucketModel == null)
                return new ActionResults().WithSuccess(false);
            
            // Display the delete confirmation prompt.
            var control = new DeleteBucketControl(bucketModel.Name);            
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(control, MessageBoxButton.OKCancel))
            {
                try
                {                    
                    if (control.DeleteBucketWithObjects)
                    {
                        // If bucket is to be deleted along with the objects in it.
                        // Add this bucket to in-progress list.
                        bucketModel.PendingDeletion = true;
                        ((S3RootViewMetaNode)bucketModel.S3RootViewModel.MetaNode).AddBucketToDeleteList(bucketModel);

                        AmazonS3Util.DeleteS3BucketWithObjectsAsync(
                            bucketModel.S3Client,
                            model.Name,
                            new S3DeleteBucketWithObjectsOptions
                            {
                                ContinueOnError = false,
                                QuietMode = false
                            },
                            null,
                            default(CancellationToken)
                            )
                            .ContinueWith(task =>
                            {
                                if (task.Exception != null)
                                {
                                    _syncContext.Post((object state) => 
                                    {
                                        ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting bucket: " + task.Exception.Message);
                                        bucketModel.PendingDeletion = false;
                                    },
                                    null);
                                }

                                // Remove this bucket from In-Progress list.
                                ((S3RootViewMetaNode)bucketModel.S3RootViewModel.MetaNode).RemoveBucketFromDeleteList(bucketModel);

                                // Refresh the bucket list.
                                _syncContext.Post((object state) => { bucketModel.S3RootViewModel.Refresh(false); }, null);

                                return new ActionResults()
                                    .WithSuccess(task.Exception == null)
                                    .WithFocalname(model.Name)
                                    .WithShouldRefresh(true);
                            });
                    }
                    else
                    {
                        // If only the bucket is to be deleted.
                        // Invoke simple delete bucket.
                        DeleteBucketRequest request = new DeleteBucketRequest() { BucketName = model.Name };
                        bucketModel.S3Client.DeleteBucket(request);

                        return new ActionResults()
                            .WithSuccess(true)
                            .WithFocalname(model.Name)
                            .WithShouldRefresh(true);
                    }
                }
                catch (Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting bucket: " + e.Message);
                    return new ActionResults().WithSuccess(false);
                }
            }
                        
            return new ActionResults().WithSuccess(false);
        }
    }
}
