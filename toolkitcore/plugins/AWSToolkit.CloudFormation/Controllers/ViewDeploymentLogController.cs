using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.View;
using Amazon.AWSToolkit.CloudFormation.Util;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class ViewDeploymentLogController
    {
        ViewDeploymentLogModel _model;

        public void Execute(RunningInstanceWrapper instance)
        {
            this._model = new ViewDeploymentLogModel(instance);
            ToolkitFactory.Instance.ShellProvider.ShowModal(new ViewDeploymentLogControl(this), System.Windows.MessageBoxButton.OK);
        }

        public ViewDeploymentLogModel Model
        {
            get { return this._model; }
        }

        public void LoadModel()
        {
            try
            {
                string log = CloudFormationUtil.GetDeploymentLog(this._model.Instance);
                this._model.Log = log;
                this._model.ErrorMessage = "";
            }
            catch (Exception e)
            {
                this._model.ErrorMessage = e.Message;
            }
        }
    }
}
