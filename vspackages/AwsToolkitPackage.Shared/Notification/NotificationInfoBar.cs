using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using log4net;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    /// <summary>
    /// Manages an InfoBar (and its events) related to surfacing notifications to users
    /// </summary>
    public class NotificationInfoBar : IVsInfoBarUIEvents
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(NotificationInfoBar));

        private readonly Notification _notification;
        private readonly NotificationStrategy _strategy;
        private readonly ToolkitContext _toolkitContext;
        private readonly List<IVsInfoBarActionItem> _actions;
        private IVsInfoBarUIElement _registeredInfoBarElement;
        private uint _infoBarElementCookie;
        private const string _defaultCulture = "en-US";
        private readonly List<string> _cultureSearchOrder = new List<string> { CultureInfo.CurrentCulture.Name, CultureInfo.CurrentCulture.TwoLetterISOLanguageName };

        public InfoBarModel InfoBarModel { get; }

        public NotificationInfoBar(Notification notification, NotificationStrategy strategy, ToolkitContext toolkitContext)
        {
            _notification = notification;
            _strategy = strategy;
            _toolkitContext = toolkitContext;
            _actions = new List<IVsInfoBarActionItem>();
            InfoBarModel = CreateInfoBar();
        }

        private InfoBarModel CreateInfoBar()
        {
            return new InfoBarModel(
                textSpans: new[] {new InfoBarTextSpan(GetLocalizedText(_notification.Content, _cultureSearchOrder)) },
                actionItems: GetActionItems(),
                image: KnownMonikers.StatusInformation,
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

                if (!(actionItem?.ActionContext is INotificationAction notificationAction))
                {
                    return;
                }

                notificationAction.InvokeAsync(_strategy).LogExceptionAndForget();

                if (!notificationAction.DisplayAgain)
                {
                    _strategy.MarkNotificationAsDismissedAsync(_notification).LogExceptionAndForget();
                }

                if (notificationAction.Dismiss)
                {
                    infoBarUiElement.Close();
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

        private IEnumerable<IVsInfoBarActionItem> GetActionItems()
        {
            _notification.Actions?.ForEach(ParseAction);

            _actions.Add(CreateActionItem(new DontShowAgainAction(), GetLocalizedDontShowAgainDisplayText(), Gesture.Link));

            return _actions;
        }

        private void ParseAction(NotificationAction action)
        {
            try
            {
                var actionContext = (ActionContexts) Enum.Parse(typeof(ActionContexts), action.ActionId, true);
                var gesture = (Gesture) Enum.Parse(typeof(Gesture), action.Gesture, true);

                switch (actionContext)
                {
                    case ActionContexts.ShowMarketplace:
                        _actions.Add(CreateActionItem(new ShowMarketplaceAction(_toolkitContext, _notification.NotificationId), GetLocalizedText(action.DisplayText, _cultureSearchOrder), gesture));
                        break;
                    case ActionContexts.ShowUrl:
                        var url = action.Args["url"];
                        _actions.Add(CreateActionItem(new ShowUrlAction(_toolkitContext, _notification.NotificationId, url), GetLocalizedText(action.DisplayText, _cultureSearchOrder), gesture));
                        break;
                    case ActionContexts.None:
                        break;
                }
                _strategy.RecordToolkitShowActionMetric(Result.Succeeded, actionContext, _notification.NotificationId, null);
            }
            catch (NotificationToolkitException toolkitException)
            {
                _logger.Error(toolkitException.Message, toolkitException);
                _strategy.RecordToolkitShowActionMetric(Result.Failed, ActionContexts.None, _notification.NotificationId, toolkitException);
            }
            catch (Exception e)
            {
                const string message = "Error parsing notification action items";
                _logger.Error(message, e);
                _strategy.RecordToolkitShowActionMetric(Result.Failed, ActionContexts.None, _notification.NotificationId, new NotificationToolkitException(message, NotificationToolkitException.NotificationErrorCode.UnsupportedActionContext, e));
            }
        }

        private static IVsInfoBarActionItem CreateActionItem(object actionContext, string displayText, Gesture gesture)
        {
            switch (gesture)
            {
                case Gesture.Button:
                    return new InfoBarButton(displayText, actionContext);
                case Gesture.Link:
                    return new InfoBarHyperlink(displayText, actionContext);
                default:
                    throw new NotificationToolkitException("Error parsing action item gesture",
                        NotificationToolkitException.NotificationErrorCode.UnsupportedGesture);
            }
        }

        /// <summary>
        /// Gets static localized text from resx files
        /// </summary>
        /// <returns>
        /// DontShowAgain value from Notification.{culture}.resx. || Notification.{language}.resx || Notification.resx
        /// </returns>
        public string GetLocalizedDontShowAgainDisplayText()
        {
            return Properties.Localization.Notification.DontShowAgain;
        }

        /// <summary>
        /// Gets dynamic localized text from NotificationModel
        /// </summary>
        public static string GetLocalizedText(Dictionary<string, string> locales, List<string> cultureSearchOrder)
        {
            foreach (var culture in cultureSearchOrder)
            {
                if (locales.TryGetValue(culture, out var localizedText))
                {
                    return localizedText;
                }
            }

            return locales[_defaultCulture];
        }
    }
}
