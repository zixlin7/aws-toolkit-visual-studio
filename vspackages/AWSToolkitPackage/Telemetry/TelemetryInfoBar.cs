using System;
using Amazon.AWSToolkit.Settings;
using log4net;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Telemetry
{
    /// <summary>
    /// Manages an InfoBar (and its events) related to informing users about Toolkit Telemetry
    /// </summary>
    public class TelemetryInfoBar: IVsInfoBarUIEvents
    {
        static readonly ILog Logger = LogManager.GetLogger(typeof(TelemetryInfoBar));

        public enum ActionContexts
        {
            MoreDetails,
            Disable,
            DontShowAgain,
        }

        private readonly InfoBarHyperlink _infoBarMoreDetails = new InfoBarHyperlink("More details", ActionContexts.MoreDetails);
        private readonly InfoBarHyperlink _infoBarDisable = new InfoBarHyperlink("Disable", ActionContexts.Disable);
        private readonly InfoBarHyperlink _infoBarDontShowAgain = new InfoBarHyperlink("Don't show this again", ActionContexts.DontShowAgain);

        private IVsInfoBarUIElement _registeredInfoBarElement;
        private uint _infoBarElementCookie;

        public InfoBarModel InfoBarModel { get; }

        public TelemetryInfoBar()
        {
            var imageMoniker = new ImageMoniker()
            {
                Guid = GuidList.VsImageCatalog.ImageCatalogGuid,
                Id = GuidList.VsImageCatalog.StatusInformation
            };

            InfoBarModel = new InfoBarModel(
                textSpans: new[]
                {
                    new InfoBarTextSpan("AWS Toolkit collects usage information to improve its features."),
                },
                actionItems: new[]
                {
                    _infoBarMoreDetails,
                    _infoBarDisable,
                    _infoBarDontShowAgain,
                },
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
                    TelemetryNotice.MarkNoticeAsShown();

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
                        case ActionContexts.Disable:
                            ToolkitSettings.Instance.TelemetryEnabled = false;
                            infoBarUiElement.Close();
                            break;
                        case ActionContexts.MoreDetails:
                        {
                            var dlg = new TelemetryInformationDialog();
                            dlg.ShowModal();
                            break;
                        }
                        case ActionContexts.DontShowAgain:
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