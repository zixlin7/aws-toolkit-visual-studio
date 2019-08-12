using Amazon.AWSToolkit.Lambda.Model;
using Amazon.KeyManagementService.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for KMSKeySelectorControl.xaml
    /// </summary>
    public partial class KMSKeySelectorControl : UserControl, INotifyPropertyChanged
    {
        public KMSKeySelectorControl()
        {
            DataContext = this;

            AvailableKMSKeys = new ObservableCollection<KeyAndAliasWrapper>();

            InitializeComponent();

            var kmsKeysView = (CollectionView)CollectionViewSource.GetDefaultView(_ctlKMSKey.ItemsSource);
            var kmsGroupDescription = new PropertyGroupDescription("GroupCategoryName");
            kmsKeysView.GroupDescriptions.Add(kmsGroupDescription);
        }

        public ObservableCollection<KeyAndAliasWrapper> AvailableKMSKeys { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void SetAvailableKMSKeys(IEnumerable<KeyListEntry> keys, 
                                       IEnumerable<AliasListEntry> aliases, 
                                       KeyAndAliasWrapper[] serviceDefaults,
                                       KeyListEntry initialSelection)
        {
            var aliasedKeys = BuildAliasedKeyList(keys, aliases);

            AvailableKMSKeys.Clear();

            KeyAndAliasWrapper instanceToSelect = null;

            if (serviceDefaults != null && serviceDefaults.Any())
            {
                foreach (var serviceDefault in serviceDefaults)
                {
                    AvailableKMSKeys.Add(serviceDefault);
                    if (instanceToSelect == null && serviceDefault.Key == initialSelection)
                        instanceToSelect = serviceDefault;
                }
            }

            foreach (var k in aliasedKeys)
            {
                AvailableKMSKeys.Add(k);
                if (instanceToSelect == null && k.Key == initialSelection)
                    instanceToSelect = k;
            }

            if (instanceToSelect != null)
                _ctlKMSKey.SelectedItem = instanceToSelect;
              
            _ctlKMSKey.IsEnabled = AvailableKMSKeys.Any();
        }

        private IEnumerable<KeyAndAliasWrapper> BuildAliasedKeyList(IEnumerable<KeyListEntry> keys, IEnumerable<AliasListEntry> aliases)
        {
            var aliasedKeys = new Dictionary<string, AliasListEntry>();
            foreach (var alias in aliases)
            {
                if (!string.IsNullOrEmpty(alias.TargetKeyId) && !aliasedKeys.ContainsKey(alias.TargetKeyId))
                    aliasedKeys.Add(alias.TargetKeyId, alias);
            }

            var list = new List<KeyAndAliasWrapper>();
            foreach (var key in keys)
            {
                if (aliasedKeys.ContainsKey(key.KeyId))
                    list.Add(new KeyAndAliasWrapper(key, aliasedKeys[key.KeyId]));
                else
                    list.Add(new KeyAndAliasWrapper(key));
            }

            return list;
        }

        public KeyListEntry SelectedKey
        {
             get
            {
                var aliasedKey = _ctlKMSKey.SelectedItem as KeyAndAliasWrapper;
                if (aliasedKey == null)
                    return null;

                return aliasedKey.Key;
            }
        }

        private void _ctlKMSKey_DropDownClosed(object sender, EventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs("kmskey"));                
        }
    }
}
