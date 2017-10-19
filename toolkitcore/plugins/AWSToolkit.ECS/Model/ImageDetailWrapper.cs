using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.ECR.Model;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ImageDetailWrapper
    {
        private readonly ImageDetail _inner;

        public ImageDetailWrapper(ImageDetail inner)
        {
            _inner = inner;
        }

        public string ImageTagsFlattened
        {
            get
            {
                var tags = new StringBuilder();
                if (_inner != null)
                {
                    foreach (var t in _inner.ImageTags)
                    {
                        if (tags.Length > 0)
                            tags.Append(";");
                        tags.Append(t);
                    }
                }
                return tags.ToString();
            }
        }

        public string Digest
        {
            get { return _inner?.ImageDigest ?? ""; }
        }

        public string Size
        {
            get { return _inner?.ImageSizeInBytes.ToString() ?? ""; }
        }

        public string PushedAt
        {
            get { return _inner?.ImagePushedAt.ToLocalTime().ToString("yy-MM-dd HH:mm:ss zzz") ?? ""; }
        }
    }
}
