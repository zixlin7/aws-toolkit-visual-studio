using System;
using System.ComponentModel;
using System.Windows;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Model;
using Amazon.AWSToolkit.VisualStudio.FirstRun.View;
using log4net;

namespace Amazon.AWSToolkit.VisualStudio.FirstRun.Controller
{
    /// <summary>
    /// Controller class for the 'first run' experience when the toolkit
    /// determines no credentials have been set up.
    /// </summary>
    public class FirstRunController
    {
        internal static ILog LOGGER = LogManager.GetLogger(typeof(FirstRunController));

        private readonly IAWSToolkitShellProvider _shellProvider;

        protected ActionResults _results;
        private FirstRunControl _control;
        private readonly IToolkitSettingsWatcher _settingsWatcher;
        private readonly ToolkitContext _toolkitContext;

        public FirstRunController(AWSToolkitPackage hostPackage, IToolkitSettingsWatcher toolkitSettingsWatcher, ToolkitContext toolkitContext)
            : this(hostPackage, toolkitSettingsWatcher, ToolkitFactory.Instance.ShellProvider, toolkitContext)
        {
        }

        public FirstRunController(
            AWSToolkitPackage hostPackage,
            IToolkitSettingsWatcher toolkitSettingsWatcher,
            IAWSToolkitShellProvider shellProvider, ToolkitContext toolkitContext)
        {
            _shellProvider = shellProvider;
            HostPackage = hostPackage;
            _toolkitContext = toolkitContext;
            Model = new FirstRunModel(_toolkitContext);
            Model.PropertyChanged += ModelOnPropertyChanged;

            _settingsWatcher = toolkitSettingsWatcher;
            _settingsWatcher.SettingsChanged += ToolkitSettingsChanged;
        }

        public FirstRunModel Model { get; }

        public ToolkitContext ToolkitContext => _toolkitContext;

        public AWSToolkitPackage HostPackage { get; }

        public ActionResults Execute()
        {
            try
            {
                this._control = new FirstRunControl(this);
                _shellProvider.OpenInEditor(this._control);
                _control.Unloaded += FirstRunControlUnloaded;

                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.FirstExperienceDisplayStatus, ToolkitEvent.COMMON_STATUS_SUCCESS);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

            }
            catch (Exception e)
            {
                ToolkitEvent evnt = new ToolkitEvent();
                evnt.AddProperty(AttributeKeys.FirstExperienceDisplayStatus, ToolkitEvent.COMMON_STATUS_FAILURE);
                SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);

                LOGGER.Error("First run controller caught exception loading control", e);
            }

            return new ActionResults().WithSuccess(true);
        }

        private void FirstRunControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (!(sender is FirstRunControl control))
            {
                return;
            }

            control.Unloaded -= FirstRunControlUnloaded;
            _settingsWatcher.SettingsChanged -= ToolkitSettingsChanged;
        }

        public void OpenInBrowser(string endpoint)
        {
            this.HostPackage.JoinableTaskFactory.Run(async () =>
            {
                await this.HostPackage.JoinableTaskFactory.SwitchToMainThreadAsync();
                HostPackage.ToolkitShellProviderService.OpenInBrowser(endpoint, true);
            });            
        }

        private void ModelOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            // ignore our self-inflicted updates
            if (propertyChangedEventArgs.PropertyName.Equals("IsValid", StringComparison.OrdinalIgnoreCase))
                return;

            Model.IsValid = !string.IsNullOrEmpty(Model.AccessKey) && !string.IsNullOrEmpty(Model.SecretKey);

            if (propertyChangedEventArgs.PropertyName.Equals("CollectAnalytics", StringComparison.OrdinalIgnoreCase))
            {
                // Update settings
                ToolkitSettings.Instance.TelemetryEnabled = Model.CollectAnalytics;
            }
        }

        private void ToolkitSettingsChanged(object sender, EventArgs e)
        {
            if (Model.CollectAnalytics != ToolkitSettings.Instance.TelemetryEnabled)
            {
                _shellProvider.ExecuteOnUIThread(() =>
                {
                    Model.CollectAnalytics = ToolkitSettings.Instance.TelemetryEnabled;
                });
            }
        }
    }
}
