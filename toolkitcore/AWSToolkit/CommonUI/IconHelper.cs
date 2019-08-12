using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using IconImage=System.Drawing.Icon;
using Amazon.AWSToolkit.CommonUI.Private;

namespace Amazon.AWSToolkit.CommonUI
{
    public static class IconHelper
    {
        public static Image GetIcon(string embeddedName)
        {
            return GetIcon(Assembly.GetExecutingAssembly(), embeddedName);
        }

        public static Image GetIcon(string embeddedName, int width, int height)
        {
            return GetIcon(Assembly.GetExecutingAssembly(), embeddedName, width, height);
        }

        public static Image GetIcon(Assembly assembly, string embeddedName)
        {
            return GetIcon(assembly, embeddedName, 16, 16);
        }

        public static Image GetIcon(Assembly assembly, string embeddedName, int width, int height)
        {
            if (assembly == null)
                assembly = Assembly.GetExecutingAssembly();

            if (string.IsNullOrEmpty(embeddedName))
                return null;

            if (!embeddedName.StartsWith("Amazon.AWSToolkit."))
                embeddedName = "Amazon.AWSToolkit.Resources." + embeddedName;

            Stream stream = assembly.GetManifestResourceStream(embeddedName);
            if (stream == null)
                return null;

            BitmapImage map = new BitmapImage();
            map.BeginInit();
            map.StreamSource = stream;
            map.EndInit();
            Image image = new Image() { Source = map, Width = width, Height = height };
            return image;
        }

        public static Image GetIcon(Stream stream)
        {
            return GetIcon(stream, 16, 16);
        }

        public static Image GetIcon(Stream stream, int width, int height)
        {
            var map = new BitmapImage();
            map.BeginInit();
            map.StreamSource = stream;
            map.EndInit();
            var image = new Image() { Source = map, Width = width, Height = height };
            return image;
        }

        static readonly Dictionary<string, Image> _cachedIcons = new Dictionary<string, Image>();
        public static Image GetIconByExtension(string filepath)
        {
            var extension = getExtension(filepath);
            Image image = null;
            if (!_cachedIcons.TryGetValue(extension, out image))
            {
                var ico = __IconHelper.IconFromExtensionShell(extension, __IconHelper.SystemIconSize.Small);
                var stream = new MemoryStream();

                var bmp = ico.ToBitmap();
                bmp.Save(stream, ImageFormat.Png); // use png format to carry transparency info onwards
                
                var map = new BitmapImage();
                map.BeginInit();
                map.StreamSource = stream;
                map.EndInit();

                image = new Image { Source = map, Width = 16, Height = 16 };

                _cachedIcons[extension] = image;
            }

            return image;
        }

        private static string getExtension(string filepath)
        {
            int pos = filepath.LastIndexOf('/');
            string filename;
            if(pos >= 0)
            {
                filename = filepath.Substring(pos + 1);
            }
            else
            {
                filename = filepath;
            }

            pos = filename.LastIndexOf('.');
            string extention;
            if(pos >= 0)
            {
                extention = filename.Substring(pos);
            }
            else
            {
                extention = filename;
            }

            return extention;
        }
    }
}
