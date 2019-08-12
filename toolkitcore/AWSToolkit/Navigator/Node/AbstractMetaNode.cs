using System.Collections.Generic;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public abstract class AbstractMetaNode : IMetaNode
    {
        public virtual bool SupportsEndPoint => false;

        public virtual bool SupportsRefresh => this.SupportsEndPoint;

        public virtual string EndPointSystemName => null;

        public virtual IList<ActionHandlerWrapper> Actions => new List<ActionHandlerWrapper>();

        protected IList<ActionHandlerWrapper> BuildActionHandlerList(params ActionHandlerWrapper[] handles)
        {
            IList<ActionHandlerWrapper> map = new List<ActionHandlerWrapper>();
            foreach (ActionHandlerWrapper handle in handles)
            {
                map.Add(handle);
            }

            return map;
        }

        IList<IMetaNode> _children;
        public IList<IMetaNode> Children
        {
            get 
            { 
                if(this._children == null)
                    this._children = new List<IMetaNode>();

                return this._children; 
            }
        }

        public T FindChild<T>() where T : IMetaNode
        {
            foreach (IMetaNode meta in this.Children)
            {
                if (meta is T)
                    return (T)meta;
            }

            return default(T);
        }
    }
}
