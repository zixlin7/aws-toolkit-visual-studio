using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.Lambda
{
    public static class LambdaContants
    {
        public const string SeedFunctionName = "SeedFunctionName";
        public const string SeedDescription = "SeedDescription";
        public const string SeedFileName = "SeedFileName";
        public const string SeedHandler = "SeedHandler";
        public const string SeedSourcePath = "SeedSourcePath";
        public const string SeedIAMRole = "SeedIAMRole";
        public const string SeedMemory = "SeedMemory";
        public const string SeedTimeout = "SeedTimeout";

        public const string DefaultHandlerName = "handler";

        public const string PARAM_LAMBDA_FUNCTION = "ParamFunction";

        public static readonly string LAMBDA_ASSUME_ROLE_POLICY =
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
    }
  ]
}
".Trim();


        public const string ERROR_MESSAGE_FOR_TASK_CANT_ASSUMED = "The role defined for the task cannot be assumed by Lambda.";
        public const string ERROR_MESSAGE_UNABLE_TO_VALIDATE_DESTINATION = "Unable to validate the following destination configurations";
        public const string ERROR_MESSAGE_START_ADD_TRUST_ENTITY = "Please add Lambda as a Trusted Entity";
    }
}
