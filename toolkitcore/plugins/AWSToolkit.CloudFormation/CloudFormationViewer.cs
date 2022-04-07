using System;

using Amazon.AWSToolkit.CloudFormation.Controllers;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudFormation
{
    public class CloudFormationViewer : ICloudFormationViewer
    {
        private readonly ToolkitContext _toolkitContext;

        public CloudFormationViewer(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public void View(string stackName, AwsConnectionSettings connectionSettings)
        {
            if (string.IsNullOrWhiteSpace(stackName))
            {
                throw new ArgumentException("Stack Name cannot be null or empty.");
            }

            var model = new ViewStackModel(connectionSettings.Region.Id, stackName);
            var controller = new ViewStackController(model, _toolkitContext, connectionSettings);

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                controller.Execute();
            });
        }
    }
}
