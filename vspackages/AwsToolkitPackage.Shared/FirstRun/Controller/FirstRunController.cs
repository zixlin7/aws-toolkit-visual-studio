﻿using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Model;
using Amazon.AWSToolkit.VisualStudio.FirstRun.View;

using log4net;

using Microsoft.VisualStudio.Shell;

using Task = System.Threading.Tasks.Task;

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
        private readonly ToolkitSettings _toolkitSettings;

        public FirstRunController(AWSToolkitPackage hostPackage,
            IToolkitSettingsWatcher toolkitSettingsWatcher,
            ToolkitContext toolkitContext)
            : this(hostPackage, toolkitSettingsWatcher, toolkitContext,
                ToolkitFactory.Instance.ShellProvider,
                ToolkitSettings.Instance)
        {
        }

        public FirstRunController(
            AWSToolkitPackage hostPackage,
            IToolkitSettingsWatcher toolkitSettingsWatcher,
            ToolkitContext toolkitContext,
            IAWSToolkitShellProvider shellProvider,
            ToolkitSettings toolkitSettings)
        {
            _shellProvider = shellProvider;
            _toolkitSettings = toolkitSettings;
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
            }
            catch (Exception e)
            {
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
                _toolkitSettings.TelemetryEnabled = Model.CollectAnalytics;
            }
        }

        private void ToolkitSettingsChanged(object sender, EventArgs e)
        {
            if (Model.CollectAnalytics != _toolkitSettings.TelemetryEnabled)
            {
                _shellProvider.ExecuteOnUIThread(() =>
                {
                    Model.CollectAnalytics = _toolkitSettings.TelemetryEnabled;
                });
            }
        }

        public void RecordAwsHelpQuickstartMetric(bool success)
        {
            ToolkitContext.TelemetryLogger.RecordAwsHelpQuickstart(new AwsHelpQuickstart()
            {
                AwsAccount = _toolkitContext.ConnectionManager.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = MetadataValue.NotApplicable,
                Result = success ? Result.Succeeded : Result.Failed
            });
        }

        public void RecordAwsModifyCredentialsMetric(bool success, CredentialModification modification)
        {
            ToolkitContext.TelemetryLogger.RecordAwsModifyCredentials(new AwsModifyCredentials()
            {
                AwsAccount = _toolkitContext.ConnectionManager.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = MetadataValue.NotApplicable,
                Result = success ? Result.Succeeded : Result.Failed,
                CredentialModification = modification,
                Source = _control.UniqueId
            });
        }

        public async Task ShowAwsExplorerAsync()
        {
            await _shellProvider.OpenShellWindowAsync(ShellWindows.Explorer);
        }

        public void CloseEditor(IAWSToolkitControl firstRunControl)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                _shellProvider.CloseEditor(firstRunControl);
            });
        }
    }
}
