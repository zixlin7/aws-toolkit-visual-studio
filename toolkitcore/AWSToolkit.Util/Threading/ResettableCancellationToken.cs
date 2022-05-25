using System;
using System.Threading;

namespace Amazon.AWSToolkit.Threading
{
    public class ResettableCancellationToken : IDisposable
    {
        public bool IsCancellationRequested => _cancellationTokenSource.IsCancellationRequested;
        public CancellationToken Token => _cancellationTokenSource.Token;

        private readonly object _tokenSync = new object();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public void Cancel() => _cancellationTokenSource.Cancel();

        public CancellationToken Reset()
        {
            CancellationTokenSource oldSource;
            CancellationToken newToken;

            lock (_tokenSync)
            {
                oldSource = _cancellationTokenSource;
                _cancellationTokenSource = new CancellationTokenSource();
                newToken = _cancellationTokenSource.Token;
            }

            oldSource.Cancel();
            oldSource.Dispose();

            return newToken;
        }

        public void Dispose()
        {
            lock (_tokenSync)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
            }
        }
    }
}
