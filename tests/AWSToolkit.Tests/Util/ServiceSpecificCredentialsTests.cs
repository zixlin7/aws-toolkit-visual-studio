using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Util;

using Xunit;

namespace AWSToolkit.Tests.Util
{
    public class ServiceSpecificCredentialsTests
    {
        private const string _password = "test-password";
        private const string _username = "test-username";

        public static IEnumerable<object[]> GetTestData()
        {
            object[] Emit(string username, string password, DateTime? expiresOn = null)
            {
                return new object[] {new ServiceSpecificCredentials(username, password, expiresOn)};
            }

            yield return Emit(_username, _password);
            yield return Emit(string.Empty, _password);
            yield return Emit(_username, _password, DateTime.Now);
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public void CredentialsWrittenUsingToJsonCanBeReadFromJson(ServiceSpecificCredentials expected)
        {
            var actual = ServiceSpecificCredentials.FromJson(expected.ToJson());
            Assert.Equal(expected, actual);
        }
    }
}
