using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.CloudFormation;
using Amazon.AWSToolkit.CloudFormation.Nodes;

using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.EC2;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages
{
    internal static class DeploymentWizardHelper
    {
        public static IAmazonCloudFormation GetGenericCloudFormationClient(AccountViewModel accountViewModel)
        {
            CloudFormationRootViewModel rootViewModel = accountViewModel.FindSingleChild<CloudFormationRootViewModel>(false);
            if (rootViewModel != null)
                return rootViewModel.CloudFormationClient;
            else
                return new AmazonCloudFormationClient(accountViewModel.Credentials, Amazon.RegionEndpoint.USEast1);
        }
    }
}
