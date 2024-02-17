using System.ComponentModel.Composition;

using Amazon.AwsToolkit.CodeWhisperer.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AwsToolkit.CodeWhisperer.Telemetry;

namespace Amazon.AwsToolkit.CodeWhisperer.Mef
{
    /// <summary>
    /// A MEF component containing central CodeWhisperer componenets that are not automatically activated by
    /// Visual Studio or the VS SDK.
    ///
    /// These are components we want to instantiate shortly after the IDE starts.
    /// By placing them here, we import this class in one of the components that *is*
    /// instantiated by Visual Studio (<seealso cref="Margins.CodeWhispererMarginProvider"/>), and this avoids 
    /// having a list of arbitrary imports in that class.
    ///
    /// Components imported here are typically systems that react to other events.
    /// </summary>
    [Export(typeof(CentralComponents))]
    internal class CentralComponents
    {
        [ImportingConstructor]
        public CentralComponents(
            IExpirationNotificationManager expirationNotificationManager,
            ICodeWhispererSettingsPublisher codeWhispererSettingsPublisher,
            ICodeWhispererTelemetryEventPublisher codeWhispererTelemetryEventPublisher,
            ICodeWhispererEnabledStateEmitter enabledStateEmitter,
            EnabledStateManager enabledStateManager)
        {
        }
    }
}
