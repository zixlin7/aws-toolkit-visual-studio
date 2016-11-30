using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CommonUI
{
    public class LoadState
    {
        public const string LOADING_TEXT = "Loading, please wait...";

        public LoadState(bool displayWaitState, bool fullRefresh)
        {
            this.LastTextFilterChangeToken = Guid.Empty;
            this.DisplayWaitState = displayWaitState;
            this.FullRefresh = fullRefresh;
        }

        public LoadState(Guid lastTextFilterChangeToken, bool displayWaitState, bool fullRefresh)
        {
            this.LastTextFilterChangeToken = lastTextFilterChangeToken;
            this.DisplayWaitState = displayWaitState;
            this.FullRefresh = fullRefresh;
        }

        public Guid LastTextFilterChangeToken
        {
            get;
            private set;
        }

        public bool DisplayWaitState
        {
            get;
            private set;
        }

        public bool FullRefresh
        {
            get;
            private set;
        }
    }
}
