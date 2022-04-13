using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.CommonUI.Dialogs;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Toolkit dialog producer
    /// </summary>
    public interface IDialogFactory
    {
        ISelectionDialog CreateSelectionDialog();
        IIamRoleSelectionDialog CreateIamRoleSelectionDialog();
        IVpcSelectionDialog CreateVpcSelectionDialog();
        IInstanceTypeSelectionDialog CreateInstanceTypeSelectionDialog();
        IKeyValueEditorDialog CreateKeyValueEditorDialog();
        ICredentialSelectionDialog CreateCredentialsSelectionDialog();
        IEcrRepositorySelectionDialog CreateEcrRepositorySelectionDialog();
    }
}
