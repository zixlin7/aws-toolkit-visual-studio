using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;


namespace Amazon.AWSToolkit.Navigator.Node
{
    public class ErrorViewModel : AbstractViewModel
    {
        Exception _exception;

        public ErrorViewModel(IViewModel parent, Exception exception)
            : base(new ErrorMetaNode(), parent, exception.Message)
        {
            this._exception = exception;
        }

        public ErrorViewModel(Exception exception)
            : this(null, exception)
        {
        }

        protected override string IconName
        {
            get
            {
                return "Amazon.AWSToolkit.Resources.warning.png";
            }
        }


        public override string Name
        {
            get
            {
                if (this.IsSignUpError)
                {
                    return "Please sign up for this service";
                }

                return base.Name;
            }
        }

        public override string ToolTip
        {
            get
            {
                return this._exception.Message;
            }
        }

        public bool IsSignUpError
        {
            get
            {
                if (this._exception.Message.ToLower().Contains("subscription"))
                {
                    return true;
                }

                return false;
            }
        }

        protected override bool IsLink
        {
            get
            {
                return this.IsSignUpError;
            }
        }

        
    }
}
