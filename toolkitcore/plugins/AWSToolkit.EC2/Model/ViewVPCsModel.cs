using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewVPCsModel : BaseModel
    {
        ObservableCollection<VPCWrapper> _vpcs = new ObservableCollection<VPCWrapper>();
        public ObservableCollection<VPCWrapper> VPCs => this._vpcs;

        EC2ColumnDefinition[] _vpcPropertytColumnDefinitions;
        public EC2ColumnDefinition[] VPCPropertyColumnDefinitions
        {
            get
            {
                if (this._vpcPropertytColumnDefinitions == null)
                {
                    this._vpcPropertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(VPCWrapper));
                }

                return this._vpcPropertytColumnDefinitions;
            }
        }

        public string[] ListVPCAvailableTags => EC2ColumnDefinition.GetListAvailableTags(this.VPCs);
    }
}
