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
        const int ONE_MEGABYTE = 1024 * 1024;
        private readonly ImageDetail _inner;

        public ImageDetailWrapper(ImageDetail inner)
        {
            _inner = inner;
        }

        // temp property until we get a listbox in the grid column
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
            get
            {
                if (_inner == null)
                    return "";

                var size = (double) _inner.ImageSizeInBytes / ONE_MEGABYTE;
                if (size < 0.01)
                    return "< 0.01";

                return size.ToString("N2");
            }
        }

        public string PushedAt
        {
            get { return _inner?.ImagePushedAt.ToString() ?? ""; }
        }
    }
}
