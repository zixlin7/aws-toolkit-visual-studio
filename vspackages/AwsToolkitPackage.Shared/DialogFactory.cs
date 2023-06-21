using Amazon.AwsToolkit.SourceControl;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;

using AwsToolkit.VsSdk.Common.CommonUI;
using AwsToolkit.VsSdk.Common.CommonUI.Ecr;
using AwsToolkit.VsSdk.Common.CommonUI.Models;

using Microsoft.VisualStudio.Threading;
using Microsoft.Win32;

namespace Amazon.AWSToolkit.VisualStudio
{
    public class DialogFactory : IDialogFactory
    {
        private readonly ToolkitContext _toolkitContext;
        private readonly JoinableTaskFactory _joinableTaskFactory;

        public DialogFactory(ToolkitContext toolkitContext, JoinableTaskFactory joinableTaskFactory)
        {
            _toolkitContext = toolkitContext;
            _joinableTaskFactory = joinableTaskFactory;
        }

        public OpenFileDialog CreateOpenFileDialog()
        {
            return new OpenFileDialog();
        }

        public IFolderBrowserDialog CreateFolderBrowserDialog()
        {
            return new VsFolderBrowserDialog(_joinableTaskFactory);
        }

        public ISelectionDialog CreateSelectionDialog()
        {
            return new SelectionDialog();
        }

        public IIamRoleSelectionDialog CreateIamRoleSelectionDialog()
        {
            return new IamRoleSelectionDialog(_toolkitContext, _joinableTaskFactory);
        }

        public IVpcSelectionDialog CreateVpcSelectionDialog()
        {
            return new VpcSelectionDialog(_toolkitContext, _joinableTaskFactory);
        }

        public IInstanceTypeSelectionDialog CreateInstanceTypeSelectionDialog()
        {
            return new InstanceTypeSelectionDialog(_toolkitContext, _joinableTaskFactory);
        }

        public IKeyValueEditorDialog CreateKeyValueEditorDialog()
        {
            return new KeyValueEditorDialog(_toolkitContext);
        }
        
        public ICredentialSelectionDialog CreateCredentialsSelectionDialog()
        {
            return new CredentialSelectionDialog(_toolkitContext, _joinableTaskFactory);
        }

        public IEcrRepositorySelectionDialog CreateEcrRepositorySelectionDialog()
        {
            return new EcrRepositorySelectionDialog(_toolkitContext, _joinableTaskFactory);
        }

        public ICloneCodeCommitRepositoryDialog CreateCloneCodeCommitRepositoryDialog()
        {
            return new CloneCodeCommitRepositoryDialog(_toolkitContext, _joinableTaskFactory);
        }

        public ICloneCodeCatalystRepositoryDialog CreateCloneCodeCatalystRepositoryDialog()
        {
            return new CloneCodeCatalystRepositoryDialog(_toolkitContext, _joinableTaskFactory, new GitService(_toolkitContext));
        }

        public ISsoLoginDialog CreateSsoLoginDialog()
        {
            return new SsoLoginDialog(_toolkitContext);
        }

        public ICredentialProfileDialog CreateCredentialProfileDialog()
        {
            return new CredentialProfileDialog()
            {
                DataContext = new CredentialProfileDialogViewModel(_toolkitContext)
            };
        }
    }
}
