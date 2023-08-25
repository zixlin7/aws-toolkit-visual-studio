using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Behaviors
{
    public static class UiClickMetric
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(UiClickMetric));

        private static ToolkitContext _toolkitContext;

        /// <summary>
        /// Initializes UiClickMetric.
        /// </summary>
        /// <param name="toolkitContext">ToolkitContext used by whole toolkit.</param>
        /// <returns>Indicates success/failure of completing async method.</returns>
        /// <remarks>
        /// For consistency with other initialization methods that accept ToolkitContext,
        /// this method is async, though it does not currently perform async work, it
        /// may need to in the future.
        /// </remarks>
        public static Task InitializeAsync(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles UiClickMetricConfig.EventName events to post UiClick metrics with given ElementId.
        /// </summary>
        public static readonly DependencyProperty ConfigureProperty = DependencyProperty.RegisterAttached(
            "Configure",
            typeof(UiClickMetricConfig),
            typeof(UiClickMetric),
            new FrameworkPropertyMetadata(Configure_PropertyChanged));

        /// <summary>
        /// Gets the Configure attached property from DependencyObject instances.
        /// </summary>
        /// <param name="d">The DependencyObject to get Configure from.</param>
        /// <returns>The UiClickMetricConfig object attached to the DependencyObject.</returns>
        public static UiClickMetricConfig GetConfigure(DependencyObject d)
        {
            return (UiClickMetricConfig) d.GetValue(ConfigureProperty);
        }

        /// <summary>
        /// Sets the Configure attached property on DependencyObject instances.
        /// </summary>
        /// <param name="d">The DependencyObject to set Configure on.</param>
        /// <param name="value">The UiClickMetricConfig to set on the DependencyObject.</param>
        public static void SetConfigure(DependencyObject d, UiClickMetricConfig value)
        {
            d.SetValue(ConfigureProperty, value);
        }

        private static void Configure_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var config = e.NewValue as UiClickMetricConfig;
            if (config == null)
            {
                _logger.Warn($"{ConfigureProperty.Name} cannot be null.");
                BreakInDebugOnly();
                return;
            }

            if (string.IsNullOrWhiteSpace(config.EventName))
            {
                _logger.Warn($"{nameof(UiClickMetricConfig.EventName)} must be set.");
                BreakInDebugOnly();
                return;
            }

            var eventInfo = d.GetType().GetEvent(config.EventName);
            if (eventInfo == null)
            {
                _logger.Warn($"Event '{config.EventName}' does not exist on {d.GetType().Name}.");
                BreakInDebugOnly();
                return;
            }

            AddEventHandler(d, eventInfo, config);
        }

        static UiClickMetric()
        {
            _recordUiClickMetricMethodInfo = typeof(UiClickMetric).GetMethod(nameof(RecordUiClickMetric),
                BindingFlags.Static | BindingFlags.NonPublic);
        }

        private static readonly MethodInfo _recordUiClickMetricMethodInfo;

        private const int _maxCSharpIdentifierNameLength = 512;

        private static string GetElementId(UiClickMetricConfig config, object element)
        {
            return config.ElementId ?? (element as FrameworkElement)?.Name ?? (element as FrameworkContentElement)?.Name;
        }

        private static void AddEventHandler(object target, EventInfo eventInfo, UiClickMetricConfig config)
        {
            try
            {
                // We create a DynamicMethod here in the correct prototype for the event we're listening
                // to.  This is required as even though contravariances allows a call with args
                // (object sender, RoutedEventArgs e) to be passed to a method defined as an EventHandler
                // (object sender, EventArgs e), adding an event handler using a Delegate instance
                // requires the exact prototype.  The DynamicMethod just calls RecordUiClickMetric passing
                // the first parameter (sender) that it receives.  The EventArgs are not needed.

                // Get the delegate prototype by reflecting the Invoke method of the event handler type (delegate)
                var methodInfo = eventInfo.EventHandlerType.GetMethod("Invoke");
                var paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();

                // The handler name could just be an empty string, but generate something useful for debugging
                var handlerName = $"{nameof(UiClickMetric)}_{GetElementId(config, target)}";
                if (handlerName.Length > _maxCSharpIdentifierNameLength)
                {
                    handlerName = handlerName.Substring(0, _maxCSharpIdentifierNameLength);
                }

                var handler = new DynamicMethod(handlerName, methodInfo.ReturnType, paramTypes, typeof(UiClickMetric));

                // Generate the DynamicMethod body from IL.  The easiest way to see what the IL should look like is create
                // the code you want in C# then examine the code in the built assembly using ILSpy or a similar tool.
                // The IL here is effectively:
                //
                // private static void UiClickMetric_someElementId(object sender, HandlerSpecificEventArgs e)
                // {
                //     UiClickMetric.RecordUiClickMetric(sender);
                // }
                var il = handler.GetILGenerator();
                il.Emit(OpCodes.Ldarg_0); // Pass "sender" on to RecordUiClickMetric
                il.Emit(OpCodes.Call, _recordUiClickMetricMethodInfo);
                il.Emit(OpCodes.Ret);

                eventInfo.AddEventHandler(target, handler.CreateDelegate(eventInfo.EventHandlerType));
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to create and add event handler for UiClick metrics.", ex);
                BreakInDebugOnly();
            }
        }

        private static readonly ActionResults _actionResults = new ActionResults();

        private static void RecordUiClickMetric(DependencyObject d)
        {
            if (_toolkitContext == null)
            {
                _logger.Warn($"{nameof(ToolkitContext)} not set, call {nameof(InitializeAsync)} method first.");
                BreakInDebugOnly();
                return;
            }

            var config = GetConfigure(d);
            if (config == null)
            {
                _logger.Warn($"{ConfigureProperty.Name} cannot be null.");
                BreakInDebugOnly();
                return;
            }

            var data = _actionResults.CreateMetricData<UiClick>(MetadataValue.NotApplicable, MetadataValue.NotApplicable);
            data.ElementId = GetElementId(config, d);
            if (string.IsNullOrWhiteSpace(data.ElementId))
            {
                _logger.Warn($"{nameof(data.ElementId)} must be set or derivable from DependencyObject Name property.");
                BreakInDebugOnly();
                return;
            }

            _toolkitContext.TelemetryLogger.RecordUiClick(data);
        }

        /// <summary>
        /// Prompts developer to open debugger (in debug builds only) if UiClick metrics are misconfigured.
        /// </summary>
        /// <remarks>
        /// DebuggerStepThroughAttribute is used so that the break point is the caller rather than in this utility method.
        ///
        /// Debugger.Launch() is called to ensure the developer is notified of the misconfiguration.  Debugger.Break()
        /// is insufficient because as of .NET Framework 4.0 the runtime does not prompt a user to launch a debugger unless
        /// the user has specifically configured their system to do so.  Break() will only break if a debugger is already
        /// attached in the default setup.
        /// </remarks>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private static void BreakInDebugOnly()
        {
            Debugger.Launch();
        }
    }

    /// <summary>
    /// Configuration to support posting a UiClick metric.
    /// </summary>
    /// <remarks>
    /// Binding and participating in WPF lifetime events on objects (including FrameworkElements)
    /// in attached properties like UiClickMetricConfig in Configure doesn't work, likely
    /// because they aren't in the visual tree.  Adding ToolkitContext or attempting to convert
    /// this class to derive from FrameworkElement or FrameworkContentElement to support binding
    /// and loaded events doesn't work.
    /// </remarks>
    public class UiClickMetricConfig
    {
        /// <summary>
        /// The event on the object attached to for which to post a UiClick metric.
        /// </summary>
        /// <remarks>
        /// This can be any event, regardless of the name UiClick, this need not be a click event.
        /// </remarks>
        public string EventName { get; set; } = "Click";

        /// <summary>
        /// The ElementId to post with the UiClick metric.
        /// </summary>
        /// <remarks>
        /// If ElementId is set, it will override the default behavior of using the element Name
        /// property to which this object is attached.  This can also be useful in cases where
        /// the same element ID needs to be used more than once in the same naming scope.  This
        /// property can also be set if the element ID contains values that are not valid for a
        /// C# identifier.
        /// </remarks>
        public string ElementId { get; set; }
    }
}
