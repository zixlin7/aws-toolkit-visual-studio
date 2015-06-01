using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.PolicyEditor.Model
{
    public class ActionModel
    {
        public ActionModel(string systemName, string displayName)
        {
            this.SystemName = systemName;
            this.DisplayName = displayName;
        }

        public string DisplayName
        {
            get;
            set;
        }

        public string SystemName
        {
            get;
            set;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }
    }
}
