using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;

using log4net;


namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class InstanceDataRootViewModel : AbstractViewModel
    {
        ILog LOGGER = LogManager.GetLogger(typeof(InstanceDataRootViewModel));

        ObservableCollection<IViewModel> _instances;

        public InstanceDataRootViewModel(IMetaNode metaNode, IViewModel parent, string name)
            : base(metaNode, parent, name)
        {
        }

        public override ObservableCollection<IViewModel> Children
        {
            get
            {
                if (this._instances == null)
                {
                    if (this._instances == null)
                        this._instances = new ObservableCollection<IViewModel>();

                    Refresh(true);
                }

                return this._instances;
            }
        }

        protected override void InitializeChildrensCollection()
        {
            if (this._instances == null)
                this._instances = new ObservableCollection<IViewModel>();
            else
                this._instances.Clear();
        }

        public override void Refresh(bool async)
        {
            if (async)
            {
                if (this._instances == null)
                    this._instances = new ObservableCollection<IViewModel>();
                else
                    this._instances.Clear();
                this._instances.Add(new LoadingViewModel());
                ThreadPool.QueueUserWorkItem(new WaitCallback(this.loadChildren));
            }
            else
            {
                this.loadChildren(null);
            }
        }

        private void loadChildren(object state)
        {
            try
            {
                LoadChildren();
            }
            catch (Exception e)
            {
                LOGGER.Warn("Error loading children for root " + this.Name, e);
                base.AddErrorChild(e);
            }
        }

        protected void AddChild(IViewModel child)
        {
            int index = 0;
            for (index = 0; index < this.Children.Count; index++)
            {
                if (child.Name.CompareTo(this.Children[index].Name) <= 0)
                {
                    break;
                }
            }

            if (index == this.Children.Count)
            {
                this.Children.Add(child);
            }
            else
            {
                this.Children.Insert(index, child);
            }

            ToolkitFactory.Instance.Navigator.SelectedNode = child;
        }

        protected void RemoveChild(string bucketName)
        {
            int index = 0;
            for (index = 0; index < this.Children.Count; index++)
            {
                if (bucketName.Equals(this.Children[index].Name))
                {
                    break;
                }
            }

            if (index < this.Children.Count)
            {
                this.Children.RemoveAt(index);
            }
        }

        protected abstract void LoadChildren();
    }
}
