using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.Navigator.Node
{
    public class LoadingViewModel : AbstractViewModel
    {
        public LoadingViewModel()
            : base(new LoadingMetaNode(), null, "Loading")
        {
        }

        protected override string IconName
        {
            get
            {
                return null;
            }
        }
    }
}
