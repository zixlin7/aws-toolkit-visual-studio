using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models.Configuration;

using static Amazon.AWSToolkit.Publish.Models.Configuration.ListConfigurationDetail;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public static class EditListCommandFactory
    {
        public static ICommand Create(
            ListConfigurationDetail listDetail,
            string dialogTitle,
            IDialogFactory dialogFactory)
        {
            return new RelayCommand(
                _ => listDetail != null,
                _ =>
                {
                    var dlg = dialogFactory.CreateSelectionDialog();
                    dlg.Title = dialogTitle;
                    dlg.Items = listDetail.Items;
                    dlg.SelectedItems = listDetail.SelectedItems;
                    dlg.SelectionMode = SelectionMode.Multiple;
                    dlg.DisplayMemberPath = nameof(ListConfigurationDetail.ListItem.DisplayName);

                    if (dlg.Show())
                    {
                        listDetail.SelectedItems.Clear();
                        listDetail.SelectedItems.AddAll(dlg.SelectedItems.Cast<ListItem>());
                        listDetail.UpdateListValues();
                    }
                });
        }
    }
}
