using System;
using System.Windows.Media.Imaging;

using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AWSToolkit.CommonUI
{
    // Source : Microsoft.VisualStudio.ImageCatalog
    // There are issues with incompatible/missing monikers between
    // v16 and v17 versions of ImageCatalog referenced by the toolkit
    // Guids of interest are placed here instead as a workaround
    internal class ImageGuids
    {
        public const string ImageCatalogGuidString = "{ae27a6b0-e345-4288-96df-5eaf394ee369}";
        public static readonly Guid ImageCatalogGuid = new Guid(ImageCatalogGuidString);
        public const int NavigateExternalInlineNoHalo = 3845;
    }

    /// <summary>
    /// Custom VS Images that are retrieved using their Known Moniker Guid
    /// Introduced as a workaround for incompatible/missing monikers between v16 and v17 versions of ImageCatalog referenced by the toolkit
    /// TODO: Remove when we deprecate VS 2019 or move to a later version of VS SDK 2019
    /// </summary>
    public class CustomImages
    {
        private static IVsImageProvider _imageProvider;

        public static void Initialize(IVsImageProvider imageProvider)
        {
            _imageProvider = imageProvider;
        }

        public BitmapSource NavigateExternalInlineNoHalo => _imageProvider?.GetImage(ImageGuids.ImageCatalogGuid, ImageGuids.NavigateExternalInlineNoHalo, 16);
    }
}
