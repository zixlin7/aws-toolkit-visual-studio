using Amazon.AWSToolkit.DynamoDB.Model;
using Amazon.AWSToolkit.DynamoDB.View;

namespace Amazon.AWSToolkit.DynamoDB.Controller
{
    public class AddAttributeController
    {
        AddAttributeModel _model = new AddAttributeModel();

        public AddAttributeController()
        {
        }


        public AddAttributeModel Model => this._model;

        public bool Execute()
        {
            AddAttributeControl control = new AddAttributeControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }
    }
}
