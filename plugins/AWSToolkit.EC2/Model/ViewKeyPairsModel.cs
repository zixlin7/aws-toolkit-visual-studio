using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class ViewKeyPairsModel : BaseModel
    {

        ObservableCollection<KeyPairWrapper> _keyPairs = new ObservableCollection<KeyPairWrapper>();
        public ObservableCollection<KeyPairWrapper> KeyPairs
        {
            get { return this._keyPairs; }
        }

        IList<KeyPairWrapper> _selectedKeys = new List<KeyPairWrapper>();
        public IList<KeyPairWrapper> SelectedKeys
        {
            get { return this._selectedKeys; }
        }


        string _selectedKeyNames;
        public string SelectedKeyNames
        {
            get { return this._selectedKeyNames; }
            set
            {
                this._selectedKeyNames = value;
                base.NotifyPropertyChanged("SelectedKeyNames");
            }
        }
    }
}
