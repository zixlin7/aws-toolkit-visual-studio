using Amazon.AWSToolkit.SimpleDB.Model;
using Amazon.AWSToolkit.SimpleDB.View;

namespace Amazon.AWSToolkit.SimpleDB.Controller
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
