using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

using Amazon.AWSToolkit.CommonUI.Images;

using log4net;

using Microsoft;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Amazon.AWSToolkit.VisualStudio.Images
{
    /// <summary>
    /// A Toolkit encapsulation to fetch images from Visual Studio.
    /// This is intended as an alternative to using CrispImage and KnownMonikers from
    /// places in the Toolkit that do not have access to the VSSDK.
    /// 
    /// Image service details: https://docs.microsoft.com/en-us/visualstudio/extensibility/image-service-and-catalog
    /// </summary>
    public class VsImageProvider : IVsImageProvider
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(VsImageProvider));

        private readonly IVsImageService2 _imageService;

        public VsImageProvider(IServiceProvider serviceProvider)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _imageService = serviceProvider.GetService(typeof(SVsImageService)) as IVsImageService2;
            Assumes.Present(_imageService);
        }

        /// <summary>
        /// Retrieves an image from the VS Image Service that can be used in Image controls
        /// </summary>
        public BitmapSource GetImage(VsKnownImages knownImage, int size)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                var property = typeof(KnownMonikers).GetProperty(knownImage.ToString(),
                    BindingFlags.Static | BindingFlags.Public);
                if (property == null)
                {
                    Logger.Debug($"Unrecognized image: {knownImage}");
                    return null;
                }

                var value = (ImageMoniker) property.GetValue(null, null);
                return LoadImage(value, size);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load image: {knownImage}", e);
                return null;
            }
        }

        /// <summary>
        ///  Retrieves an image from the VS Image Service that can be used in Image controls using the moniker properties supplied
        /// </summary>
        /// <param name="imageGuid">Known moniker image catalog guid</param>
        /// <param name="imageId">Known moniker image Id</param>
        /// <param name="size"></param>
        /// <returns></returns>
        public BitmapSource GetImage(Guid imageGuid, int imageId, int size)
        {
            var moniker = new ImageMoniker() { Guid = imageGuid, Id = imageId };
            try
            {
                return LoadImage(moniker, size);
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to load image: {moniker}", e);
                return null;
            }
        }

        /// <summary>
        /// Loads an image from the Image Service that can be used in WPF Image controls.
        /// </summary>
        private BitmapSource LoadImage(ImageMoniker moniker, int size)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_imageService == null)
            {
                return null;
            }

            var attributes = new ImageAttributes()
            {
                Flags = (uint) _ImageAttributesFlags.IAF_RequiredFlags,
                ImageType = (uint) _UIImageType.IT_Bitmap,
                Format = (uint) _UIDataFormat.DF_WPF,
                LogicalHeight = size,
                LogicalWidth = size,
                StructSize = Marshal.SizeOf(typeof(ImageAttributes))
            };

            IVsUIObject imageObject = _imageService.GetImage(moniker, attributes);

            imageObject.get_Data(out object imageData);

            return imageData as BitmapSource;
        }
    }
}
