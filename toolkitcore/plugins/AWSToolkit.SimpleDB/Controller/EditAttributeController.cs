using Amazon.AWSToolkit.SimpleDB.Model;
using Amazon.AWSToolkit.SimpleDB.View;

namespace Amazon.AWSToolkit.SimpleDB.Controller
{
    public class EditAttributeController
    {
        EditAttributeModel _model;

        public EditAttributeController()
        {
            this._model = new EditAttributeModel();
        }

        public EditAttributeController(EditAttributeModel model)
        {
            this._model = model;
        }


        public EditAttributeModel Model => this._model;

        public bool Execute()
        {
            EditAttributeControl control = new EditAttributeControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }
    }
}
