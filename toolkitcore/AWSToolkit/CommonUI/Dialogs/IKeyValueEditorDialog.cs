using System.Collections.Generic;

using Amazon.AWSToolkit.Models;

namespace Amazon.AWSToolkit.CommonUI.Dialogs
{
    public interface IKeyValueEditorDialog
    {
        string Title { get; set; }
        IEnumerable<KeyValue> KeyValues { get; set; }

        bool Show();
    }
}
