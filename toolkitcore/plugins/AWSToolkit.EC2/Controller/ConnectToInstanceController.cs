using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Navigator;

using log4net;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ConnectToInstanceController
    {
        private static ILog Logger = LogManager.GetLogger(typeof(ConnectToInstanceController));

        protected readonly ToolkitContext _toolkitContext;

        public ConnectToInstanceController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public ActionResults Execute(AwsConnectionSettings connectionSettings, IList<string> instanceIds)
        {
            try
            {
                if (instanceIds == null)
                    return new ActionResults().WithSuccess(false);

                if (instanceIds.Count == 0)
                {
                    _toolkitContext.ToolkitHost.ShowError("There are no instances to connect to.");
                    return new ActionResults().WithSuccess(false);
                }

                string selectedInstanceId = instanceIds[0];
                if (instanceIds.Count > 1)
                {
                    var chooseModel = new ChooseInstanceToConnectModel() { SelectedInstance = selectedInstanceId, InstanceIds = instanceIds };
                    var control = new ChooseInstanceToConnectControl();
                    control.DataContext = chooseModel;

                    if (!_toolkitContext.ToolkitHost.ShowModal(control))
                    {
                        return new ActionResults().WithSuccess(false);
                    }

                    selectedInstanceId = chooseModel.SelectedInstance;
                }
                
                IAWSEC2 awsEc2 =_toolkitContext.ToolkitHost.QueryAWSToolkitPluginService(typeof(IAWSEC2)) as IAWSEC2;
                awsEc2.ConnectToInstance(connectionSettings, selectedInstanceId);

                return new ActionResults().WithSuccess(true);
            }
            catch(Exception e)
            {
                Logger.Error("Error connecting to instance", e);
                _toolkitContext.ToolkitHost.ShowError("Error connecting to instance: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }
    }
}
