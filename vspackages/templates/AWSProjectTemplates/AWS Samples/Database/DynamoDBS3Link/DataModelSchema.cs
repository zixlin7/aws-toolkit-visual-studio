using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace $safeprojectname$
{
    [DynamoDBTable("Profiles")]
    public class Profile
    {
        [DynamoDBHashKey]
        public string Name { get; set; }

        public S3Link ProfilePicture { get; set; }

        public int Age { get; set; }

        [DynamoDBProperty(AttributeName = "Interests")]
        public List<string> Likes { get; set; }
    }
}
