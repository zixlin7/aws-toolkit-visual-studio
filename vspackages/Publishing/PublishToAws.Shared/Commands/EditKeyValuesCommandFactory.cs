using System.Collections.ObjectModel;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public static class EditKeyValuesCommandFactory
    {
        public static ICommand Create(
            KeyValueConfigurationDetail keyValueDetail,
            string dialogTitle,
            IDialogFactory dialogFactory)
        {
            return new RelayCommand(
                _ => keyValueDetail != null,
                _ =>
                {
                    var dlg = dialogFactory.CreateKeyValueEditorDialog();
                    dlg.Title = dialogTitle;
                    dlg.KeyValues = keyValueDetail.KeyValues.Collection;
                    if (dlg.Show())
                    {
                        keyValueDetail.SetKeyValues(dlg.KeyValues);
                    }
                });
        }
    }
}
