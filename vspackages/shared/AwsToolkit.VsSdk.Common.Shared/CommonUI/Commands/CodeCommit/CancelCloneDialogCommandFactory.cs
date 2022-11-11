using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;

namespace AwsToolkit.VsSdk.Common.CommonUI.Commands.CodeCommit
{
    internal static class CancelCloneDialogCommandFactory
    {
        public static ICommand Create(Window window)
        {
            return new RelayCommand(_ => CancelDialog(window));
        }

        private static void CancelDialog(Window window)
        {
            window.DialogResult = false;
        }
    }
}
