using System;

using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class Ec2Exception : ToolkitException
    {
        public enum Ec2ErrorCode
        {
            InternalMissingEc2State,
            NoElasticIp,
            NoImages,
            NoInstances,
            NoPublicIp,
            NoSecurityGroupCreated,
        }

        public Ec2Exception(string message, Ec2ErrorCode errorCode) : this(message, errorCode, null) { }

        public Ec2Exception(string message, Ec2ErrorCode errorCode, Exception e)
            : base(message, errorCode.ToString(), e)
        {
        }
    }
}
