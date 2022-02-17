using Amazon.AWSToolkit.CommonUI.Dialogs;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Toolkit dialog producer
    /// </summary>
    public interface IDialogFactory
    {
        IIamRoleSelectionDialog CreateIamRoleSelectionDialog();
        IVpcSelectionDialog CreateVpcSelectionDialog();
        IInstanceTypeSelectionDialog CreateInstanceTypeSelectionDialog();
        IKeyValueEditorDialog CreateKeyValueEditorDialog();
    }
}
