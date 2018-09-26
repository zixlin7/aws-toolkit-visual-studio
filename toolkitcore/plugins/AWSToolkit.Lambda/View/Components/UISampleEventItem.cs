namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    ///     Representation of a entry within the Sample Lambda Event Picker (<see cref="SampleEventPicker" />).
    /// </summary>
    public abstract class UISampleEventItem
    {
        public abstract string Name { get; }
        public abstract bool IsSelectable { get; }
    }
}