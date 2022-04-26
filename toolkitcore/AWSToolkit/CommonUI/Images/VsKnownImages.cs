namespace Amazon.AWSToolkit.CommonUI.Images
{
    /// <summary>
    /// Images used by the Toolkit that are provided by the Visual Studio Image Service.
    /// Images are surfaced through <seealso cref="VsImages"/>.
    ///
    /// Enum values added here must have a corresponding KnownMonikers property.
    /// There is a test that verifies the enum values.
    /// 
    /// Image service details: https://docs.microsoft.com/en-us/visualstudio/extensibility/image-service-and-catalog
    /// For help looking up what images are available from VS, and their corresponding KnownMonikers,
    /// try out the KnownMonikersExplorer VS Extension, see:
    /// https://marketplace.visualstudio.com/items?itemName=MadsKristensen.KnownMonikersExplorer
    /// </summary>
    public enum VsKnownImages
    {
        Add,
        AddLink,
        AddUser,
        Cancel,
        Cloud,
        Copy,
        DeleteListItem, 
        Edit,
        FeedbackFrown,
        FeedbackSmile,
        Loading,
        Refresh,
        Remove,
        RemoveLink,
        Search,
        StatusError,
        StatusInformation,
        StatusOK,
        StatusWarning,
    }
}
