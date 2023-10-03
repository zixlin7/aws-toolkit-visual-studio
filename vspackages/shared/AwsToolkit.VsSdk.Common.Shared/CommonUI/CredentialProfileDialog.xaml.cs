using Amazon.AWSToolkit.CommonUI.Dialogs;

namespace Amazon.AwsToolkit.VsSdk.Common.CommonUI
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
    }
}
