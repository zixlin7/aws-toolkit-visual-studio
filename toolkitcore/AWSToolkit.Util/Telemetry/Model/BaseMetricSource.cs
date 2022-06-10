namespace Amazon.AWSToolkit.Telemetry.Model
{
    /// <summary>
    /// Represents "where" an operation originated.
    /// This class backs the "serviceType" and "source" Telemetry fields.
    ///
    /// Plugins should define their own sources, relevant to the gestures provided in their UIs.
    /// </summary>
    public abstract class BaseMetricSource
    {
        /// <summary>
        /// The service this operation related to
        /// This should be a service name - see <see cref="Amazon.AWSToolkit.Regions.ServiceNames"/>
        /// Optional - places like the AWS Explorer don't have a relevant service.
        /// </summary>
        public string Service { get; }

        /// <summary>
        /// The UX gesture where the operation was initiated
        /// </summary>
        public string Location { get; }

        protected BaseMetricSource(string service, string location)
        {
            Service = service;
            Location = location;
        }
    }
}
