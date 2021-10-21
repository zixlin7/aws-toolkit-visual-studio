using System;

using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.CloudFormation
{
    public class CloudFormationViewer : ICloudFormationViewer
    {
        private readonly IAWSToolkitShellProvider _shellProvider;
        private readonly NavigatorControl _navigatorControl;

        public CloudFormationViewer(IAWSToolkitShellProvider shellProvider, NavigatorControl navigatorControl)
        {
            _shellProvider = shellProvider;
            _navigatorControl = navigatorControl;
        }

        public void View(string stackName, ICredentialIdentifier identifier, ToolkitRegion region)
        {
            if (string.IsNullOrWhiteSpace(stackName))
            {
                throw new ArgumentException("Stack Name cannot be null or empty.");
            }

            if (_navigatorControl.SelectedAccount?.Identifier?.Id != identifier?.Id ||
                _navigatorControl.SelectedRegion != region)
            {
                _shellProvider.OutputToHostConsole(
                    $"Unable to find {stackName}. You may find it in the AWS Explorer with the following credential settings: {identifier?.DisplayName}, {region?.DisplayName}");
                return;
            }
            
            _shellProvider.ExecuteOnUIThread(() =>
            {
                var cloudFormationRootNode = _navigatorControl.SelectedAccount
                    .FindSingleChild<CloudFormationRootViewModel>(false);

                if (cloudFormationRootNode == null)
                {
                    throw new NodeNotFoundException("Unable to load CloudFormation stacks");
                }

                var stackNode = GetStackViewModel(stackName, cloudFormationRootNode);

                if (stackNode == null)
                {
                    throw new NodeNotFoundException($"CloudFormation stack: {stackName} cannot be found");
                }

                _navigatorControl.SelectedNode = stackNode;
                stackNode.ExecuteDefaultAction();
            });
        }

        private CloudFormationStackViewModel GetStackViewModel(string stackName, CloudFormationRootViewModel cloudFormationRootNode)
        {
            var stackNode = FindSingleChild(stackName, cloudFormationRootNode);
            if (stackNode == null)
            {
                cloudFormationRootNode.Refresh(false);
                stackNode = FindSingleChild(stackName, cloudFormationRootNode);
            }

            return stackNode;
        }

        private CloudFormationStackViewModel FindSingleChild(string stackName, CloudFormationRootViewModel cloudFormationRootNode)
        {
            return cloudFormationRootNode.FindSingleChild<CloudFormationStackViewModel>(false,
                x => string.Equals(x.StackName, stackName));
        }
    }
}
