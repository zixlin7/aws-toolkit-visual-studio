using Microsoft.VisualStudio.Imaging.Interop;

namespace AWSToolkit.Tests.Common.VS
{
    public static class ImageMonikerExtensionMethods
    {
        public static bool ValueEquals(this ImageMoniker @this, ImageMoniker moniker)
        {
            return @this.Id == moniker.Id &&
                   @this.Guid == moniker.Guid;
        }
    }
}
