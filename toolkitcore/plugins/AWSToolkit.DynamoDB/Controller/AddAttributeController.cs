using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

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


        public AddAttributeModel Model
        {
            get
            {
                return this._model;
            }
        }

        public bool Execute()
        {
            AddAttributeControl control = new AddAttributeControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }
    }
}
