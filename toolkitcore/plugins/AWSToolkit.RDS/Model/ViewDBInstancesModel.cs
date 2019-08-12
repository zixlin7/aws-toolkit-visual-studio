﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amazon.RDS.Model;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class ViewDBInstancesModel : BaseModel
    {
        ObservableCollection<DBInstanceWrapper> _instances = new ObservableCollection<DBInstanceWrapper>();
        public ObservableCollection<DBInstanceWrapper> DBInstances => this._instances;

        IList<DBInstanceWrapper> _selectedDBInstances = new List<DBInstanceWrapper>();
        public IList<DBInstanceWrapper> SelectedDBInstances => _selectedDBInstances;

        EC2ColumnDefinition[] _instancePropertytColumnDefinitions;
        public EC2ColumnDefinition[] PropertyColumnDefinitions
        {
            get
            {
                if (this._instancePropertytColumnDefinitions == null)
                {
                    this._instancePropertytColumnDefinitions = EC2ColumnDefinition.GetPropertyColumnDefinitions(typeof(DBInstanceWrapper));
                }

                return this._instancePropertytColumnDefinitions;
            }
        }

        ObservableCollection<Event> _selectedEvents = new ObservableCollection<Event>();
        public ObservableCollection<Event> SelectedEvents
        {
            get => _selectedEvents;
            set
            {
                _selectedEvents = value;
                base.NotifyPropertyChanged("SelectedEvents");
            }
        }
    }
}
