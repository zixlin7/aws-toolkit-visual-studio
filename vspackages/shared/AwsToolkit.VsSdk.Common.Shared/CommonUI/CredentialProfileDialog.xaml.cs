using System;

using Amazon.AWSToolkit.CommonUI.Dialogs;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public partial class CredentialProfileDialog : ThemedDialogWindow, ICredentialProfileDialog
    {
        public CredentialProfileDialog()
        {
            InitializeComponent();
        }

        public new bool Show()
        {
            return ShowModal() == true;
        }

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                (DataContext as IDisposable)?.Dispose();
                DataContext = null;
            }
        }
    }
}
