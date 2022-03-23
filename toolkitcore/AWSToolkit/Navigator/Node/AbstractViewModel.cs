using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;
using System.Windows;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class AbstractViewModel : IViewModel, ITreeNodeProperties
    {
        IMetaNode _metaNode;
        string _name;
        IViewModel _parent;
        private readonly ToolkitContext _toolkitContext;

        public AbstractViewModel(IMetaNode metaNode, IViewModel parent, string name)
            : this(metaNode, parent, name, ToolkitFactory.Instance.ToolkitContext)
        {
        }

        public AbstractViewModel(IMetaNode metaNode, IViewModel parent, string name, ToolkitContext toolkitContext)
        {
            this._metaNode = metaNode;
            this._parent = parent;
            this._name = name;
            _toolkitContext = toolkitContext;
        }

        public virtual string Name => this._name;

        public Stream Icon
        {
            get
            {
                if (this.IconName == null)
                    return null;

                Stream stream = this.GetType().Assembly.GetManifestResourceStream(this.IconName);
                if(stream == null)
                    stream = typeof(ToolkitFactory).Assembly.GetManifestResourceStream(this.IconName);
                return stream;
            }
        }

        public virtual string ToolTip => this.Name;

        protected virtual string IconName => null;

        public IViewModel Parent => this._parent;

        public virtual ObservableCollection<IViewModel> Children => AccountViewModel.NO_CHILDREN;

        protected virtual void InitializeChildrensCollection()
        {
            this.Children.Clear();
        }

        public IMetaNode MetaNode => this._metaNode;

        public ToolkitContext ToolkitContext => _toolkitContext;

        public virtual IList<ActionHandlerWrapper> GetVisibleActions()
        {
            return this.MetaNode.Actions;
        }

        public void ExecuteDefaultAction()
        {
            foreach (var action in this.MetaNode.Actions)
            {
                if (action != null && action.IsDefault)
                {
                    NodeClickExecutor executor = new NodeClickExecutor(this, action);
                    executor.OnClick(this, new RoutedEventArgs());
                    break;
                }
            }
        }

        public virtual T FindSingleChild<T>(bool recursive) where T : IViewModel
        {
            foreach (IViewModel model in this.Children)
            {
                if (model is T)
                    return (T)model;
                else if (recursive)
                {
                    T value = model.FindSingleChild<T>(recursive);
                    if (value != null)
                        return value;
                }
            }

            return default(T);
        }

        public virtual T FindAncestor<T>() where T : class, IViewModel
        {
            var parent = this.Parent;
            if (parent == null || parent is T)
                return parent as T;
            else
                return parent.FindAncestor<T>();
        }

        public virtual T FindSingleChild<T>(bool recursive, Predicate<T> func) where T : IViewModel
        {
            foreach (IViewModel model in this.Children)
            {
                if (model is T && func.Invoke((T)model))
                    return (T)model;
                else if (recursive)
                {
                    T value = model.FindSingleChild<T>(recursive, func);
                    if (value != null)
                        return value;
                }
            }

            return default(T);
        }

        protected void SetChildren(IList<IViewModel> items)
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread((Action)(() =>
            {
                this.InitializeChildrensCollection();

                foreach (var item in items.OrderBy(item => item.Name))
                {
                    this.Children.Add(item);
                }
            }));
        }

        protected void AddErrorChild(Exception e)
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread((Action)(() => 
            {
                this.Children.Clear();
                this.Children.Add(new ErrorViewModel(this, e));
            }));
        }

        public string AccountDisplayName
        {
            get
            {
                AccountViewModel vm = this.AccountViewModel;
                if (vm == null)
                    return string.Empty;
                return vm.Identifier.ProfileName;
            }

        }

        public AccountViewModel AccountViewModel
        {
            get
            {
                if (this is AccountViewModel)
                {
                    return this as AccountViewModel;
                }
                if (this.Parent is AbstractViewModel)
                {
                    return ((AbstractViewModel)this.Parent).AccountViewModel;
                }
                else
                {
                    return null;
                }
            }
        }

        protected virtual bool IsLink => false;

        public virtual void Refresh(bool async)
        {

        }

        public virtual void LoadDnDObjects(IDataObject dndDataObjects)
        {
        }

        public virtual bool FailedToLoadChildren 
        {
            get
            {
                bool isNotLoaded = this.Children.Count == 1 && this.Children[0] is ErrorViewModel;
                return isNotLoaded;
            }
        }

        #region INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        #region ITreeNodeProperties implementation

        bool _isSelected;
        bool ITreeNodeProperties.IsSelected
        {
            get => this._isSelected;
            set
            {
                if (this._isSelected == value)
                    return;

                this._isSelected = value;
                this.NotifyPropertyChanged("IsSelected");
            }
        }

        bool _isExpanded;
        bool ITreeNodeProperties.IsExpanded
        {
            get => this._isExpanded;
            set
            {
                if (this._isExpanded == value)
                    return;

                this._isExpanded = value;
                this.NotifyPropertyChanged("IsExpanded");
            }
        }

        string ITreeNodeProperties.TextDecoration
        {
            get 
            {
                if (this.IsLink)
                {
                    return "Underline";
                }

                return null; 
            }
        }

        #endregion
    }
}
