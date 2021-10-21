using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Amazon.AWSToolkit.Tests.Publishing.Util
{
    public static class ImageSourceExtensionMethods
    {
        /// <summary>
        /// Compares <see cref="ImageSource"/>(imagesource1) with the given <see cref="ImageSource"/>(imageSource2) for equality
        /// </summary>
        public static bool IsEqual(this ImageSource imageSource1, ImageSource imageSource2)
        {
            if (imageSource1 == null || imageSource2 == null)
            {
                return imageSource1 == imageSource2;
            }

            return imageSource1.ToBytes().SequenceEqual(imageSource2.ToBytes());
        }

        private static byte[] ToBytes(this ImageSource imageSource)
        {
            byte[] bytes = { };
            var image = (BitmapImage) imageSource;
            if (image != null)
            {
                try
                {
                    var encoder = new BmpBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    using (var stream = new MemoryStream())
                    {
                        encoder.Save(stream);
                        bytes = stream.ToArray();
                    }
                }
                catch
                {
                    //suppress any errors
                }
            }
            return bytes;
        }
    }
}
