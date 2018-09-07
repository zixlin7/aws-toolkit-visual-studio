namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    ///     Represents a grouping that Sample Events can be associated with.
    /// </summary>
    public class UISampleEventCategory : UISampleEventItem
    {
        public UISampleEventCategory(string category)
        {
            Name = category;
        }

        public override string Name { get; }

        public override bool IsSelectable => false;
    }
}