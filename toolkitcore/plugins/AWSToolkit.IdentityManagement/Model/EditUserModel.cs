using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class EditUserModel : EditSecureItemModel
    {
        public HashSet<string> OrignalAssignedGroups
        {
            get;
            set;
        }


        public override void CommitChanges()
        {
            base.CommitChanges();

            this.OrignalAssignedGroups = new HashSet<string>();
            foreach (var group in this.AssignedGroups)
                this.OrignalAssignedGroups.Add(group);
            
        }

        ObservableCollection<string> _availableGroups = new ObservableCollection<string>();
        public ObservableCollection<string> AvailableGroups
        {
            get => this._availableGroups;
            set
            {
                this._availableGroups = value;
                base.NotifyPropertyChanged("AvailableGroups");
            }
        }

        ObservableCollection<string> _assignedGroups = new ObservableCollection<string>();
        public ObservableCollection<string> AssignedGroups
        {
            get => this._assignedGroups;
            set
            {
                this._assignedGroups = value;
                base.NotifyPropertyChanged("AssignedGroups");
            }
        }

        ObservableCollection<AccessKeyModel> _accessKeys = new ObservableCollection<AccessKeyModel>();
        public ObservableCollection<AccessKeyModel> AccessKeys
        {
            get => this._accessKeys;
            set
            {
                this._accessKeys = value;
                base.NotifyPropertyChanged("AccessKeys");
            }
        }
    }
}
