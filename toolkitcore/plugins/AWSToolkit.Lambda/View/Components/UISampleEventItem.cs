namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    ///     Representation of a Sample Lambda Event, for use with <see cref="SampleEventPicker" />.
    ///     Represents Sample Events and Category based grouping of sample events.
    /// </summary>
    public abstract class UISampleEventItem
    {
        public abstract string Name { get; }
        public abstract bool IsSelectable { get; }
    }
}