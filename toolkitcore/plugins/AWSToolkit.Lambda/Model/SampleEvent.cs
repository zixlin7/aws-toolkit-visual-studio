namespace Amazon.AWSToolkit.Lambda.Model
{
    /// <summary>
    ///     Describes a Sample Lambda Event
    /// </summary>
    public class SampleEvent
    {
        /// <summary>
        ///     Group that an event belongs to.
        ///     May or may not be surfaced to a UI.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        ///     User facing name of the Sample Event
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        ///     Filename of the Sample Event
        /// </summary>
        public string Filename { get; set; }
    }
}