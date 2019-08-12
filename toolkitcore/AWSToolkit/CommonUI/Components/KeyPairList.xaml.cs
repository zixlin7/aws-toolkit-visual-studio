using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Interaction logic for KeyPairList.xaml
    /// </summary>
    public partial class KeyPairList
    {
        class KeyPairComboItem
        {
            public enum KeyPairType
            {
                EmptyPair,
                CreateNewPair, // transient selection of special 'create new' entry
                NewPair,       // user-entered name, pair doesn't actually exist yet
                ExistingPair
            }

            public KeyPairComboItem(string name, KeyPairType pairType)
                : base()
            {
                KeyPairName = name;
                PairType = pairType;
            }

            public string KeyPairName { get; }
            public KeyPairType PairType { get; }

            public System.Windows.Media.ImageSource IsStoredLocallyIcon
            {
                get
                {
                    if (IsStoredLocally)
                    {
                        var icon = IconHelper.GetIcon("private-found.png");
                        return icon.Source;
                    }

                    return null;
                }
            }

            public bool IsStoredLocally { get; set; }

        }

        private KeyPairComboItem _createNewKeyPairComboItem = new KeyPairComboItem("<Create new key pair...>", KeyPairComboItem.KeyPairType.CreateNewPair);
        private KeyPairComboItem _noKeyPairComboItem = new KeyPairComboItem("<No key pair>", KeyPairComboItem.KeyPairType.EmptyPair);

        // used to record 'last selected' so if user cancels new keyname entry we can reselect
        KeyPairComboItem _previousSelection = null;

        public EventHandler<EventArgs> OnKeyPairSelectionChanged;

        public KeyPairList()
        {
            InitializeComponent();
        }

        /// <summary>
        /// If true, the list will contain a 'blank' entry allowing the user to elect to
        /// not specify a keypair.
        /// </summary>
        public bool AllowNoKeyPairSelection { get; set; }

        /// <summary>
        /// If set true, the list will contain a '<Create new key pair...>' entry that allows the
        /// user to enter the name of a new key pair to be created. False removes the ability to
        /// create a new keypair.
        /// </summary>
        public bool AllowCreateKeyPairSelection { get; set; }

        /// <summary>
        /// Returns the name of the currently selected key pair
        /// </summary>
        public string SelectedKeyPairName 
        {
            get
            {
                KeyPairComboItem kpi = _keyPairSelector.SelectedItem as KeyPairComboItem;
                if (kpi != null && kpi != _noKeyPairComboItem)
                    return kpi.KeyPairName;

                return string.Empty;
            }
        }

        /// <summary>
        /// Return true if the key list currently has a valid non-blank key pair selected
        /// </summary>
        public bool HasSelectedKeyPairName
        {
            get
            {
                KeyPairComboItem kp = _keyPairSelector.SelectedItem as KeyPairComboItem;
                return kp != _noKeyPairComboItem;
            }
        }

        public bool IsExistingKeyPairSelected
        {
            get
            {
                KeyPairComboItem kp = _keyPairSelector.SelectedItem as KeyPairComboItem;
                if (kp != null)
                    return kp.PairType == KeyPairComboItem.KeyPairType.ExistingPair;
                else
                    return false;
            }
        }

        public void SetExistingKeyPairs(ICollection<string> existingKeyPairs, ICollection<string> keyPairsStoredInToolkit, string autoSelectPair)
        {
            ObservableCollection<KeyPairComboItem> keypairs = new ObservableCollection<KeyPairComboItem>();

            if (AllowNoKeyPairSelection)
                keypairs.Add(_noKeyPairComboItem);

            if (AllowCreateKeyPairSelection)
                keypairs.Add(_createNewKeyPairComboItem);

            KeyPairComboItem autoSelectItem = null;
            if (existingKeyPairs != null)
            {
                foreach (string keyName in existingKeyPairs)
                {
                    var kp = new KeyPairComboItem(keyName, KeyPairComboItem.KeyPairType.ExistingPair);
                    kp.IsStoredLocally = keyPairsStoredInToolkit.Contains(keyName);
                    keypairs.Add(kp);
                    if (autoSelectItem == null && !string.IsNullOrEmpty(autoSelectPair))
                    {
                        if (string.Compare(autoSelectPair, keyName, true) == 0)
                            autoSelectItem = kp;
                    }
                }
            }

            _keyPairSelector.ItemsSource = keypairs;
            if (autoSelectItem == null)
            {
                // select first genuine pair if possible
                if (existingKeyPairs != null && existingKeyPairs.Count<string>() > 0)
                {
                    int firstGenuineIndex = 2;
                    if (!AllowNoKeyPairSelection)
                        firstGenuineIndex--;
                    if (!AllowCreateKeyPairSelection)
                        firstGenuineIndex--;
                    autoSelectItem = keypairs[firstGenuineIndex];
                }
                else
                    autoSelectItem = AllowNoKeyPairSelection || AllowCreateKeyPairSelection ? keypairs[0] : null;
            }

            _keyPairSelector.SelectedItem = autoSelectItem;
        }

        private void _keyPairSelector_DropDownClosed(object sender, EventArgs e)
        {
            KeyPairComboItem kpi = _keyPairSelector.SelectedItem as KeyPairComboItem;
            if (kpi == _createNewKeyPairComboItem)
                _keyNameEntryPopup.IsOpen = true;
            else
                _previousSelection = null;
        }

        private void _popupKeyNameOK_Click(object sender, RoutedEventArgs e)
        {
            CommitPopup();
        }

        private void CommitPopup()
        {
            _keyNameEntryPopup.IsOpen = false;

            string newKeyName = _keyName.Text;
            ObservableCollection<KeyPairComboItem> keys = _keyPairSelector.ItemsSource as ObservableCollection<KeyPairComboItem>;
            // try and trap the case where the user enters the name of an existing key
            KeyPairComboItem existingKey = null;
            foreach (KeyPairComboItem key in keys)
            {
                if (string.Compare(key.KeyPairName, newKeyName, true) == 0)
                {
                    existingKey = key;
                    break;
                }
            }
            if (existingKey == null)
            {
                KeyPairComboItem kp = new KeyPairComboItem(newKeyName, KeyPairComboItem.KeyPairType.NewPair);
                keys.Add(kp);
                _keyPairSelector.SelectedItem = kp;
            }
            else
            {
                // should we warn of existence at this point?
                _keyPairSelector.SelectedItem = existingKey;
            }

            _keyName.Text = string.Empty;
            _previousSelection = null;
        }

        private void CancelPopup()
        {
            _keyNameEntryPopup.IsOpen = false;
            _keyName.Text = string.Empty;
            if (_previousSelection != null)
            {
                _keyPairSelector.SelectedItem = _previousSelection;
                _previousSelection = null;
            }
        }

        private void _popupKeyNameCancel_Click(object sender, RoutedEventArgs e)
        {
            CancelPopup();
        }

        private void _keyPairSelector_DropDownOpened(object sender, EventArgs e)
        {
            _previousSelection = _keyPairSelector.SelectedItem as KeyPairComboItem;
        }

        private void _keyPairSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OnKeyPairSelectionChanged != null)
                OnKeyPairSelectionChanged(this, EventArgs.Empty);
        }

        private void _keyName_TextChanged(object sender, TextChangedEventArgs e)
        {
            _popupKeyNameOK.IsEnabled = _keyName.Text.Length > 0;
        }

        private void _keyNameEntryPopup_Opened(object sender, EventArgs e)
        {
            _popupKeyNameOK.IsEnabled = false;
            _keyName.Focus();
        }

        private void _keyName_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    e.Handled = true;
                    CancelPopup();
                    break;

                case Key.Return:
                    e.Handled = true;
                    CommitPopup();
                    break;

                default:
                    break;
            }
        }
    }
}
