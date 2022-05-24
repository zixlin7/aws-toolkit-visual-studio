using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.Dialogs;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    // This dialog is currently not reusable.  Get a new SelectionDialog from DialogFactory each time.
    public partial class SelectionDialog : DialogWindow, ISelectionDialog
    {
        private IEnumerable _items;
        public IEnumerable Items
        {
            get => _items;
            set
            {
                VerifyBeforeShowCalled();
                _items = value;
            }
        }

        private IEnumerable _selectedItems;
        public IEnumerable SelectedItems
        {
            get => _selectedItems;
            set
            {
                VerifyBeforeShowCalled();
                _selectedItems = value;
            }
        }

        private SelectionMode _selectionMode = SelectionMode.Single;
        public SelectionMode SelectionMode
        {
            get => _selectionMode;
            set
            {
                VerifyBeforeShowCalled();
                _selectionMode = value;
            }
        }

        private bool _isSelectionRequired;
        public bool IsSelectionRequired
        {
            get => _isSelectionRequired;
            set
            {
                VerifyBeforeShowCalled();
                _isSelectionRequired = value;
            }
        }

        private string _displayMemberPath;
        public string DisplayMemberPath
        {
            get => _displayMemberPath;
            set
            {
                VerifyBeforeShowCalled();
                _displayMemberPath = value;
            }
        }

        private readonly ICommand _okCommand;
        public ICommand OkCommand
        {
            get => _okCommand;
        }

        public SelectionDialog()
        {
            _okCommand = new RelayCommand(OkCommandCanExecute, OkCommandExecute);

            InitializeComponent();
        }

        private bool OkCommandCanExecute(object arg)
        {
            return !_isSelectionRequired || _listBox.SelectedIndex != -1;
        }

        private void OkCommandExecute(object arg)
        {
            _selectedItems = _listBox.SelectedItems;
            DialogResult = true;
        }

        private bool _hasBeenShown;

        // Here to maintain some state of not mutating properties after dialog has been shown as it will have no effect.  
        private void VerifyBeforeShowCalled([CallerMemberName] string callerName = "UNKNOWN")
        {
            if (_hasBeenShown)
            {
                throw new InvalidOperationException($"Invalid to set {callerName} after {nameof(SelectionDialog)} has been shown.");
            }
        }

        public new bool Show()
        {
            _hasBeenShown = true;

            _listBox.SelectionMode = SelectionMode;
            _listBox.DisplayMemberPath = DisplayMemberPath;
            
            if (Items != null)
            {
                foreach (object item in Items)
                {
                    _listBox.Items.Add(item);
                }
            }

            if (SelectedItems != null)
            {
                if (SelectionMode == SelectionMode.Single)
                {
                    IEnumerator enumerator = SelectedItems.GetEnumerator();
                    if (enumerator?.MoveNext() == true)
                    {
                        _listBox.SelectedItem = enumerator.Current;
                    }
                }
                else
                {
                    foreach (object selectedItem in SelectedItems)
                    {
                        _listBox.SelectedItems.Add(selectedItem);
                    }
                }
            }

            return ShowModal() ?? false;
        }
    }
}
