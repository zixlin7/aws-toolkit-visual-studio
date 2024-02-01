using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Credentials.Internal;
using Amazon.Runtime.Internal;
using Amazon.Util;
using Amazon.Util.Internal;

namespace Amazon.AWSToolkit.Credentials.Sono
{
    /// <summary>
    ///  Provides a way to generate a Token Provider that the Toolkit can use to get tokens from Sono
    /// </summary>
    public sealed class SonoTokenProviderBuilder
    {
        private IAWSToolkitShellProvider _toolkitShell;
        private ICredentialIdentifier _credentialIdentifier;
        private string _sessionName;
        private RegionEndpoint _oidcRegion = SonoProperties.DefaultOidcRegion;
        private RegionEndpoint _tokenProviderRegion = SonoProperties.DefaultTokenProviderRegion;
        private string[] _scopes = new string[] { SonoProperties.SsoAccountAccessScope };
        private string _startUrl = SonoProperties.StartUrl;
        private bool _isBuilderId = true;
        private Action<SsoVerificationArguments> _ssoCallbackHandler;

        private IFile _tokenCacheFileHandler = new FileRetriever();
        private IDirectory _tokenCacheDirectoryHandler = new DirectoryRetriever();

        public static SonoTokenProviderBuilder Create()
        {
            return new SonoTokenProviderBuilder();
        }

        public SonoTokenProviderBuilder ShallowClone()
        {
            return MemberwiseClone() as SonoTokenProviderBuilder;
        }

        private SonoTokenProviderBuilder() { }

        public SonoTokenProviderBuilder WithToolkitShell(IAWSToolkitShellProvider toolkitShell)
        {
            _toolkitShell = toolkitShell;
            return this;
        }

        public SonoTokenProviderBuilder WithCredentialIdentifier(ICredentialIdentifier credentialIdentifier)
        {
            _credentialIdentifier = credentialIdentifier;
            return this;
        }

        public SonoTokenProviderBuilder WithSessionName(string sessionName)
        {
            _sessionName = sessionName;
            return this;
        }

        public SonoTokenProviderBuilder WithTokenProviderRegion(RegionEndpoint tokenProviderRegion)
        {
            _tokenProviderRegion = tokenProviderRegion;
            return this;
        }

        public SonoTokenProviderBuilder WithStartUrl(string startUrl)
        {
            _startUrl = startUrl;
            return this;
        }

        public SonoTokenProviderBuilder WithOidcRegion(RegionEndpoint oidcRegion)
        {
            _oidcRegion = oidcRegion;
            return this;
        }

        public SonoTokenProviderBuilder WithIsBuilderId(bool isBuilderId)
        {
            _isBuilderId = isBuilderId;
            return this;
        }

        public SonoTokenProviderBuilder WithScopes(params string[] scopes)
        {
            _scopes = scopes;
            return this;
        }

        public SonoTokenProviderBuilder WithSsoCallback(Action<SsoVerificationArguments> callbackHandler)
        {
            _ssoCallbackHandler = callbackHandler;
            return this;
        }

        /// <summary>
        /// Intended for use with testing
        /// </summary>
        public SonoTokenProviderBuilder WithTokenCacheFileHandler(IFile fileHandler)
        {
            _tokenCacheFileHandler = fileHandler;
            return this;
        }

        /// <summary>
        /// Intended for use with testing
        /// </summary>
        public SonoTokenProviderBuilder WithTokenCacheDirectoryHandler(IDirectory directoryHandler)
        {
            _tokenCacheDirectoryHandler = directoryHandler;
            return this;
        }

        public IAWSTokenProvider Build()
        {
            ValidateProperties();

            var tokenManager = CreateTokenManager();

            return new SSOTokenProvider(tokenManager, _sessionName, _startUrl, _tokenProviderRegion.SystemName);
        }

