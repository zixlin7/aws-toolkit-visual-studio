using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Amazon.AWSToolkit.CodeCommit.Interface;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.Base
{
    /// <summary>
    /// Common base functionality for integrating our CodeCommit support
    /// with various TeamExplorer extension points.
    /// </summary>
    public abstract class TeamExplorerBase : INotifyPropertyChanged, IDisposable
    {
        protected IServiceProvider TeamExplorerServiceProvider { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static DrawingBrush LoadSectionIcon(string resourceSubpath)
        {
            var resourceUrl = string.Format("pack://application:,,,/{0};component/{1}",
                                            Assembly.GetExecutingAssembly().GetName().Name,
                                            resourceSubpath);
            var image = new BitmapImage(new Uri(resourceUrl, UriKind.Absolute));

            var drawing = new DrawingGroup();
            drawing.Children.Add(new GeometryDrawing
            {
                Brush = new ImageBrush(image),
                Geometry = new RectangleGeometry(new Rect(new Size(image.Width, image.Height)))
            });

            return new DrawingBrush(drawing);
        }
    }
}
