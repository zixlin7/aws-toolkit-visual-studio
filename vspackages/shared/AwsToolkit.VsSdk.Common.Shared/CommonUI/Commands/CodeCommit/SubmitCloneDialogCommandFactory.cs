using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;

using CommonUI.Models;

namespace AwsToolkit.VsSdk.Common.CommonUI.Commands.CodeCommit
{
    internal static class SubmitCloneDialogCommandFactory
    {
        public static ICommand Create(CloneCodeCommitRepositoryViewModel viewModel, Window window)
        {
            return new RelayCommand(_ => CanSubmit(viewModel), _ => SubmitDialog(window));
        }

        private static bool CanSubmit(CloneCodeCommitRepositoryViewModel viewModel)
        {
            return viewModel.CanSubmit();
        }

        private static void SubmitDialog(Window window)
        {
            window.DialogResult = true;
        }
    }
}
