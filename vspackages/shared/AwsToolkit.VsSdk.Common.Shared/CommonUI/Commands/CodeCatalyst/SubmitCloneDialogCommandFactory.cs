using System.Windows;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;

using CommonUI.Models;

namespace AwsToolkit.VsSdk.Common.CommonUI.Commands.CodeCatalyst
{
    internal static class SubmitCloneDialogCommandFactory
    {
        public static ICommand Create(CloneCodeCatalystRepositoryViewModel viewModel, Window window)
        {
            return new RelayCommand(_ => CanSubmit(viewModel), _ => SubmitDialog(window));
        }

        private static bool CanSubmit(CloneCodeCatalystRepositoryViewModel viewModel)
        {
            return viewModel.CanSubmit();
        }

        private static void SubmitDialog(Window window)
        {
            window.DialogResult = true;
        }
    }
}
