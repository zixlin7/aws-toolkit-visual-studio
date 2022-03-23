using System;

namespace Amazon.AWSToolkit.ECS.Models.Ecr
{
    public class Repository
    {
        public DateTime CreatedOn { get; set; }
        public string Arn { get; set; }
        public string Name { get; set; }
        public string Uri { get; set; }
    }
}
