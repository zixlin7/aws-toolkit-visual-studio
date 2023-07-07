using System;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles;
using Amazon.AWSToolkit.Context;

namespace AwsToolkit.VsSdk.Common.CommonUI.Models
{
    public class CredentialProfileDialogViewModel : BaseModel, IDisposable
    {
        public CredentialProfileFormViewModel CredentialProfileFormViewModel { get; private set; }

        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            set => SetProperty(ref _dialogResult, value);
        }

        public string Heading { get; } = "Setup a Profile to Authenticate";

        public CredentialProfileDialogViewModel(ToolkitContext toolkitContext)
        {
            CredentialProfileFormViewModel = new CredentialProfileFormViewModel(toolkitContext);
            CredentialProfileFormViewModel.CredentialProfileSaved += CredentialProfileFormViewModel_CredentialProfileSaved;
        }

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                if (CredentialProfileFormViewModel != null)
                {
                    CredentialProfileFormViewModel.CredentialProfileSaved -= CredentialProfileFormViewModel_CredentialProfileSaved;
                    CredentialProfileFormViewModel.Dispose();
                    CredentialProfileFormViewModel = null;
                }
            }
        }

        private void CredentialProfileFormViewModel_CredentialProfileSaved(object sender, CredentialProfileFormViewModel.CredentialProfileSavedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
