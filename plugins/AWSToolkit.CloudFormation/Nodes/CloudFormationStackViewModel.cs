using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit;

namespace Amazon.AWSToolkit.CloudFormation.Nodes
{
    public class CloudFormationStackViewModel : AbstractViewModel, ICloudFormationStackViewModel
    {
        CloudFormationStackViewMetaNode _metaNode;
        CloudFormationRootViewModel _serviceModel;
        IAmazonCloudFormation _cloudFormationClient;
        string _stackName;

        public CloudFormationStackViewModel(CloudFormationStackViewMetaNode metaNode, CloudFormationRootViewModel viewModel, string stackName)
            : base(metaNode, viewModel, stackName)
        {
            this._metaNode = metaNode;
            this._serviceModel = viewModel;
            this._stackName = stackName;
            this._cloudFormationClient = this._serviceModel.CloudFormationClient;
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.CloudFormation.Resources.EmbeddedImages.stack-node.png";
            }
        }

        public IAmazonCloudFormation CloudFormationClient
        {
            get
            {
                return this._cloudFormationClient;
            }
        }

        public string StackName
        {
            get { return this._stackName; }
        }

        public CloudFormationRootViewModel CloudFormationRootViewModel
        {
            get
            {
                return this._serviceModel;
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);

            Func<string> getTemplate = () => this._cloudFormationClient.GetTemplate(new GetTemplateRequest{StackName = this.StackName}).TemplateBody;
            dndDataObjects.SetData(ToolkitGlobalContants.CloudFormationStackTemplateFetcherDnDFormat, getTemplate);
        }
    }
}
