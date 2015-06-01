﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit;

using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ConnectToInstanceController
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ConnectToInstanceController));

        public ActionResults Execute(EC2InstancesViewModel instanceViewModel, IList<string> instanceIds)
        {
            try
            {
                if (instanceIds == null)
                    return new ActionResults().WithSuccess(false);

                if (instanceIds.Count == 0)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("There are no instances to connect to.");
                    return new ActionResults().WithSuccess(false);
                }

                string selectedInstanceId = instanceIds[0];
                if (instanceIds.Count > 1)
                {
                    var chooseModel = new ChooseInstanceToConnectModel() { SelectedInstance = selectedInstanceId, InstanceIds = instanceIds };
                    var control = new ChooseInstanceToConnectControl();
                    control.DataContext = chooseModel;

                    if (!ToolkitFactory.Instance.ShellProvider.ShowModal(control))
                    {
                        return new ActionResults().WithSuccess(false);
                    }

                    selectedInstanceId = chooseModel.SelectedInstance;
                }

                instanceViewModel.ConnectToInstance(selectedInstanceId);

                return new ActionResults().WithSuccess(true);
            }
            catch(Exception e)
            {
                LOGGER.Error("Error connecting to instance", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error connecting to instance: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }
    }
}
