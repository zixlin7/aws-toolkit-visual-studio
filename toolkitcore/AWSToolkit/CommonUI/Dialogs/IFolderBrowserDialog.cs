namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface IFolderBrowserDialog
    {
        /// <summary>
        /// Gets the path to the folder selected in the dialog.
        /// Set this prior to calling <see cref="ShowModal"/> in order to specify the initial folder shown in the dialog.
        /// </summary>
        string FolderPath { get; set; }

        /// <summary>
        /// The dialog's title
        /// </summary>
        string Title { get; set; }

        bool ShowModal();
    }
}
