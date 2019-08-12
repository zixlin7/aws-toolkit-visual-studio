using System.Collections.Generic;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public interface ITagSupport
    {
        Tag FindTag(string name);
        void SetTag(string name, string value);

        List<Tag> Tags { get; }
    }
}
