﻿using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

        public static DrawingBrush CreateDrawingBrush(ImageSource imageSource)
        {
            var drawing = new DrawingGroup();
            drawing.Children.Add(new GeometryDrawing
            {
                Brush = new ImageBrush(imageSource),
                Geometry = new RectangleGeometry(new Rect(new Size(imageSource.Width, imageSource.Height)))
            });

            return new DrawingBrush(drawing);
        }
    }
}
