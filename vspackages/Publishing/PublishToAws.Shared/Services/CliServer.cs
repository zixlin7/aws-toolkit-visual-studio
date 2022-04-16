using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

using Amazon.AWSToolkit.Tasks;
using Amazon.Runtime;

using AWS.Deploy.ServerMode.Client;

using log4net;

using Timer = System.Timers.Timer;

namespace Amazon.AWSToolkit.Publish.Services
{
    /// <summary>
    /// VS Service responsible for managing the deploy tool's CLI Server
    /// </summary>
    public class CliServer : ICliServer, SCliServer, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CliServer));
        private static readonly SemaphoreSlim StartupSemaphore = new SemaphoreSlim(1, 1);

        public const int HealthCheckIntervalMs = 5000;

        public event EventHandler Disconnect;

        private bool _isConnected = false;
        private IServerModeSession _server;

        private readonly Timer _healthCheckTimer;

        /// <summary>
        /// Main constructor used by VS Service
        /// </summary>
        public CliServer(IServerModeSession serverModeSession)
        {
            _server = serverModeSession;

            _healthCheckTimer = new Timer()
            {
                Interval = HealthCheckIntervalMs,
                AutoReset = false,
                Enabled = false,
            };
            _healthCheckTimer.Elapsed += OnCheckHealth;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                await StartupSemaphore.WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (_isConnected || _server == null)
                {
                    return;
                }

                await _server.Start(cancellationToken).ConfigureAwait(false);

                _healthCheckTimer.Start();

                SetConnectedState(true);
            }
            catch (Exception)
            {
                SetConnectedState(false);
                throw;
            }
            finally
            {
                StartupSemaphore.Release();
            }
        }

        public void Stop()
        {
            try
            {
                // TODO : How to stop the server?
            }
            catch (Exception e)
            {
                Logger.Error("Error while stopping the CLI Server", e);
            }
            finally
            {
                SetConnectedState(false);
            }
        }

        public IRestAPIClient GetRestClient(Func<Task<AWSCredentials>> credentialsGenerator)
        {
            ThrowIfServerIsNull();

            if (_server.TryGetRestAPIClient(credentialsGenerator, out var client))
            {
                return client;
            }

            throw new Exception("No deploy tooling rest client available. Check that your AWS Credentials are valid, and try again.");
        }

        public IDeploymentCommunicationClient GetDeploymentClient()
        {
            ThrowIfServerIsNull();

            if (_server.TryGetDeploymentCommunicationClient(out var client))
            {
                return client;
            }

            throw new Exception("No deploy tooling communication client available.");
        }

        private void OnCheckHealth(object sender, ElapsedEventArgs args)
        {
            OnCheckHealthAsync().LogExceptionAndForget();
        }

        private async Task OnCheckHealthAsync()
        {
            try
            {
                var isConnected = false;

                if (_server != null)
                {
                    isConnected = await _server.IsAlive(CancellationToken.None).ConfigureAwait(false);
                }

                SetConnectedState(isConnected);
            }
            catch (Exception e)
            {
                SetConnectedState(false);

                // Don't spam-log, this method is called repeatedly
                Debug.Assert(false, $"Health check failure: {e.Message}");
            }
            finally
            {
                if (_isConnected)
                {
                    _healthCheckTimer.Start();
                }
            }
        }

        private void SetConnectedState(bool isConnected)
        {
            try
            {
                if (_isConnected == isConnected)
                {
                    return;
                }

                _isConnected = isConnected;

                if (!isConnected)
                {
                    RaiseDisconnect();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }

        private void RaiseDisconnect()
        {
            Disconnect?.Invoke(this, EventArgs.Empty);
        }

        private void ThrowIfServerIsNull()
        {
            if (_server == null)
            {
                throw new Exception("The deploy tool server is not available. Try restarting Visual Studio.");
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _healthCheckTimer.Stop();
                _healthCheckTimer.Dispose();

                if (_isConnected)
                {
                    Stop();
                }

                if (_server is ServerModeSession serverModeSession)
                {
                    serverModeSession.Dispose();
                    _server = null;
                }
            }
        }
    }
}
