using System;

namespace Amazon.AWSToolkit
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
