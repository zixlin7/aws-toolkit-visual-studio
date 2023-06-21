using System;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;

using log4net;

using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.VisualStudio.ArmPreview
{
    /// <summary>
    /// Manages an InfoBar (and its events) related to informing users about Preview support for Arm64 VS
    /// </summary>
    public class ArmPreviewInfoBar: IVsInfoBarUIEvents
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ArmPreviewInfoBar));

        private readonly ToolkitContext _toolkitContext;

        private IVsInfoBarUIElement _registeredInfoBarElement;
        private uint _infoBarElementCookie;

        public InfoBarModel InfoBarModel { get; }

        public ArmPreviewInfoBar(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;

            var imageMoniker = new ImageMoniker()
            {
                Guid = GuidList.VsImageCatalog.ImageCatalogGuid,
                Id = GuidList.VsImageCatalog.StatusInformation
            };

            InfoBarModel = new InfoBarModel(
                textSpans: new[]
                {
                    new InfoBarTextSpan("AWS Toolkit support for Arm64 Visual Studio is now in Preview"),
                },
                actionItems: CreateActionItems(),
                image: imageMoniker,
                isCloseButtonVisible: true);
        }

        private IVsInfoBarActionItem[] CreateActionItems()
        {
            var shareFeedback = new InfoBarHyperlink(ShareArmPreviewFeedbackCommand.Title, new ShareArmPreviewFeedbackCommand(_toolkitContext));
            var fileIssue = new InfoBarHyperlink(FileArmPreviewIssueCommand.Title, new FileArmPreviewIssueCommand(_toolkitContext.ToolkitHost));
            var dontShowAgain = new InfoBarHyperlink("Don't show this again", new DismissArmPreviewCommand(CloseInfoBarAsync));

            return new IVsInfoBarActionItem[]
                {
                    shareFeedback,
                    fileIssue,
                    dontShowAgain,
                };
        }

        private async Task CloseInfoBarAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            _registeredInfoBarElement.Close();
        }

        /// <summary>
        /// Event: Info bar host element has been closed
        /// </summary>
        public void OnClosed(IVsInfoBarUIElement infoBarUiElement)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                if (infoBarUiElement == _registeredInfoBarElement)
                {
                    UnregisterInfoBarEvents(infoBarUiElement);
                    _registeredInfoBarElement = null;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        /// <summary>
        /// Event: Info bar action item was fired
        /// </summary>
        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUiElement, IVsInfoBarActionItem actionItem)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (actionItem?.ActionContext is AsyncCommand asyncCommand)
                {
                    asyncCommand.Execute();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        public void RegisterInfoBarEvents(IVsInfoBarUIElement uiElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            uiElement.Advise(this, out _infoBarElementCookie);
            _registeredInfoBarElement = uiElement;
        }

        public void UnregisterInfoBarEvents(IVsInfoBarUIElement uiElement)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            uiElement.Unadvise(_infoBarElementCookie);
        }
    }
}
