using System;
using System.ComponentModel.Composition;
using System.Threading;

using log4net;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

namespace Amazon.AwsToolkit.VsSdk.Common.Tasks
{
    /// <summary>
    /// Responsible for producing a JoinableTaskFactory (JTF) for the Toolkit's MEF components.
    /// The JTF lives within Visual Studio's task context.
    ///
    /// This helps provide MEF components with a proper JTF without needing to obtain the Toolkit's
    /// AsyncPackage JTF.
    ///
    /// Reference: https://github.com/microsoft/vs-threading/blob/main/doc/cookbook_vs.md
    /// </summary>
    [Export(typeof(ToolkitJoinableTaskFactoryProvider))]
    public class ToolkitJoinableTaskFactoryProvider : IDisposable
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ToolkitJoinableTaskFactoryProvider));
        private readonly CancellationTokenSource _disposalTokenSource = new CancellationTokenSource();
        private readonly JoinableTaskCollection _taskCollection;

        [ImportingConstructor]
        public ToolkitJoinableTaskFactoryProvider(JoinableTaskContext taskContext)
        {
            _taskCollection = taskContext.CreateCollection();
            _taskCollection.DisplayName = "AWS Toolkit for Visual Studio";
            JoinableTaskFactory = taskContext.CreateFactory(_taskCollection);
        }

        /// <summary>
        /// Allows calling code to run Tasks <see cref="JoinableTaskFactory"/> while checking if the Toolkit's MEF components
        /// are being (or have been) disposed.
        /// </summary>
        public CancellationToken DisposalToken => _disposalTokenSource.Token;

        public JoinableTaskFactory JoinableTaskFactory { get; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            if (!ThreadHelper.CheckAccess())
            {
                return;
            }

            _disposalTokenSource.Cancel();

            try
            {
                // Wait until any in-flight tasks have completed
                ThreadHelper.JoinableTaskFactory.Run(_taskCollection.JoinTillEmptyAsync);
            }
            catch (OperationCanceledException e)
            {
                // We expect this because we cancelled _disposalTokenSource
                // Nothing to do.
            }
            catch (Exception e)
            {
                _logger.Error("Error disposing the Toolkit's joinable task collection", e);
            }
            finally
            {
                _disposalTokenSource.Dispose();
            }
        }
    }
}
