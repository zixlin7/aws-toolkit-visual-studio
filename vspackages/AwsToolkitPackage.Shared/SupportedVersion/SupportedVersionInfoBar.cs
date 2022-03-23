using System;


using log4net;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Manages an InfoBar (and its events) related to informing users about
    /// minimum version of IDE supported by the toolkit
    /// </summary>
    public class SupportedVersionInfoBar : IVsInfoBarUIEvents
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(SupportedVersionInfoBar));

        private readonly SupportedVersionStrategy _supportedVersionStrategy;
        private readonly InfoBarHyperlink _infoBarDontShowAgain =
            new InfoBarHyperlink("Don't show this again", ActionContexts.DontShowAgain);

        private IVsInfoBarUIElement _registeredInfoBarElement;
        private uint _infoBarElementCookie;

        public InfoBarModel InfoBarModel { get; set; }

        public enum ActionContexts
        {
            DontShowAgain,
        }

        public SupportedVersionInfoBar(SupportedVersionStrategy supportedVersionStrategy)
        {
            _supportedVersionStrategy = supportedVersionStrategy;
            InfoBarModel = CreateInfoBar();
        }

        private InfoBarModel CreateInfoBar()
        {
            var imageMoniker = KnownMonikers.StatusInformation;
            var message = _supportedVersionStrategy.GetMessage();
            return new InfoBarModel(
                textSpans: new[]
                {
                    new InfoBarTextSpan(message),
                },
                actionItems: new[] { _infoBarDontShowAgain },
                image: imageMoniker,
                isCloseButtonVisible: true);
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
                Logger.Error(e);
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
                if (actionItem?.ActionContext is ActionContexts actionContext)
                {
                    switch (actionContext)
                    {
                        case ActionContexts.DontShowAgain:
                            _supportedVersionStrategy.MarkNoticeAsShown();
                            infoBarUiElement.Close();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
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
