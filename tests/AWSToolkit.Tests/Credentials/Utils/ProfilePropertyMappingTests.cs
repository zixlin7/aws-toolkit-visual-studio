using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Internal.Settings;
using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class ProfilePropertyMappingTests
    {
        private ProfilePropertyMapping _mapping;

        private static readonly Dictionary<string, string> EmptyPropertyMapping =
            new Dictionary<string, string>();

        private static readonly Dictionary<string, string> SharedInvalidPropertyMapping =
            new Dictionary<string, string>()
            {
                {ProfilePropertyConstants.AccessKey, "aws_access_key_id"},
                {ProfilePropertyConstants.CredentialSource, "credential_source"}
            };

        private static readonly Dictionary<string, string> SdkInvalidPropertyMapping =
            new Dictionary<string, string>()
            {
                {ProfilePropertyConstants.AccessKey, SettingsConstants.AccessKeyField},
                {ProfilePropertyConstants.CredentialSource, SettingsConstants.CredentialSourceField}
            };

        private readonly Dictionary<string, string> _sampleSharedDictionary =
            new Dictionary<string, string>()
            {
                {"aws_access_key_id", "accesskey"}, {"aws_secret_access_key", "secretkey"}
            };

        private readonly Dictionary<string, string> _sampleSdkDictionary =
            new Dictionary<string, string>()
            {
                {SettingsConstants.AccessKeyField, "accesskey"}, {SettingsConstants.SecretKeyField, "secretkey"}
            };

        private readonly CredentialProfileOptions _expectedProfileOptions =
            new CredentialProfileOptions() {AccessKey = "accesskey", SecretKey = "secretkey"};

        private static readonly HashSet<string> TypePropertySet =
            new HashSet<string>(typeof(CredentialProfileOptions).GetProperties().Select((p) => p.Name),
                StringComparer.OrdinalIgnoreCase);

        [Fact]
        public void ValidSharedPropertyMap()
        {
            _mapping = new ProfilePropertyMapping(SharedCredentialFileReader.PropertyMapping);
            Assert.Equal(TypePropertySet, new HashSet<string>(SharedCredentialFileReader.PropertyMapping.Keys),
                StringComparer.OrdinalIgnoreCase);
            Assert.Equal(_expectedProfileOptions, _mapping.ExtractProfileOptions(_sampleSharedDictionary));
        }

        [Fact]
        public void InvalidSharedPropertyMap()
        {
            _mapping = new ProfilePropertyMapping(SdkInvalidPropertyMapping);

            Assert.NotEqual(TypePropertySet, new HashSet<string>(SharedInvalidPropertyMapping.Keys),
                StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(_expectedProfileOptions, _mapping.ExtractProfileOptions(_sampleSharedDictionary));
        }

        [Fact]
        public void ValidSdkPropertyMap()
        {
            _mapping = new ProfilePropertyMapping(SDKCredentialFileReader.PropertyMapping);
            Assert.Equal(TypePropertySet, new HashSet<string>(SDKCredentialFileReader.PropertyMapping.Keys),
                StringComparer.OrdinalIgnoreCase);
            Assert.Equal(_expectedProfileOptions, _mapping.ExtractProfileOptions(_sampleSdkDictionary));
        }

        [Fact]
        public void InvalidSdkPropertyMap()
        {
            _mapping = new ProfilePropertyMapping(SdkInvalidPropertyMapping);
            Assert.NotEqual(TypePropertySet, new HashSet<string>(SdkInvalidPropertyMapping.Keys),
                StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(_expectedProfileOptions, _mapping.ExtractProfileOptions(_sampleSdkDictionary));
        }

        [Fact]
        public void InvalidEmptyPropertyMap()
        {
            _mapping = new ProfilePropertyMapping(EmptyPropertyMapping);
            Assert.NotEqual(TypePropertySet, new HashSet<string>(EmptyPropertyMapping.Keys),
                StringComparer.OrdinalIgnoreCase);
            Assert.NotEqual(_expectedProfileOptions, _mapping.ExtractProfileOptions(_sampleSdkDictionary));
        }
    }
}
