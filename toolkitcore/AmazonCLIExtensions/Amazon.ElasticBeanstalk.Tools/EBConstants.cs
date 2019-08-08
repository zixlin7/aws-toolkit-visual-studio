namespace Amazon.ElasticBeanstalk.Tools
{
    public static class EBConstants
    {
        public const string DEFAULT_MANIFEST = @"
{
  ""manifestVersion"": 1,
  ""deployments"": {

    ""aspNetCoreWeb"": [
      {
        ""name"": ""app"",
        ""parameters"": {
          ""appBundle"": ""."",
          ""iisPath"": ""{iisPath}"",
          ""iisWebSite"": ""{iisWebSite}""
        }
      }
    ]
  }
}
";


    }
}
