using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AwsToolkit.VsSdk.Common.Tasks;
using Amazon.AWSToolkit.Context;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Settings
{
    /// <summary>
    /// MEF Component interface responsible for emitting metrics whenever CodeWhisperer is enabled
    /// or disabled. It also records the current state on startup.
    /// </summary>
    public interface ICodeWhispererEnabledStateEmitter
    {
    }

    /// <summary>
    /// CodeWhisperer MEF component responsible for emitting "is CodeWhisperer enabled?" metrics
    /// </summary>
    [Export(typeof(ICodeWhispererEnabledStateEmitter))]
    internal class CodeWhispererEnabledStateEmitter : ICodeWhispererEnabledStateEmitter, IDisposable
    {
        private static class MetricSources
        {
            public const string AtStartup = "startup";
            public const string InSettings = "settings";
        }

        private static readonly ILog _logger = LogManager.GetLogger(typeof(CodeWhispererEnabledStateEmitter));

        private readonly ICodeWhispererSettingsRepository _settingsRepository;
        private readonly IToolkitContextProvider _toolkitContextProvider;
        private bool _isDisposed = false;
        private bool? _previousEnabledState = null;

        [ImportingConstructor]
        public CodeWhispererEnabledStateEmitter(ICodeWhispererSettingsRepository settingsRepository,
            IToolkitContextProvider toolkitContextProvider,
            ToolkitJoinableTaskFactoryProvider taskFactoryProvider)
        {
            _settingsRepository = settingsRepository;
            _toolkitContextProvider = toolkitContextProvider;

            taskFactoryProvider.JoinableTaskFactory.Run(InitializeAsync);
        }

        private async Task InitializeAsync()
        {
            var settings = await _settingsRepository.GetAsync();
            _previousEnabledState = settings.IsEnabled;

            // Emit the current value once the Toolkit is initialized
            _toolkitContextProvider.RegisterOnInitializedCallback(() => RecordEnabledState(MetricSources.AtStartup, settings.IsEnabled));

            // Now that we've loaded settings, emit whenever the enabled state changes
            _settingsRepository.SettingsSaved += OnSettingsRepositorySaved;
        }

        private void OnSettingsRepositorySaved(object sender, CodeWhispererSettingsSavedEventArgs e)
        {
            if (!_previousEnabledState.HasValue || _previousEnabledState.Value != e.Settings.IsEnabled)
            {
                RecordEnabledState(MetricSources.InSettings, e.Settings.IsEnabled);
            }

            _previousEnabledState = e.Settings.IsEnabled;
        }

        private void RecordEnabledState(string source, bool isEnabled)
        {
            try
            {
                _toolkitContextProvider.GetToolkitContext().TelemetryLogger.RecordCodewhispererEnabled(new CodewhispererEnabled()
                {
                    Source = source,
                    Enabled = isEnabled,
                    AwsAccount = MetadataValue.NotApplicable,
                    AwsRegion = MetadataValue.NotApplicable,
                });
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to emit CodeWhisperer Enabled metric", ex);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _settingsRepository.SettingsSaved -= OnSettingsRepositorySaved;
                }

                _isDisposed = true;
            }
        }
    }
}
