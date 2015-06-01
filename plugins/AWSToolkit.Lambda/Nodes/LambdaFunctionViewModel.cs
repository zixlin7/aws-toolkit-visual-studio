using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

using Amazon.Lambda;
using Amazon.Lambda.Model;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Lambda.Nodes
{
    public class LambdaFunctionViewModel : AbstractViewModel, ILambdaFunctionViewModel
    {
        LambdaFunctionViewMetaNode _metaNode;
        LambdaRootViewModel _LambdaRootViewModel;
        string _functionName;
        string _functionArn;

        public LambdaFunctionViewModel(LambdaFunctionViewMetaNode metaNode, LambdaRootViewModel LambdaRootViewModel, FunctionConfiguration function)
            : base(metaNode, LambdaRootViewModel, function.FunctionName)
        {
            this._metaNode = metaNode;
            this._LambdaRootViewModel = LambdaRootViewModel;
            this._functionName = function.FunctionName;
            this._functionArn = function.FunctionArn;
        }

        public LambdaRootViewModel LambdaRootViewModel
        {
            get { return this._LambdaRootViewModel; }
        }

        public IAmazonLambda LambdaClient
        {
            get { return this._LambdaRootViewModel.LambdaClient; }
        }

        public string FunctionName
        {
            get { return this._functionName; }
        }

        public string FunctionArn
        {
            get { return this._functionArn; }
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.service-root.png";
            }
        }

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", this._functionArn);
        }
    }
}
