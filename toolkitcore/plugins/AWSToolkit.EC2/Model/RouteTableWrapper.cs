using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class RouteTableWrapper : PropertiesModel, ISubnetAssociationWrapper, ITagSupport
    {
        RouteTable _routeTable;
        IList<Subnet> _allSubnets;

        public RouteTableWrapper(RouteTable routeTable, IList<Subnet> subnets)
        {
            this._routeTable = routeTable;
            this._allSubnets = subnets;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Route Table";
            componentName = this._routeTable.RouteTableId;
        }

        [Browsable(false)]
        public RouteTable NativeRouteTable => this._routeTable;

        [Browsable(false)]
        public string DisplayName => this.NativeRouteTable.RouteTableId;

        [Browsable(false)]
        public string TypeName => "Route Table";

        [DisplayName("Route Table ID")]
        [AssociatedIcon(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.route-table.png")]
        public string RouteTableId => this._routeTable.RouteTableId;

        // TODO: Hiding this for now since I don't have a good method for refreshing it
        //[DisplayName("Associated With")]
        //public string AssociatedWith
        //{
        //    get 
        //    {
        //        int count = 0;
        //        foreach (var item in this._routeTable.Associations)
        //        {
        //            if (!string.IsNullOrEmpty(item.SubnetId))
        //                count++;
        //        }
        //        return string.Format("{0} Subnets", count); 
        //    }
        //}

        [Browsable(false)]
        public bool CanDelete => this.NativeRouteTable.Associations.Count == 0;

        [DisplayName("VPC")]
        public string VpcId => this._routeTable.VpcId;

        [DisplayName("Main")]
        public string IsMain
        {
            get
            {
                return this._routeTable.Associations.FirstOrDefault(x => x.Main) != null ? "Yes" : "No";
            }
        }

        [Browsable(false)]
        public bool HasMainAssociation
        {
            get
            {
                return this._routeTable.Associations.FirstOrDefault(x => x.Main) != null;
            }
        }


        ObservableCollection<RouteWrapper> _routes;
        [Browsable(false)]
        public ObservableCollection<RouteWrapper> Routes
        {
            get
            {
                if (this._routes == null)
                {
                    this._routes = new ObservableCollection<RouteWrapper>();
                    this._routeTable.Routes.ForEach(x => this._routes.Add(new RouteWrapper(x)));
                }

                return this._routes;
            }
        }

        [Browsable(false)]
        public bool CanDisassociate => true;


        ObservableCollection<RouteTableAssociationWrapper> _associations;
        [Browsable(false)]
        public ObservableCollection<RouteTableAssociationWrapper> Associations
        {
            get
            {
                if (this._associations == null)
                {
                    this._associations = new ObservableCollection<RouteTableAssociationWrapper>();
                    foreach (var association in this._routeTable.Associations)
                    {
                        if (string.IsNullOrEmpty(association.SubnetId))
                            continue;

                        var subnet = this._allSubnets.FirstOrDefault(s => s.SubnetId == association.SubnetId);
                        this._associations.Add(new RouteTableAssociationWrapper(association, subnet));
                    }
                }

                return this._associations;
            }
        }

        public Tag FindTag(string name)
        {
            if (this.NativeRouteTable.Tags == null)
                return null;

            return this.NativeRouteTable.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this.NativeRouteTable.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags => this.NativeRouteTable.Tags;
    }
}
