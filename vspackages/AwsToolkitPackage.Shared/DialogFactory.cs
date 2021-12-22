using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Context;

using AwsToolkit.VsSdk.Common.CommonUI;

using Microsoft.VisualStudio.Threading;

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
    }
}
