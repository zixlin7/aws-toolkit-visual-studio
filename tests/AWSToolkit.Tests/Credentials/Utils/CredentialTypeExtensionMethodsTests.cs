using System;
using System.Linq;
using System.Collections.Generic;
using Amazon.AWSToolkit.Credentials.Utils;

using Xunit;

using TelemetryCredentialType = Amazon.AwsToolkit.Telemetry.Events.Generated.CredentialType;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class CredentialTypeExtensionMethodsTests
    {
        private static readonly Dictionary<CredentialType, TelemetryCredentialType?> CredentialTypeMappings =
            new Dictionary<CredentialType, TelemetryCredentialType?>()
            {
                {CredentialType.StaticProfile, TelemetryCredentialType.StaticProfile},
                {CredentialType.StaticSessionProfile, TelemetryCredentialType.StaticSessionProfile},
                {CredentialType.CredentialProcessProfile, TelemetryCredentialType.CredentialProcessProfile},
                {CredentialType.AssumeRoleProfile, TelemetryCredentialType.AssumeRoleProfile},
                {CredentialType.AssumeMfaRoleProfile, TelemetryCredentialType.AssumeMfaRoleProfile},
                {CredentialType.SsoProfile, TelemetryCredentialType.SsoProfile},
                {CredentialType.AssumeSamlRoleProfile, TelemetryCredentialType.AssumeSamlRoleProfile},
                {CredentialType.Unknown, TelemetryCredentialType.Other},
                {CredentialType.Undefined, null},
            };

        [Fact]
        public void AsTelemetryCredentialType()
        {
            foreach (var credentialTypeMapping in CredentialTypeMappings)
            {
                Assert.Equal(credentialTypeMapping.Value, credentialTypeMapping.Key.AsTelemetryCredentialType());
            }
        }

        /// <summary>
        /// This test makes sure we're logging credential types as "Other" if we forget to update the
        /// mapping.
        /// </summary>
        [Fact]
        public void AsTelemetryCredentialType_UnexpectedTypes()
        {
            foreach (var credentialType in GetUnexpectedCredentialTypes())
            {
                Assert.Equal(TelemetryCredentialType.Other, credentialType.AsTelemetryCredentialType());
            }
        }

        /// <summary>
        /// This test helps ensure we update the mapping before releasing updated Credential types.
        /// </summary>
        [Fact]
        public void AsTelemetryCredentialType_GuardAgainstUnexpectedTypes()
        {
            Assert.Empty(GetUnexpectedCredentialTypes());
        }

        /// <summary>
        /// Defensive programming: check if this test wasn't updated when <see cref="CredentialType"/> was
        /// </summary>
        private IList<CredentialType> GetUnexpectedCredentialTypes()
        {
            return Enum.GetValues(typeof(CredentialType))
                .OfType<CredentialType>()
                .Except(CredentialTypeMappings.Keys)
                .ToList();
        }
    }
}
