using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public class OpenFileSelectionCommandFactory
    {
        public static ICommand Create(
            FilePathConfigurationDetail filePathDetail,
            IDialogFactory dialogFactory)
        {
            return new RelayCommand(
                _ => true,
                _ =>
                {
                    var dlg = dialogFactory.CreateOpenFileDialog();
                    dlg.CheckFileExists = filePathDetail.CheckFileExists;
                    dlg.Filter = filePathDetail.Filter;
                    dlg.Title = filePathDetail.Title;

                    if (dlg.ShowDialog() ?? false)
                    {
                        filePathDetail.Value = dlg.FileName;
                    }
                });
        }
    }
}
