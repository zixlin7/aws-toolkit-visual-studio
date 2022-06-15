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

        public BitmapSource Add => _imageProvider?.GetImage(VsKnownImages.Add, 16);
        public BitmapSource AddLink => _imageProvider?.GetImage(VsKnownImages.AddLink, 16);
        public BitmapSource AddUser => _imageProvider?.GetImage(VsKnownImages.AddUser, 16);
        public BitmapSource Calendar => _imageProvider?.GetImage(VsKnownImages.Calendar, 16);
        public BitmapSource Calendar32 => _imageProvider?.GetImage(VsKnownImages.Calendar, 32);
        public BitmapSource Cancel => _imageProvider?.GetImage(VsKnownImages.Cancel, 16);
        public BitmapSource Cloud => _imageProvider?.GetImage(VsKnownImages.Cloud, 16);
        public BitmapSource Cloud32 => _imageProvider?.GetImage(VsKnownImages.Cloud, 32);
        public BitmapSource CloudRun => _imageProvider?.GetImage(VsKnownImages.CloudRun, 16);
        public BitmapSource Copy => _imageProvider?.GetImage(VsKnownImages.Copy, 16);
        public BitmapSource DeleteListItem => _imageProvider?.GetImage(VsKnownImages.DeleteListItem, 16);
        public BitmapSource DownloadLog => _imageProvider?.GetImage(VsKnownImages.DownloadLog, 16);
        public BitmapSource Edit => _imageProvider?.GetImage(VsKnownImages.Edit, 16);
        public BitmapSource FeedbackFrown => _imageProvider?.GetImage(VsKnownImages.FeedbackFrown, 16);
        public BitmapSource FeedbackFrown32 => _imageProvider?.GetImage(VsKnownImages.FeedbackFrown, 32);
        public BitmapSource FeedbackSmile => _imageProvider?.GetImage(VsKnownImages.FeedbackSmile, 16);
        public BitmapSource FeedbackSmile32 => _imageProvider?.GetImage(VsKnownImages.FeedbackSmile, 32);
        public BitmapSource Loading => _imageProvider?.GetImage(VsKnownImages.Loading, 16);
        public BitmapSource Refresh => _imageProvider?.GetImage(VsKnownImages.Refresh, 16);
        public BitmapSource Remove => _imageProvider?.GetImage(VsKnownImages.Remove, 16);
        public BitmapSource RemoveLink => _imageProvider?.GetImage(VsKnownImages.RemoveLink, 16);
        public BitmapSource Search => _imageProvider?.GetImage(VsKnownImages.Search, 16);
        public BitmapSource Save => _imageProvider?.GetImage(VsKnownImages.Save, 16);
        public BitmapSource StatusError => _imageProvider?.GetImage(VsKnownImages.StatusError, 16);
        public BitmapSource StatusError32 => _imageProvider?.GetImage(VsKnownImages.StatusError, 32);
        public BitmapSource StatusInformation => _imageProvider?.GetImage(VsKnownImages.StatusInformation, 16);
        public BitmapSource StatusInformation32 => _imageProvider?.GetImage(VsKnownImages.StatusInformation, 32);
        public BitmapSource StatusInformationOutline => _imageProvider?.GetImage(VsKnownImages.StatusInformationOutline, 16);
        public BitmapSource StatusOK => _imageProvider?.GetImage(VsKnownImages.StatusOK, 16);
        public BitmapSource StatusOK32 => _imageProvider?.GetImage(VsKnownImages.StatusOK, 32);
        public BitmapSource StatusWarning => _imageProvider?.GetImage(VsKnownImages.StatusWarning, 16);
        public BitmapSource StatusWarning32 => _imageProvider?.GetImage(VsKnownImages.StatusWarning, 32);
        public BitmapSource Upload => _imageProvider?.GetImage(VsKnownImages.Upload, 16);
        public BitmapSource WordWrap => _imageProvider?.GetImage(VsKnownImages.WordWrap, 16);
        public BitmapSource WordWrap32 => _imageProvider?.GetImage(VsKnownImages.WordWrap, 32);
    }
}
