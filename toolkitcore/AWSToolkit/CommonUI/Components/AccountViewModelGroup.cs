namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Simple record class representing how a set of accounts is grouped,
    /// and what that group's sort priority is.
    /// </summary>
    public class AccountViewModelGroup
    {
        public string GroupName;

        /// <summary>
        /// A lower priority value would be sorted before a higher value
        /// </summary>
        public int SortPriority;

        // Used by the implicit UI Binding
        public override string ToString()
        {
            return GroupName;
        }
    }
}
