using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewInternetGatewayModel : BaseModel
    {
        ObservableCollection<InternetGatewayWrapper> _gateways = new ObservableCollection<InternetGatewayWrapper>();
        public ObservableCollection<InternetGatewayWrapper> Gateways => this._gateways;

        EC2ColumnDefinition[] _propertytColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._propertytColumnDefinitions == null)
                {
                    this._propertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(InternetGatewayWrapper));
                }

                return this._propertytColumnDefinitions;
            }
        }

        public string[] ListAvailableTags => EC2ColumnDefinition.GetListAvailableTags(this.Gateways);
    }
}
