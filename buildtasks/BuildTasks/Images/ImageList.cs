using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace BuildTasks.Images
{
    public class ImageList
    {
        private readonly int _expectedHeightAndWidth;
        private readonly List<Image> _bitmaps = new List<Image>();

        public ImageList(int expectedHeightAndWidth)
        {
            _expectedHeightAndWidth = expectedHeightAndWidth;
        }

        public void Add(string path)
        {
            var bmp = new Bitmap(path);
            Add(bmp);
        }

        public void Add(Image image)
        {
            if (image.Height != image.Width)
            {
                throw new Exception("Image must start with square dimensions");
            }
            _bitmaps.Add(Resize(image));
        }

        private Image Resize(Image image)
        {
            if (image.Height == _expectedHeightAndWidth && image.Width == _expectedHeightAndWidth)
            {
                return image;
            }

            Bitmap resizedBitmap = new Bitmap(_expectedHeightAndWidth, _expectedHeightAndWidth);
            using (Graphics g = Graphics.FromImage(resizedBitmap))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, 0, 0, _expectedHeightAndWidth, _expectedHeightAndWidth);
            }

            return resizedBitmap;
        }

        public void Save(string filename, ImageFormat imageFormat)
        {
            var imageList = GenerateBitmapList();
            SaveImage(imageList, filename,  imageFormat);
        }

        private Image GenerateBitmapList()
        {
            Bitmap bitmap = new Bitmap(_bitmaps.Count * _expectedHeightAndWidth, _expectedHeightAndWidth);

            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;

                int x = 0;
                foreach (var subImage in _bitmaps)
                {
                    graphics.DrawImage(subImage, x, 0);
                    x += _expectedHeightAndWidth;
                }
            }

            return bitmap;
        }

        private void SaveImage(Image imageList, string filename, ImageFormat imageFormat)
        {
            imageList.Save(filename, imageFormat);
        }
    }
}