        /// <summary>
        /// Throws an exception if a property is missing
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        private void ValidateProperties()
        {
            // If a callback handler needs to be created, ToolkitShell and CredentialId are required
            if (_ssoCallbackHandler == null)
            {
                if (_toolkitShell == null)
                {
                    throw new InvalidOperationException("Toolkit shell is missing");
                }

                if (_credentialIdentifier == null)
                {
                    throw new InvalidOperationException("Credential Id is missing");
                }
            }

            if (string.IsNullOrWhiteSpace(_sessionName))
            {
                throw new InvalidOperationException("Session Name cannot be empty");
            }

            if (_tokenProviderRegion == null)
            {
                throw new InvalidOperationException("Token Provider region is missing");
            }

            if (string.IsNullOrWhiteSpace(_startUrl))
            {
                throw new InvalidOperationException("Start URL is missing");
            }

            if (_oidcRegion == null)
            {
                throw new InvalidOperationException("OIDC region is missing");
            }

            if (_scopes == null)
            {
                throw new InvalidOperationException("Scopes is missing");
            }

            if (_tokenCacheFileHandler == null)
            {
                throw new InvalidOperationException("Token cache file handler is missing");
            }

            if (_tokenCacheDirectoryHandler == null)
            {
                throw new InvalidOperationException("Token cache directory handler is missing");
            }
        }

        private ISSOTokenManager CreateTokenManager()
        {
            var tokenManagerOptions = new ToolkitSsoTokenManagerOptions(SonoProperties.ClientName, SonoProperties.ClientType, _credentialIdentifier.DisplayName, _ssoCallbackHandler, _scopes);

            var ssoOidcClient = SSOServiceClientHelpers.BuildSSOIDCClient(_oidcRegion);

            var ssoTokenFileCache = new ValidatingSSOTokenFileCache(
                CryptoUtilFactory.CryptoInstance,
                _tokenCacheFileHandler,
                _tokenCacheDirectoryHandler);

            var ssoTokenManager = new SSOTokenManager(ssoOidcClient, ssoTokenFileCache);

            return new ToolkitSsoTokenManager(ssoTokenManager, tokenManagerOptions, _toolkitShell);
        }

        /// <summary>
        /// Adapter for SSOTokenFileCache that verifies the region and start URL match the cached token
        /// </summary>
        /// <remarks>
        /// Uses interface implementation instead of extending class because methods aren't virtual in SSOTokenFileCache.
        /// </remarks>
        private class ValidatingSSOTokenFileCache : ISSOTokenFileCache
        {
            private readonly ISSOTokenFileCache _cache;

            public ValidatingSSOTokenFileCache(ICryptoUtil cryptoUtil, IFile file, IDirectory directory)
            {
                _cache = new SSOTokenFileCache(cryptoUtil, file, directory);
            }

            public bool Exists(CredentialProfileOptions options)
            {
                return _cache.Exists(options);
            }

            public void SaveSsoToken(SsoToken token, string ssoCacheDirectory)
            {
                _cache.SaveSsoToken(token, ssoCacheDirectory);
            }

            public Task SaveSsoTokenAsync(SsoToken token, string ssoCacheDirectory, CancellationToken cancellationToken = default)
            {
                return _cache.SaveSsoTokenAsync(token, ssoCacheDirectory, cancellationToken);
            }

            public bool TryGetSsoToken(SSOTokenManagerGetTokenOptions getSsoTokenOptions, string ssoCacheDirectory, out SsoToken ssoToken)
            {
                var tokenFound = _cache.TryGetSsoToken(getSsoTokenOptions, ssoCacheDirectory, out ssoToken);

                if (tokenFound && !VerifyCachedMatchesOptions(getSsoTokenOptions, ssoToken))
                {
                    ssoToken = null;
                    return false;
                }

                return tokenFound;
            }

            public async Task<TryResponse<SsoToken>> TryGetSsoTokenAsync(SSOTokenManagerGetTokenOptions getSsoTokenOptions, string ssoCacheDirectory, CancellationToken cancellationToken = default)
            {
                var tryResponse = await _cache.TryGetSsoTokenAsync(getSsoTokenOptions, ssoCacheDirectory, cancellationToken);

                if (tryResponse.Success && !VerifyCachedMatchesOptions(getSsoTokenOptions, tryResponse.Value))
                {
                    tryResponse.Value = null;
                    tryResponse.Success = false;
                }

                return tryResponse;
            }

            private bool VerifyCachedMatchesOptions(SSOTokenManagerGetTokenOptions getSsoTokenOptions, SsoToken ssoToken)
            {
                // See SsoTokenUtils in AWSSDK, but session is not stored in JSON, so do not compare session here
                return ssoToken.StartUrl == getSsoTokenOptions.StartUrl &&
                    ssoToken.Region == getSsoTokenOptions.Region;
            }
        }
    }
}
