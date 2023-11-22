using System;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions
{
    public class PauseStateChangedEventArgs : EventArgs
    {
        public bool IsPaused { get; set; }
    }
}
