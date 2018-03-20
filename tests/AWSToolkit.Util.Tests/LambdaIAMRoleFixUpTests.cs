using System;
using System.IO;
using System.Linq;
using System.Net;
using Amazon.AWSToolkit;
using Xunit;

using Amazon.AWSToolkit.Lambda;
using ThirdParty.Json.LitJson;

namespace AWSToolkit.Util.Tests
{
    public class LambdaIAMRoleFixUpTests
    {
        public static readonly string VALID_ASSUME_POLICY =
@"
{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""lambda.amazonaws.com""
      },
      ""Action"": ""sts:AssumeRole""
    },
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""AWS"": ""arn:aws:iam::111122223333:root""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();

        public static readonly string INVALID_ASSUME_POLICY =
@"
{
  ""Version"": ""2012-10-17"",
  ""Statement"": [
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""Service"": ""lambda.amazonaws.com""
      },
      ""Action"": ""sts:AssumeRole""
    },
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""AWS"": ""arn:aws:iam::111122223333:root""
      },
      ""Action"": ""sts:AssumeRole""
    },
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""AWS"": ""arn:aws:iam::147242972042:root""
      },
      ""Action"": ""sts:AssumeRole""
    },
    {
      ""Sid"": """",
      ""Effect"": ""Allow"",
      ""Principal"": {
        ""AWS"": ""arn:aws:iam::571267556732:root""
      },
      ""Action"": ""sts:AssumeRole""
    }
  ]
}
".Trim();



        [Fact]
        public void DetectAssumeRoleWithInvalidAccounts()
        {
            Assert.True(LambdaUtilities.DoesAssumeRolePolicyDocumentContainsInvalidAccounts(INVALID_ASSUME_POLICY));
        }

        [Fact]
        public void DetectAssumeRoleDoesNotContainInvalidAccounts()
        {
            Assert.False(LambdaUtilities.DoesAssumeRolePolicyDocumentContainsInvalidAccounts(VALID_ASSUME_POLICY));
        }

        [Fact]
        public void RemoveInvalidAccounts()
        {
            var cleanedPolicy = LambdaUtilities.RemoveInvalidAccountsFromAssumeRolePolicyDocument(INVALID_ASSUME_POLICY);

            var rootData = JsonMapper.ToObject(cleanedPolicy) as JsonData;
            var statements = rootData["Statement"] as JsonData;
            Assert.NotNull(statements);
            Assert.Equal(2, statements.Count);
            Assert.True(statements.IsArray);

            foreach(JsonData statement in statements)
            {
                var principal = statement["Principal"] as JsonData;
                Assert.NotNull(principal);

                if(principal["Service"] != null)
                {
                    Assert.Equal("lambda.amazonaws.com", principal["Service"]);
                }
                else if (principal["AWS"] != null)
                {
                    Assert.Equal("arn:aws:iam::111122223333:root", principal["AWS"]);
                }
                else
                {
                    Assert.True(false, "Unknown principal type");
                }
            }
        }
    }
}
