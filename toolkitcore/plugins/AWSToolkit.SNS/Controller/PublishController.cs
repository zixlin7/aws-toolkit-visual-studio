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
    public class PublishController
    {
        IAmazonSimpleNotificationService _snsClient;
        PublishModel _model;

        public PublishController(IAmazonSimpleNotificationService snsClient, string topicARN)
        {
            this._snsClient = snsClient;
            this._model = new PublishModel(topicARN);

        }

        public PublishModel Model
       {
           get
           {
               return this._model;
           }
       }

       public bool Execute()
       {
           var control = new PublishControl(this);
           return ToolkitFactory.Instance.ShellProvider.ShowModal(control);
       }

       public void Persist()
       {
           this._snsClient.Publish(new PublishRequest()
           {
               TopicArn = this._model.TopicARN,
               Subject = this._model.Subject,
               Message = this._model.Message
           });
       }
    }
}
