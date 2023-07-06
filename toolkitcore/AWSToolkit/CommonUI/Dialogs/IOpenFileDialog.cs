namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    /// <summary>
    /// Interface to decouple current Microsoft.Win32.OpenFileDialog from DialogFactory usage.
    /// </summary>
    /// <remarks>
    /// Minimum set of members has been provided to support current needs.  Members of
    /// Microsoft.Win32.OpenFileDialog can be added to this interface in the future as needed
    /// without disruption to existing consumers.
    /// </remarks>
    public interface IOpenFileDialog
    {
        bool CheckFileExists { get; set; }

        bool CheckPathExists { get; set; }

        string DefaultExt { get; set; }

        string FileName { get; set; }

        string Filter { get; set; }

        string Title { get; set; }

        bool? ShowDialog();
    }
}
