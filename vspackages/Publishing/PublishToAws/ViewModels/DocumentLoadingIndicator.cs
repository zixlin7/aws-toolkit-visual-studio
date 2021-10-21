using System;

using log4net;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// Utility class that marks the loading state as true (on the UI thread) until
    /// this object is disposed
    /// </summary>
    public class DocumentLoadingIndicator : IDisposable
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(DocumentLoadingIndicator));

        private readonly PublishToAwsDocumentViewModel _viewModel;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        public DocumentLoadingIndicator(PublishToAwsDocumentViewModel viewModel, JoinableTaskFactory joinableTaskFactory)
        {
            _viewModel = viewModel;
            _joinableTaskFactory = joinableTaskFactory;

            SetIsLoading(true);
        }

        private void SetIsLoading(bool isLoading)
        {
            try
            {
                _joinableTaskFactory.Run(async () =>
                {
                    await _joinableTaskFactory.SwitchToMainThreadAsync();
                    _viewModel.IsLoading = isLoading;
                });
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        public void Dispose()
        {
            SetIsLoading(false);
        }
    }
}
