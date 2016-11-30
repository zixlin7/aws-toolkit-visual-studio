using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.SNS.View;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit;

using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace Amazon.AWSToolkit.SNS.Controller
{
    public class EditDisplayNameController
    {
        IAmazonSimpleNotificationService _snsClient;
        EditDisplayNameModel _model;

        public EditDisplayNameController(IAmazonSimpleNotificationService snsClient, string topicARN)
            : this(snsClient, new EditDisplayNameModel())
        {
            this._model.TopicARN = topicARN;
        }

        public EditDisplayNameController(IAmazonSimpleNotificationService snsClient, EditDisplayNameModel model)
        {
            this._snsClient = snsClient;
            this._model = model;
        }

        public EditDisplayNameModel Model
        {
           get
           {
               return this._model;
           }
        }

        public bool Execute()
        {
            var control = new EditDisplayNameControl(this);
            return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }

        public void Persist()
        {
            this._snsClient.SetDisplayName(this._model.TopicARN, this._model.DisplayName);
        }

    }
}
