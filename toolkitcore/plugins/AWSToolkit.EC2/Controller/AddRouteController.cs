using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AddRouteController
    {
        IAmazonEC2 _ec2Client;
        RouteTableWrapper _routeTable;
        ActionResults _result;
        AddRouteModel _model;

        public ActionResults Execute(IAmazonEC2 ec2Client, RouteTableWrapper routeTable)
        {
            this._ec2Client = ec2Client;
            this._routeTable = routeTable;
            this._model = new AddRouteModel(routeTable);

            List<AddRouteModel.Target> targets = new List<AddRouteModel.Target>();
            addPossibleInstances(targets);
            addPossibleNetworkInterfaces(targets);
            addPossibleInternetGateways(targets);
            targets.Sort((x, y) => { return x.DisplayName.CompareTo(y.DisplayName); });

            this._model.AvailableTargets = targets;

            AddRouteControl control = new AddRouteControl(this);

            if (!ToolkitFactory.Instance.ShellProvider.ShowModal(control))
                return new ActionResults().WithSuccess(false);

            if (this._result == null)
                return new ActionResults().WithSuccess(false);

            return this._result;
        }

        public AddRouteModel Model
        {
            get { return this._model; }
        }

        void addPossibleInstances(IList<AddRouteModel.Target> targets)
        {
            var request = new DescribeInstancesRequest() { Filters = new List<Filter>() 
                { 
                    new Filter() { Name = "vpc-id", Values = new List<string>(){this._model.RouteTable.VpcId }},
                    new Filter() { Name = "instance-state-name", Values = new List<string>(){"running" }} 
                } 
            };

            var response = this._ec2Client.DescribeInstances(request);
            foreach (var reservation in response.Reservations)
            {
                foreach (var instance in reservation.Instances)
                {
                    var wrapper = new RunningInstanceWrapper(reservation, instance);
                    var target = new AddRouteModel.Target("EC2 Instance: " + wrapper.DisplayName, wrapper.InstanceId, AddRouteModel.Target.TargetType.Instance);

                    if(this._routeTable.Routes.FirstOrDefault(x => x.NativeRoute.InstanceId == instance.InstanceId) == null)
                        targets.Add(target);
                }
            }
        }

        void addPossibleNetworkInterfaces(IList<AddRouteModel.Target> targets)
        {
            var request = new DescribeNetworkInterfacesRequest()
            {
                Filters = new List<Filter>() 
                { 
                    new Filter() { Name = "vpc-id", Values = new List<string>(){this._model.RouteTable.VpcId }}
                }
            };

            var response = this._ec2Client.DescribeNetworkInterfaces(request);
            foreach (var item in response.NetworkInterfaces)
            {
                var wrapper = new NetworkInterfaceWrapper(item);
                var target = new AddRouteModel.Target("Network Interface: " + wrapper.FormattedLabel, item.NetworkInterfaceId, AddRouteModel.Target.TargetType.NetworkInferface);

                if (this._routeTable.Routes.FirstOrDefault(x => x.NativeRoute.NetworkInterfaceId == item.NetworkInterfaceId) == null)
                    targets.Add(target);
            }
        }

        void addPossibleInternetGateways(IList<AddRouteModel.Target> targets)
        {
            var request = new DescribeInternetGatewaysRequest()
            {
                Filters = new List<Filter>() 
                { 
                    new Filter() { Name = "attachment.vpc-id", Values = new List<string>(){this._model.RouteTable.VpcId }}
                }
            };

            var response = this._ec2Client.DescribeInternetGateways(request);
            foreach (var item in response.InternetGateways)
            {
                var wrapper = new InternetGatewayWrapper(item);
                var target = new AddRouteModel.Target("Internet Gateway: " + wrapper.FormattedLabel, item.InternetGatewayId, AddRouteModel.Target.TargetType.InternetGateway);

                if (this._routeTable.Routes.FirstOrDefault(x => x.NativeRoute.GatewayId == item.InternetGatewayId) == null)
                    targets.Add(target);
            }
        }

        public void AddRoute()
        {
            var request = new CreateRouteRequest()
            {
                RouteTableId = this._routeTable.RouteTableId,
                DestinationCidrBlock = this.Model.Destination,                
            };


            switch (this.Model.SelectedTarget.Type)
            {
                case AddRouteModel.Target.TargetType.Instance:
                    request.InstanceId = this.Model.SelectedTarget.KeyField;
                    break;
                case AddRouteModel.Target.TargetType.NetworkInferface:
                    request.NetworkInterfaceId = this.Model.SelectedTarget.KeyField;
                    break;
                case AddRouteModel.Target.TargetType.InternetGateway:
                    request.GatewayId = this.Model.SelectedTarget.KeyField;
                    break;
                default:
                    throw new ApplicationException("Unknown target for route");
            }
            var response = this._ec2Client.CreateRoute(request);

            this._result = new ActionResults() { Success = true, FocalName = request.DestinationCidrBlock };
        }
    }
}
