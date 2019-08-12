using System;
using System.Collections.Generic;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using log4net;

namespace Amazon.AWSToolkit.EC2.Nodes
{
    public class EC2InstancesViewModel : FeatureViewModel, IEC2InstancesViewModel
    {
        readonly static ILog LOGGER = LogManager.GetLogger(typeof(EC2InstancesViewModel));

        public EC2InstancesViewModel(EC2InstancesViewMetaNode metaNode, EC2RootViewModel viewModel)
            : base(metaNode, viewModel, "Instances")
        {
        }

        protected override string IconName => "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.instance.png";

        public override string ToolTip => "Manage EC2 instances and launch new EC2 instances";

        public void ConnectToInstance(IList<string> instanceIds)
        {
            try
            {
                var controller = new ConnectToInstanceController();
                controller.Execute(this, instanceIds);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error connecting to instance", e);
            }
        }

        public void ConnectToInstance(string instanceId)
        {
            try
            {
                var instance = getRunningInstance(instanceId);
                if (instance.IsWindowsPlatform)
                {
                    var controller = new OpenRemoteDesktopController();
                    controller.Execute(this, instance);
                }
                else
                {
                    var controller = new OpenSSHSessionController();
                    controller.Execute(this, instance);
                }                    
            }
            catch (Exception e)
            {
                LOGGER.Error(string.Format("Error connecting to instance {0}", instanceId), e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error Connecting", string.Format("Error connecting to instance {0}: {1}", instanceId, e.Message));
            }
        }


        RunningInstanceWrapper getRunningInstance(string instanceId)
        {
            var request = new DescribeInstancesRequest() { InstanceIds = new List<string>() { instanceId } };
            var response = this.EC2Client.DescribeInstances(request);

            if (response.Reservations.Count != 1 && response.Reservations[0].Instances.Count != 1)
                return null;

            var reservation = response.Reservations[0];
            var wrapper = new RunningInstanceWrapper(reservation, reservation.Instances[0]);
            return wrapper;
        }

    }
}
