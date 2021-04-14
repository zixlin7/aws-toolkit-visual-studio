using System.Windows.Media.Imaging;

namespace Amazon.AWSToolkit.CommonUI.Images
{
    /// <summary>
    /// This is the abstraction for the VSSDK image service, which provides high quality, scalable images.
    /// Most of the Toolkit does not have direct access to the VSSDK.
    /// This provider exposes the images served by VS, and <see cref="VsKnownImages"/> is the
    /// abstraction around the image Identifiers.
    /// 
    /// Image service details: https://docs.microsoft.com/en-us/visualstudio/extensibility/image-service-and-catalog
    /// </summary>
    public interface IVsImageProvider
    {
        BitmapSource GetImage(VsKnownImages knownImage, int size);
    }
}
