using System;
using log4net;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace AwsToolkit.VsSdk.Common.Notifications
{
    /// <summary>
    /// Represents toolkit specific info bars
    /// </summary>
    public abstract class ToolkitInfoBar: IVsInfoBarUIEvents
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ToolkitInfoBar));
        private IVsInfoBarUIElement _registeredInfoBarElement;
        private uint _infoBarElementCookie;

        public InfoBarModel InfoBarModel { get; set; }

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

        public void OnActionItemClicked(IVsInfoBarUIElement infoBarUiElement, IVsInfoBarActionItem actionItem)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                HandleActionItemClicked(infoBarUiElement, actionItem);
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
        }

        protected abstract void HandleActionItemClicked(IVsInfoBarUIElement infoBarUiElement,
            IVsInfoBarActionItem actionItem);

        // Note: should initialize the infoBarModel as part of the constructor of the derived class
        protected abstract InfoBarModel CreateInfoBar();
    }
}
