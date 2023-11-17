using System.Reflection;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI.Images;

namespace Amazon.AwsToolkit.CodeWhisperer.Resources
{
    /// <summary>
    /// Toolkit abstraction around custom Aws images that are used across the toolkit
    /// </summary>
    public class CodeWhispererImages
    {
        private static readonly Assembly _assembly = typeof(CodeWhispererImages).Assembly;

        private static class EmbeddedResourceNames
        {
            public const string Disconnected32 =
                "Amazon.AwsToolkit.CodeWhisperer.Resources.MarginStatus.disconnected-32.png";
            public const string Error32 = "Amazon.AwsToolkit.CodeWhisperer.Resources.MarginStatus.error-32.png";
            public const string Paused32 = "Amazon.AwsToolkit.CodeWhisperer.Resources.MarginStatus.pause-32.png";
        }

        public static ImageSource Disconnected32 => GetImageSource(EmbeddedResourceNames.Disconnected32);
        public static ImageSource Error32 => GetImageSource(EmbeddedResourceNames.Error32);
        public static ImageSource Paused32 => GetImageSource(EmbeddedResourceNames.Paused32);

        public static ImageSource GetImageSource(string embeddedResourceName)
        {
            return ImageSourceFactory.GetImageSource(_assembly, embeddedResourceName);
        }
    }
}
