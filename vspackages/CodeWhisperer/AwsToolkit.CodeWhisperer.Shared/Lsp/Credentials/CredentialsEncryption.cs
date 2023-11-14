using System;
using System.Collections.Generic;
using System.Security.Cryptography;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.Runtime;

using Jose;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials
{
    internal class CredentialsEncryption
    {
        private const int _encryptionKeyBytes = 32;

        private static readonly JwtSettings _jwtSettings = new JwtSettings()
        {
            JsonMapper = new JwtJsonMapper(),
        };

        private readonly byte[] _encryptionKey = new byte[_encryptionKeyBytes];

        public CredentialsEncryption() : this(CreateEncryptionKey())
        {
        }

        /// <summary>
        /// Ctor overload that accepts an encryption key.
        /// Used for testing purposes.
        /// </summary>
        public CredentialsEncryption(byte[] encryptionKey)
        {
            _encryptionKey = encryptionKey;
        }

        /// <summary>
        /// Create a request to send to the language server for the credentials initialization handshake.
        /// The request defines the credentials encryption key that will be used for the session.
        /// </summary>
        public CredentialsEncryptionInitializationRequest CreateEncryptionInitializationRequest()
        {
            var request = new CredentialsEncryptionInitializationRequest()
            {
                Mode = CredentialsEncryptionInitializationRequest.Modes.Jwt,
                Key = _encryptionKeyAsBase64,
            };

            return request;
        }

        /// <summary>
        /// Create a request to send to the language server's "update credentials" method containing the provided credentials.
        /// Credentials are encrypted and stored in a JWT.
        /// </summary>
        public UpdateCredentialsRequest CreateUpdateCredentialsRequest(ImmutableCredentials credentials)
        {
            var requestData = new Sigv4Credentials
            {
                AccessKeyId = credentials.AccessKey,
                SecretAccessKey = credentials.SecretKey,
                SessionToken = credentials.Token,
            };

            return CreateUpdateCredentialsRequest(requestData);
        }

        /// <summary>
        /// Create a request to send to the language server's "update credentials" method containing the provided token.
        /// Credentials are encrypted and stored in a JWT.
        /// </summary>
        public UpdateCredentialsRequest CreateUpdateCredentialsRequest(BearerToken token)
        {
            return CreateUpdateCredentialsRequest((CredentialsData) token);
        }

        /// <summary>
        /// Creates an "update credentials" language server request that contains encrypted data
        /// </summary>
        private UpdateCredentialsRequest CreateUpdateCredentialsRequest(CredentialsData data)
        {
            var payload = new Dictionary<string, object>()
            {
                { JwtFieldNames.Data, data },
            };

            var notBefore = new DateTimeOffset(DateTime.UtcNow.AddMinutes(-1)).ToUnixTimeSeconds();
            var expiresOn = new DateTimeOffset(DateTime.UtcNow.AddMinutes(1)).ToUnixTimeSeconds();

            var headers = new Dictionary<string, object>()
            {
                { JwtFieldNames.NotBefore, notBefore },
                { JwtFieldNames.ExpiresOn, expiresOn },
            };


            var jwt = JWT.Encode(
                payload, _encryptionKey,
                JweAlgorithm.DIR, JweEncryption.A256GCM,
                null,
                headers, _jwtSettings);

            return new UpdateCredentialsRequest
            {
                Data = jwt,
            };
        }

        private string _encryptionKeyAsBase64 => Convert.ToBase64String(_encryptionKey);

        private static byte[] CreateEncryptionKey()
        {
            using (var aes = Aes.Create())
            {
                // aes.KeySize is in bits
                aes.KeySize = _encryptionKeyBytes * 8;
                aes.GenerateKey();
                return aes.Key;
            }
        }
    }
}
