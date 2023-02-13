using System;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Amazon.AWSToolkit.CommonUI.Images
{
    public class ImageSourceFactory
    {
        public static ImageSource GetImageSource(Uri uri)
        {
            return new BitmapImage(uri);
        }

        public static ImageSource GetImageSource(string embeddedName)
        {
            return GetImageSource(Assembly.GetExecutingAssembly(), embeddedName);
        }

        public static ImageSource GetImageSource(Assembly assembly, string embeddedName)
        {
            if (string.IsNullOrEmpty(embeddedName))
            {
                return null;
            }

            assembly = assembly ?? Assembly.GetExecutingAssembly();

            Stream stream = assembly.GetManifestResourceStream(embeddedName);
            if (stream == null)
            {
                return null;
            }    

            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            return bitmap;
        }
    }
}
