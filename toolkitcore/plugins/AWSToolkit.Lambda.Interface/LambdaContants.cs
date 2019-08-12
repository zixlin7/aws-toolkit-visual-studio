namespace Amazon.AWSToolkit.Lambda
{
    public static class LambdaConstants
    {
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


        public const string ERROR_MESSAGE_CANT_BE_ASSUMED = " cannot be assumed by Lambda.";
        public const string ERROR_MESSAGE_UNABLE_TO_VALIDATE_DESTINATION = "Unable to validate the following destination configurations";
        public const string ERROR_MESSAGE_START_ADD_TRUST_ENTITY = "Please add Lambda as a Trusted Entity";
    }
}
