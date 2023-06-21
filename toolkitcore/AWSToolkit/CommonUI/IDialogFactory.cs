using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Credentials.Core;

using Microsoft.Win32;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Toolkit dialog producer
    /// </summary>
    public interface IDialogFactory
    {
        OpenFileDialog CreateOpenFileDialog();
        IFolderBrowserDialog CreateFolderBrowserDialog();
        ISelectionDialog CreateSelectionDialog();
        IIamRoleSelectionDialog CreateIamRoleSelectionDialog();
        IVpcSelectionDialog CreateVpcSelectionDialog();
        IInstanceTypeSelectionDialog CreateInstanceTypeSelectionDialog();
        IKeyValueEditorDialog CreateKeyValueEditorDialog();
        ICredentialSelectionDialog CreateCredentialsSelectionDialog();
        IEcrRepositorySelectionDialog CreateEcrRepositorySelectionDialog();
        ICloneCodeCommitRepositoryDialog CreateCloneCodeCommitRepositoryDialog();
        ICloneCodeCatalystRepositoryDialog CreateCloneCodeCatalystRepositoryDialog();
        ISsoLoginDialog CreateSsoLoginDialog();
        ICredentialProfileDialog CreateCredentialProfileDialog();
    }
}
