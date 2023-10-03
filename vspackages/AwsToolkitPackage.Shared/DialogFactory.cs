using Amazon.AwsToolkit.SourceControl;
using Amazon.AwsToolkit.VsSdk.Common.CommonUI;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Behaviors;
using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Telemetry.Model;

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

        #region CreateOpenFileDialog
        private class OpenFileDialogProxy : IOpenFileDialog
        {
            private readonly OpenFileDialog _dialog;

            public OpenFileDialogProxy()
            {
                _dialog = new OpenFileDialog();
            }

            public bool CheckFileExists
            {
                get => _dialog.CheckFileExists;
                set => _dialog.CheckFileExists = value;
            }

            public bool CheckPathExists
            {
                get => _dialog.CheckPathExists;
                set => _dialog.CheckPathExists = value;
            }

            public string DefaultExt
            {
                get => _dialog.DefaultExt;
                set => _dialog.DefaultExt = value;
            }

            public string FileName
            {
                get => _dialog.FileName;
                set => _dialog.FileName = value;
            }

            public string Filter
            {
                get => _dialog.Filter;
                set => _dialog.Filter = value;
            }

            public string Title
            {
                get => _dialog.Title;
                set => _dialog.Title = value;
            }

            public bool? ShowDialog()
            {
                return _dialog.ShowDialog();
            }
        }

        public IOpenFileDialog CreateOpenFileDialog()
        {
            return new OpenFileDialogProxy();
        }
        #endregion

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

        public ICredentialProfileDialog CreateCredentialProfileDialog(BaseMetricSource saveMetricSource)
        {
            var dialog = new CredentialProfileDialog();
            Mvvm.SetViewModel(dialog, new CredentialProfileDialogViewModel(_toolkitContext, saveMetricSource));
            return dialog;
        }
    }
}
