using System;
using System.Reflection;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Navigator
{
    public class ActionHandlerWrapper
    {
        public delegate ActionResults ActionHandler(IViewModel focus);
        public delegate void ActionResponseHandler(IViewModel focus, ActionResults results);

        // allows actions to be probed prior to addition to a menu to have them disabled 
        //and/or hidden
        [Flags]
        public enum ActionVisibility
        {
            enabled     = 0,
            disabled    = 1,
            hidden      = 2
        }
        public delegate ActionVisibility DynamicVisibilityHandler(IViewModel focus);

        public ActionHandlerWrapper() { }

        public ActionHandlerWrapper(string name, 
                                    ActionHandler actionHandler, 
                                    ActionResponseHandler responseHandler, 
                                    bool isDefault, 
                                    Assembly iconAssembly, 
                                    string iconFile)
        {
            this.Name = name;
            this.Handler = actionHandler;
            this.ResponseHandler = responseHandler;
            this.IsDefault = isDefault;

            this.IconAssembly = iconAssembly;
            this.IconFile = iconFile;
        }

        public ActionHandlerWrapper(string name, ActionHandler actionHandler, ActionResponseHandler responseHandler)
        {
            this.Name = name;
            this.Handler = actionHandler;
            this.ResponseHandler = responseHandler;
        }

        public bool IsDefault
        {
            get;
            set;
        }

        public Image Icon => IconHelper.GetIcon(IconAssembly, IconFile);

        public Assembly IconAssembly
        {
            get;
            set;
        }

        public string IconFile
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public ActionHandler Handler
        {
            get;
            set;
        }

        public ActionResponseHandler ResponseHandler
        {
            get;
            set;
        }

        public DynamicVisibilityHandler VisibilityHandler
        {
            get;
            set;
        }
    }
}
