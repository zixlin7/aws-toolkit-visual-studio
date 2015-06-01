using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;


namespace Amazon.AWSToolkit.EC2.Model
{
    public class AssociateDHCPOptionSetModel : BaseModel
    {
        DHCPOptionsWrapper _newDHCPOptions;

        public AssociateDHCPOptionSetModel(VPCWrapper vpc)
        {
            this.VPC = vpc;
        }

        public VPCWrapper VPC
        {
            get;
            private set;
        }

        bool _isNew;
        public bool IsNew
        {
            get { return this._isNew; }
            set
            {
                this._isNew = value;
                base.NotifyPropertyChanged("IsNew");
            }
        }

        public bool IsExisting
        {
            get { return !this._isNew; }
            set
            {
                this._isNew = !value;
                base.NotifyPropertyChanged("IsExisting");
            }
        }

        public DHCPOptionsWrapper NewDHCPOptions
        {
            get 
            {
                if (this._newDHCPOptions == null)
                {
                    this._newDHCPOptions = new DHCPOptionsWrapper(new DhcpOptions()
                    {
                        DhcpOptionsId = "no-id",
                        DhcpConfigurations = new List<DhcpConfiguration>(),
                        Tags = new List<Tag>()
                    });
                }
                return this._newDHCPOptions; 
            }
        }

        DHCPOptionsWrapper _selectedDHCPOptions;
        public DHCPOptionsWrapper SelectedDHCPOptions
        {
            get
            {
                return this._selectedDHCPOptions;
            }
            set
            {
                this._selectedDHCPOptions = value;
                base.NotifyPropertyChanged("SelectedDHCPOptions");
            }
        }

        ObservableCollection<DHCPOptionsWrapper> _availableDHCPOptions;
        public ObservableCollection<DHCPOptionsWrapper> AvailableDHCPOptions
        {
            get { return this._availableDHCPOptions; }
            set { this._availableDHCPOptions = value; }
        }
    }
}
