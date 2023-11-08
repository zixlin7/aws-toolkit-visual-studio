using System;
using System.ComponentModel.Composition;
using System.Text;
using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AwsToolkit.VsSdk.Common.OutputWindow;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;

using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    [Export(typeof(IReferenceLogger))]
    internal class ReferenceLogger : IReferenceLogger, IDisposable
    {
        private const string _separator = "----------------------------------------";
        private const string _outputWindowName = "CodeWhisperer Reference Log";
        private static readonly Guid _outputWindowGuid = Guid.NewGuid();

        private readonly IOutputWindow _outputWindow;

        [ImportingConstructor]
        public ReferenceLogger(SVsServiceProvider serviceProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
            : this(serviceProvider, taskFactoryProvider, OutputWindowFactory)
        {
        }

        /// <summary>
        /// Overload used for testing
        /// </summary>
        public ReferenceLogger(
            SVsServiceProvider serviceProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider,
            Func<IServiceProvider, JoinableTaskFactory, IOutputWindow> fnCreateOutputWindow)
        {
            _outputWindow = fnCreateOutputWindow(serviceProvider, taskFactoryProvider.JoinableTaskFactory);
        }

        private static IOutputWindow OutputWindowFactory(IServiceProvider serviceProvider, JoinableTaskFactory taskFactory)
        {
            return taskFactory.Run(async () => await CreateOutputWindowAsync(serviceProvider, taskFactory));
        }

        private static async Task<OutputWindow> CreateOutputWindowAsync(IServiceProvider serviceProvider, JoinableTaskFactory taskFactory)
        {
            await taskFactory.SwitchToMainThreadAsync();

            var vsOutputWindow = serviceProvider.GetService<SVsOutputWindow, IVsOutputWindow>();
            Assumes.Present(vsOutputWindow);

            var outputWindow = new OutputWindow(_outputWindowGuid, _outputWindowName, vsOutputWindow);
            await outputWindow.InitializeAsync();

            return outputWindow;
        }

        public Task ShowAsync()
        {
            _outputWindow.Show();
            return Task.CompletedTask;
        }

        public Task LogReferenceAsync(LogReferenceRequest request)
        {
            _outputWindow.WriteText(CreateOutputText(request));
            return Task.CompletedTask;
        }

        private string CreateOutputText(LogReferenceRequest request)
        {
            var attributedText = GetAttributedText(request);

            var sb = new StringBuilder();
            sb.AppendLine(_separator);
            sb.AppendLine($"{request.Filename}({request.Position.Line + 1},{request.Position.Column + 1}): Accepted recommendation with license: {request.SuggestionReference.LicenseName}");
            sb.AppendLine($"From {request.SuggestionReference.Name} ({request.SuggestionReference.Url}):");
            sb.AppendLine(_separator);
            sb.AppendLine(attributedText);
            sb.AppendLine(_separator);
            sb.AppendLine();

            return sb.ToString();
        }

        /// <summary>
        /// Returns the range of text attributed with a license.
        /// When no range is known, the whole suggestion text is returned.
        /// </summary>
        private static string GetAttributedText(LogReferenceRequest request)
        {
            var length = request.SuggestionReference.EndIndex - request.SuggestionReference.StartIndex;

            return request.Suggestion.Text.Substring(request.SuggestionReference.StartIndex, length);
        }

        public void Dispose()
        {
            _outputWindow?.Dispose();
        }
    }
}
