using System;
using System.Collections.Generic;

using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;
using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials.Models;
using Amazon.Runtime;

using FluentAssertions;

using Jose;

using Newtonsoft.Json.Linq;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Credentials
{
    public class CredentialsEncryptionTests
    {
        private static readonly JwtSettings _jwtSettings = new JwtSettings()
        {
            JsonMapper = new JwtJsonMapper(),
        };

        private readonly byte[] _sampleEncryptionKey = new byte[32];
        private readonly string _sampleEncryptionKeyAsBase64;

        private readonly CredentialsEncryption _sut;

        public CredentialsEncryptionTests()
        {
            _sampleEncryptionKeyAsBase64 = Convert.ToBase64String(_sampleEncryptionKey);
            _sut = new CredentialsEncryption(_sampleEncryptionKey);
        }

        [Fact]
        public void CreateEncryptionInitializationRequest()
        {
            var request = _sut.CreateEncryptionInitializationRequest();
            request.Mode.Should().Be(CredentialsEncryptionInitializationRequest.Modes.Jwt);
            request.Key.Should().Be(_sampleEncryptionKeyAsBase64);
        }

        [Fact]
        public void CreateUpdateCredentialsRequest_SigV4Credentials()
        {
            var credentials = new ImmutableCredentials("access-key", "secret", "token");

            var request = _sut.CreateUpdateCredentialsRequest(credentials);

            request.Data.Should().NotBeNull();
            var decodedCredentials = DecodeJwtDataAs<Sigv4Credentials>(request.Data);
            decodedCredentials.AccessKeyId.Should().Be(credentials.AccessKey);
            decodedCredentials.SecretAccessKey.Should().Be(credentials.SecretKey);
            decodedCredentials.SessionToken.Should().Be(credentials.Token);
        }

        [Fact]
        public void CreateUpdateCredentialsRequest_BearerTokenCredentials()
        {
            var credentials = new BearerToken()
            {
                Token = "token",
            };

            var request = _sut.CreateUpdateCredentialsRequest(credentials);

            request.Data.Should().NotBeNull();
            var decodedCredentials = DecodeJwtDataAs<BearerToken>(request.Data);
            decodedCredentials.Token.Should().Be(credentials.Token);
        }

        /// <summary>
        /// Test Utility: decode the JWT, and deserialize the "data" property to the specified type
        /// </summary>
        private T DecodeJwtDataAs<T>(string jwt) where T : class
        {
            var payload = JWT.Decode<Dictionary<string, object>>(jwt, _sampleEncryptionKey, _jwtSettings);
            var payloadData = payload[JwtFieldNames.Data] as JObject;
            return payloadData.ToObject<T>();
        }
    }
}
