using System;

namespace Amazon.AWSToolkit.Publish.Util
{
    public class DisposingAction : IDisposable
    {
        private readonly Action _onDispose;

        public DisposingAction(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            _onDispose?.Invoke();
        }
    }
}
