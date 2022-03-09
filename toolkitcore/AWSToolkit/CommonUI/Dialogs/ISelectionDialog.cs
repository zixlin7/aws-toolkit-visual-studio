using System.Collections;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface ISelectionDialog
    {
        string Title { get; set; }

        IEnumerable Items { get; set; }

        IEnumerable SelectedItems { get; set; }

        SelectionMode SelectionMode { get; set; }

        bool IsSelectionRequired { get; set; }

        bool Show();
    }
}
