using System.Collections.Generic;
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

        public ICollection<string> ImageTags => _inner?.ImageTags;

        public string Digest => _inner?.ImageDigest ?? "";

        public ImageDetail NativeImageDetail => this._inner;

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

        public string PushedAt => _inner?.ImagePushedAt.ToLocalTime().ToString() ?? "";
    }
}
