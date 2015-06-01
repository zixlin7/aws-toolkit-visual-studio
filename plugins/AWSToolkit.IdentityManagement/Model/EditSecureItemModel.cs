using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.IdentityManagement.Model
{
    public class EditSecureItemModel : BaseModel
    {
        public EditSecureItemModel()
        {
            this.IAMPolicyModels.CollectionChanged += new NotifyCollectionChangedEventHandler(onPolicyCollectionChange);
        }

        bool _isDirty;
        public bool IsDirty
        {
            get { return this._isDirty; }
            set
            {
                this._isDirty = value;
                base.NotifyPropertyChanged("IsDirty");
            }
        }

        List<string> _newPolicies = new List<string>();
        public List<string> NewPolicies
        {
            get { return this._newPolicies; }
        }

        List<string> _deletedPolicies = new List<string>();
        public List<string> DeletedPolicies
        {
            get { return this._deletedPolicies; }
        }

        ObservableCollection<IAMPolicyModel> _iamPolicyModels = new ObservableCollection<IAMPolicyModel>();
        public ObservableCollection<IAMPolicyModel> IAMPolicyModels
        {
            get { return this._iamPolicyModels; }
            set
            {
                this._iamPolicyModels = value;
                base.NotifyPropertyChanged("IAMPolicyModels");
            }
        }

        void onPolicyCollectionChange(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.NotifyPropertyChanged("IAMPolicyModels");
        }

        public IAMPolicyModel AddPolicy(string name)
        {
            IAMPolicyModel policyModel = new IAMPolicyModel();
            policyModel.Name = name;
            this.IAMPolicyModels.Add(policyModel);
            this.NewPolicies.Add(name);

            return policyModel;
        }

        public void RemovePolicy(IAMPolicyModel policyModel)
        {
            this.DeletedPolicies.Add(policyModel.Name);
            this.IAMPolicyModels.Remove(policyModel);
        }

        public virtual void CommitChanges()
        {
            this.DeletedPolicies.Clear();
            this.NewPolicies.Clear();
            foreach (var policyModel in this.IAMPolicyModels)
            {
                policyModel.CommitChanges();
            }

            this.IsDirty = false;
        }

        public string OrignalName
        {
            get;
            set;
        }

        string _newName;
        public string NewName
        {
            get { return this._newName == null ? string.Empty : this._newName; }
            set
            {
                this._newName = value;
                base.NotifyPropertyChanged("NewName");
            }
        }
    }
}
