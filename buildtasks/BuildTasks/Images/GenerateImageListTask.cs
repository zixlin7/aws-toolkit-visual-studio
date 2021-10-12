using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;

namespace BuildTasks.Images
{
    /// <summary>
    /// This task takes in a list of images, combines them into a single image list, and saves the image list to a PNG.
    /// Image list is composed of 16x16 images.
    /// Input images are expected to have square dimensions.
    /// </summary>
    public class GenerateImageListTask : BuildTaskBase
    {
        /// <summary>
        /// Full path of images to combine in an image list
        /// </summary>
        /// <remarks>
        /// The paths should be separated using a semi-colon character
        /// (MSBuild will do this for you if you use the @(propertyname) syntax).
        /// </remarks>
        public string ImagePaths { get; set; }

        /// <summary>
        /// Full path to save the generated image list to
        /// </summary>
        public string OutputFilename { get; set; }

        public override bool Execute()
        {
            var imageList = new ImageList(16);

            foreach (var imagePath in GetInputImagePaths())
            {
                imageList.Add(imagePath);
            }

            imageList.Save(OutputFilename, ImageFormat.Png);
            return true;
        }

        private ICollection<string> GetInputImagePaths()
        {
            return ImagePaths
                .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .ToList();
        }
    }
}
