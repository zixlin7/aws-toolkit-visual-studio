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

        private readonly InfoBarHyperlink _infoBarMoreDetails = new InfoBarHyperlink("More details");
        private readonly InfoBarHyperlink _infoBarDisable = new InfoBarHyperlink("Disable");

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

                if (actionItem == _infoBarDisable)
                {
                    ToolkitSettings.Instance.TelemetryEnabled = false;
                    infoBarUiElement.Close();
                }
                else if (actionItem == _infoBarMoreDetails)
                {
                    var dlg = new TelemetryInformationDialog();
                    dlg.ShowModal();
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