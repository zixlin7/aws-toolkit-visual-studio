﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewSecurityGroupsModel : BaseModel
    {

        ObservableCollection<SecurityGroupWrapper> _SecurityGroups = new ObservableCollection<SecurityGroupWrapper>();
        public ObservableCollection<SecurityGroupWrapper> SecurityGroups => this._SecurityGroups;

        IList<SecurityGroupWrapper> _selectedGroups = new List<SecurityGroupWrapper>();
        public IList<SecurityGroupWrapper> SelectedGroups => this._selectedGroups;

        EC2ColumnDefinition[] _instancePropertytColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._instancePropertytColumnDefinitions == null)
                {
                    this._instancePropertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(SecurityGroupWrapper));
                }

                return this._instancePropertytColumnDefinitions;
            }
        }


        public string[] ListAvailableTags => EC2ColumnDefinition.GetListAvailableTags(this.SecurityGroups);
    }
}
