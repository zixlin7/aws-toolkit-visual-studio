using System.Windows.Media.Imaging;
using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Toolkit abstraction around images that are provided by Visual Studio.
    /// Follows a similar pattern to how Themes and CommonIcons are provided.
    /// 
    /// At design-time, these images are blank, since they aren't loaded from anywhere.
    /// At run-time, the images are provided.
    /// 
    /// Image service details: https://docs.microsoft.com/en-us/visualstudio/extensibility/image-service-and-catalog
    /// </summary>
    public class VsImages
    {
        private static IVsImageProvider _imageProvider;

        public static void Initialize(IVsImageProvider imageProvider)
        {
            _imageProvider = imageProvider;
        }

        public BitmapSource AddUser => _imageProvider?.GetImage(VsKnownImages.AddUser, 16);
        public BitmapSource Cancel => _imageProvider?.GetImage(VsKnownImages.Cancel, 16);
        public BitmapSource DeleteListItem => _imageProvider?.GetImage(VsKnownImages.DeleteListItem, 16);
        public BitmapSource Edit => _imageProvider?.GetImage(VsKnownImages.Edit, 16);
        public BitmapSource FeedbackFrown => _imageProvider?.GetImage(VsKnownImages.FeedbackFrown, 16);
        public BitmapSource FeedbackSmile => _imageProvider?.GetImage(VsKnownImages.FeedbackSmile, 16);
        public BitmapSource Refresh => _imageProvider?.GetImage(VsKnownImages.Refresh, 16);
        public BitmapSource Remove => _imageProvider?.GetImage(VsKnownImages.Remove, 16);
        public BitmapSource StatusError => _imageProvider?.GetImage(VsKnownImages.StatusError, 16);
        public BitmapSource StatusWarning => _imageProvider?.GetImage(VsKnownImages.StatusWarning, 16);
    }
}
