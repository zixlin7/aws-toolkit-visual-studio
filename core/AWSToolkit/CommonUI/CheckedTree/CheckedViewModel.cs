using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CommonUI.CheckedTree
{
    /// <summary>
    /// This is based off of http://www.codeproject.com/KB/WPF/TreeViewWithCheckBoxes.aspx
    /// </summary>
    public class CheckedViewModel<T> : INotifyPropertyChanged
    {
        bool? _isChecked = false;
        CheckedViewModel<T> _parent;
        List<CheckedViewModel<T>> _children = new List<CheckedViewModel<T>>();
        T _data;

        public CheckedViewModel(T data)
        {
            this._data = data;
        }

        public CheckedViewModel(CheckedViewModel<T> parent, T data)
            : this(data)
        {
            this._parent = parent;
        }

        public CheckedViewModel<T> Parent
        {
            get { return this._parent; }
        }

        public List<CheckedViewModel<T>> Children 
        {
            get { return this._children; }
        }

        public bool IsInitiallyExpanded
        {
            get;
            set;
        }

        public bool IsInitiallySelected 
        { 
            get; 
            private set; 
        }

        public string Name 
        {
            get { return this._data.ToString(); }
        }

        public T Data
        {
            get
            {
                return this._data;
            }
        }

        /// <summary>
        /// Gets/sets the state of the associated UI toggle (ex. CheckBox).
        /// The return value is calculated based on the check state of all
        /// child FooViewModels.  Setting this property to true or false
        /// will set all children to the same check state, and setting it 
        /// to any value will cause the parent to verify its check state.
        /// </summary>
        public bool? IsChecked
        {
            get { return _isChecked; }
            set { this.SetIsChecked(value, true, true); }
        }

        public void VerifyChildrenState()
        {
            if (this._parent != null && this._parent._isChecked.GetValueOrDefault())
                this._isChecked = true;

            foreach (var item in this.Children)
            {
                item.VerifyChildrenState();
            }
        }

        void SetIsChecked(bool? value, bool updateChildren, bool updateParent)
        {
            if (value == _isChecked)
                return;

            _isChecked = value;

            if (updateChildren && _isChecked.HasValue)
            {
                foreach (var c in this.Children)
                {
                    c.SetIsChecked(_isChecked, true, false);
                }
            }

            if (updateParent && _parent != null)
                _parent.VerifyCheckState();

            this.OnPropertyChanged("IsChecked");
        }

        void VerifyCheckState()
        {
            bool? state = null;
            for (int i = 0; i < this.Children.Count; ++i)
            {
                bool? current = this.Children[i].IsChecked;
                if (i == 0)
                {
                    state = current;
                }
                else if (state != current)
                {
                    state = null;
                    break;
                }
            }
            this.SetIsChecked(state, false, true);
        }

        #region INotifyPropertyChanged Members

        void OnPropertyChanged(string prop)
        {
            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
