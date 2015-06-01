using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.EC2.Model;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class AddRouteModel : BaseModel
    {

        public AddRouteModel(RouteTableWrapper routeTable)
        {
            this.RouteTable = routeTable;
        }

        public RouteTableWrapper RouteTable
        {
            get;
            set;
        }

        private string _destination;
        public string Destination
        {
            get { return this._destination; }
            set
            {
                this._destination = value;
                base.NotifyPropertyChanged("Destination");
            }
        }

        Target _selectedTarget;
        public Target SelectedTarget
        {
            get{return this._selectedTarget;}
            set
            {
                this._selectedTarget = value;
                base.NotifyPropertyChanged("SelectedTarget");
            }
        }

        public IList<Target> AvailableTargets
        {
            get;
            set;
        }


        public class Target
        {
            public enum TargetType { Instance, NetworkInferface, InternetGateway };

            public Target(string displayName, string keyField, TargetType type)
            {
                this.DisplayName = displayName;
                this.KeyField = keyField;
                this.Type = type;
            }

            public string DisplayName
            {
                get;
                set;
            }

            public string KeyField
            {
                get;
                set;
            }

            public TargetType Type
            {
                get;
                set;
            }
        }
    }
}
