using System;

using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.AWSToolkit.Util;
using Amazon.CodeCatalyst.Model;
using Amazon.IdentityManagement.Model;

using Xunit;

namespace AWSToolkit.Tests.CodeCatalyst.Models
{
    public class CodeCatalystAccessTokenTests
    {
        private const string _name = "test-name";
        private const string _secret = "shhhhhhhhhhhh!";
        private readonly DateTime _expiresOn = DateTime.Now;

        private readonly CodeCatalystAccessToken _sut;

        public CodeCatalystAccessTokenTests()
        {
            _sut = new CodeCatalystAccessToken(_name, _secret, _expiresOn);
        }

        [Fact]
        public void PropertiesReflectCtorWithPrimitiveArgs()
        {
            Assert.Equal(_name, _sut.Name);
            Assert.Equal(_secret, _sut.Secret);
            Assert.Equal(_expiresOn, _sut.ExpiresOn);
        }

        [Fact]
        public void PropertiesReflectCtorWithAwsSdkArgs()
        {
            var createAccessTokenResponse = new CreateAccessTokenResponse()
            {
                Name = _name,
                Secret = _secret,
                ExpiresTime = _expiresOn
            };

            var sut = new CodeCatalystAccessToken(createAccessTokenResponse);

            Assert.Equal(_name, sut.Name);
            Assert.Equal(_secret, sut.Secret);
            Assert.Equal(_expiresOn, sut.ExpiresOn);
        }

        [Fact]
        public void PropertiesReflectCtorWithServiceSpecificCredentialsArgs()
        {
            var creds = new ServiceSpecificCredentials(_name, _secret, _expiresOn);

            var sut = new CodeCatalystAccessToken(creds);

            Assert.Equal(_name, sut.Name);
            Assert.Equal(_secret, sut.Secret);
            Assert.Equal(_expiresOn, sut.ExpiresOn);
        }
    }
}
