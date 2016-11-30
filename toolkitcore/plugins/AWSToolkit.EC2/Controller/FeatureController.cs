using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public abstract class FeatureController<M> : BaseContextCommand where M : new()
    {
        IAmazonEC2 _ec2Client;
        M _model;
        string _endpoint;
        FeatureViewModel _featureViewModel;

        public override ActionResults Execute(IViewModel model)
        {
            this._featureViewModel = model as FeatureViewModel;
            if (this._featureViewModel == null)
                return new ActionResults().WithSuccess(false);

            this._endpoint = ((IEndPointSupport)this._featureViewModel.Parent).CurrentEndPoint.Url;
            this._ec2Client = this._featureViewModel.EC2Client;
            this._model = new M();

            DisplayView();

            return new ActionResults().WithSuccess(true);
        }

        protected abstract void DisplayView();

        public IAmazonEC2 EC2Client
        {
            get { return this._ec2Client; }
        }

        protected FeatureViewModel FeatureViewModel
        {
            get { return this._featureViewModel; }
        }

        public M Model
        {
            get { return this._model; }
        }

        public string EndPoint
        {
            get
            {
                return this._endpoint;
            }
        }

        public AccountViewModel Account
        {
            get { return this._featureViewModel.AccountViewModel; }
        }

        public string RegionDisplayName
        {
            get { return this._featureViewModel.RegionDisplayName; }
        }

        public string RegionSystemName
        {
            get { return this.FeatureViewModel.RegionSystemName; }
        }
    }
}
