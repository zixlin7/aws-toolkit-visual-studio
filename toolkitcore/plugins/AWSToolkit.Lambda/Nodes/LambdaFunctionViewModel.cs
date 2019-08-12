using System.Windows;
using Amazon.Lambda;
using Amazon.Lambda.Model;
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

        public LambdaRootViewModel LambdaRootViewModel => this._LambdaRootViewModel;

        public IAmazonLambda LambdaClient => this._LambdaRootViewModel.LambdaClient;

        public string FunctionName => this._functionName;

        public string FunctionArn => this._functionArn;

        protected override string IconName => "Amazon.AWSToolkit.Lambda.Resources.EmbeddedImages.service-root.png";

        public override void LoadDnDObjects(IDataObject dndDataObjects)
        {
            dndDataObjects.SetData(DataFormats.Text, this.Name);
            dndDataObjects.SetData("ARN", this._functionArn);
        }
    }
}
