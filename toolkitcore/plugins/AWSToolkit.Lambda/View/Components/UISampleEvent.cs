using Amazon.AWSToolkit.Lambda.Model;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    ///     Represents a Sample Event
    /// </summary>
    public class UISampleEvent : UISampleEventItem
    {
        public UISampleEvent(SampleEvent sampleEvent)
        {
            SampleEvent = sampleEvent;
        }

        public override string Name => SampleEvent.Name;

        public override bool IsSelectable => true;

        public SampleEvent SampleEvent { get; }
    }
}