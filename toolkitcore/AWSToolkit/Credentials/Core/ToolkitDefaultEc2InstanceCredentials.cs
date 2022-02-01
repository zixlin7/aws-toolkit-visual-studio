using System;
using System.Threading;

using Amazon.Runtime;
using Amazon.Util;

using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// This class supports credentials that are Assuming Roles using the EC2 Instance as the credential source.
    /// Sourced from AWS SDK for .NET, since that implementation is an internal class:
    /// https://github.com/aws/aws-sdk-net/blob/master/sdk/src/Core/Amazon.Runtime/Credentials/DefaultInstanceProfileAWSCredentials.cs
    /// </summary>
    public class ToolkitDefaultEc2InstanceCredentials : AWSCredentials, IDisposable
    {
        private static readonly object InstanceLock = new object();
        private readonly ReaderWriterLockSlim _credentialsLock = new ReaderWriterLockSlim(); // Lock to control getting credentials across multiple threads.

        private readonly Timer _credentialsRetrieverTimer;
        private ImmutableCredentials _lastRetrievedCredentials;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ToolkitDefaultEc2InstanceCredentials));

        private static readonly TimeSpan NeverTimespan = TimeSpan.FromMilliseconds(-1);
        private static readonly TimeSpan RefreshRate = TimeSpan.FromMinutes(2); // EC2 refreshes credentials 5 min before expiration
        private const string FailedToGetCredentialsMessage = "Failed to retrieve credentials from EC2 Instance Metadata Service.";
        private static readonly TimeSpan CredentialsLockTimeout = TimeSpan.FromMilliseconds(5000);

        private static ToolkitDefaultEc2InstanceCredentials _instance;

        public static ToolkitDefaultEc2InstanceCredentials Instance
        {
            get
            {
                CheckIsImdsEnabled();

                if (_instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ToolkitDefaultEc2InstanceCredentials();
                        }
                    }
                }

                return _instance;
            }
        }

        private ToolkitDefaultEc2InstanceCredentials()
        {
            // if IMDS is turned off, no need to spin up the timer task
            if (!EC2InstanceMetadata.IsIMDSEnabled)
            {
                return;
            }

            _credentialsRetrieverTimer = new Timer(RenewCredentials, null, TimeSpan.Zero, NeverTimespan);
        }

        /// <summary>
        /// Returns a copy of the most recent instance profile credentials.
        /// </summary>
        public override ImmutableCredentials GetCredentials()
        {
            CheckIsImdsEnabled();
            ImmutableCredentials credentials = null;

            // Try to acquire read lock. The thread would be blocked if another thread has write lock.
            if (_credentialsLock.TryEnterReadLock(CredentialsLockTimeout))
            {
                try
                {
                    credentials = _lastRetrievedCredentials?.Copy();

                    if (credentials != null)
                    {
                        return credentials;
                    }
                }
                finally
                {
                    _credentialsLock.ExitReadLock();
                }
            }

            // If there's no credentials cached, hit IMDS directly. Try to acquire write lock.
            if (_credentialsLock.TryEnterWriteLock(CredentialsLockTimeout))
            {
                try
                {
                    // Check for last retrieved credentials again in case other thread might have already fetched it.
                    credentials = _lastRetrievedCredentials?.Copy();
                    if (credentials == null)
                    {
                        credentials = FetchCredentials();
                        _lastRetrievedCredentials = credentials;
                    }
                }
                finally
                {
                    _credentialsLock.ExitWriteLock();
                }
            }

            if (credentials == null)
            {
                throw new AmazonServiceException(FailedToGetCredentialsMessage);
            }

            return credentials;
        }

        private void RenewCredentials(object unused)
        {
            try
            {
                ImmutableCredentials newCredentials = FetchCredentials();

                _lastRetrievedCredentials = newCredentials;
            }
            catch (OperationCanceledException e)
            {
                // in this case, keep the lastRetrievedCredentials
                Logger.Error("RenewCredentials task canceled", e);
            }
            catch (Exception e)
            {
                _lastRetrievedCredentials = null;

                // we want to suppress any exceptions from this timer task.
                Logger.Error(FailedToGetCredentialsMessage, e);

            }
            finally
            {
                // re-invoke this task once after time specified by refreshRate
                _credentialsRetrieverTimer.Change(RefreshRate, NeverTimespan);
            }
        }

        private static ImmutableCredentials FetchCredentials()
        {
            var securityCredentials = EC2InstanceMetadata.IAMSecurityCredentials;
            if (securityCredentials == null)
            {
                throw new AmazonServiceException(
                    "Unable to get IAM security credentials from EC2 Instance Metadata Service.");
            }

            string firstRole = null;
            foreach (var role in securityCredentials.Keys)
            {
                firstRole = role;
                break;
            }

            if (string.IsNullOrEmpty(firstRole))
            {
                throw new AmazonServiceException("Unable to get EC2 instance role from EC2 Instance Metadata Service.");
            }

            var metadata = securityCredentials[firstRole];
            if (metadata == null)
            {
                throw new AmazonServiceException(
                    $"Unable to get credentials for role \"{firstRole}\" from EC2 Instance Metadata Service.");
            }

            return new ImmutableCredentials(metadata.AccessKeyId, metadata.SecretAccessKey, metadata.Token);
        }

        private static void CheckIsImdsEnabled()
        {
            // keep this behavior consistent with GetObjectFromResponse case.
            if (!EC2InstanceMetadata.IsIMDSEnabled)
            {
                throw new AmazonServiceException("Unable to retrieve credentials.");
            }
        }

        #region IDisposable
        private bool _isDisposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    lock (InstanceLock)
                    {
                        _credentialsRetrieverTimer.Dispose();
                        _instance = null;
                    }
                }

                _isDisposed = true;
            }
        }

        /// <summary>
        /// Dispose this object and all related resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
