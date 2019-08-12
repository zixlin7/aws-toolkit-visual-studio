using System;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.View;
using Amazon.AWSToolkit.CloudFormation.Util;

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

        public ViewDeploymentLogModel Model => this._model;

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
