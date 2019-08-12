using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;

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
            get => this._destination;
            set
            {
                this._destination = value;
                base.NotifyPropertyChanged("Destination");
            }
        }

        Target _selectedTarget;
        public Target SelectedTarget
        {
            get => this._selectedTarget;
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
