using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.RDS.Model;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class ViewDBSecurityGroupsModel : BaseModel
    {
        ObservableCollection<DBSecurityGroupWrapper> _securityGroups = new ObservableCollection<DBSecurityGroupWrapper>();
        public ObservableCollection<DBSecurityGroupWrapper> SecurityGroups
        {
            get { return this._securityGroups; }
        }

        EC2ColumnDefinition[] _instancePropertytColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._instancePropertytColumnDefinitions == null)
                {
                    this._instancePropertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(DBSecurityGroupWrapper));
                }

                return this._instancePropertytColumnDefinitions;
            }
        }
    }
}
