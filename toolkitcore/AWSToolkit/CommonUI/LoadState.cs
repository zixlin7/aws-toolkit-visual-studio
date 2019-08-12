using System;

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
        }

        public bool DisplayWaitState
        {
            get;
        }

        public bool FullRefresh
        {
            get;
        }
    }
}
