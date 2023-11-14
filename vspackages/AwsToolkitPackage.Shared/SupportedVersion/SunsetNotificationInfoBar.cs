using System;
using System.Collections.Generic;
using System.Threading;

using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.VsSdk.Common.Notifications;

using log4net;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.VisualStudio.SupportedVersion
{
    /// <summary>
    /// Manages an InfoBar (and its events) informing users that the 
    /// Toolkit's minimum supported version of Visual Studio will be increased.
    /// (Some versions are about to be sunset)
    /// </summary>
    public class SunsetNotificationInfoBar : IVsInfoBarUIEvents
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SunsetNotificationInfoBar));

        private IVsInfoBarUIElement _registeredInfoBarElement;
        private uint _infoBarElementCookie;
        private readonly ISunsetNotificationStrategy _strategy;
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _taskFactory;
        private readonly CancellationToken _cancellationToken;

        public InfoBarModel InfoBarModel { get; set; }

        public SunsetNotificationInfoBar(
            ISunsetNotificationStrategy strategy,
            ToolkitContext toolkitContext,
            JoinableTaskFactory taskFactory,
            CancellationToken cancellationToken)
        {
            _strategy = strategy;
            _toolkitContext = toolkitContext;
            _taskFactory = taskFactory;
            _cancellationToken = cancellationToken;
            InfoBarModel = CreateInfoBar();
        }

        private InfoBarModel CreateInfoBar()
        {
            var message = _strategy.GetMessage();

            return new InfoBarModel(
                textSpans: new[]
                {
                    new InfoBarTextSpan(message),
                },
                actionItems: CreateActionItems(),
                image: _strategy.Icon,
                isCloseButtonVisible: true);
        }

        private List<IVsInfoBarActionItem> CreateActionItems()
        {
            var actionItems = new List<IVsInfoBarActionItem>();

            if (!string.IsNullOrWhiteSpace(_strategy.GetLearnMoreUrl()))
            {
                actionItems.Add(new LearnMoreActionItem(_strategy.GetLearnMoreUrl(), _toolkitContext, _taskFactory, _cancellationToken));
            }

            actionItems.Add(new DismissSunsetNotificationActionItem(_strategy, _taskFactory, _cancellationToken));

            return actionItems;
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
                if (actionItem is ToolkitInfoBarActionItem toolkitActionItem)
                {
                    toolkitActionItem.Execute(infoBarUiElement);
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
