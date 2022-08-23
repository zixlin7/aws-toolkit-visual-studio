using System;
using System.Windows;
using System.Windows.Media;

using Microsoft.TeamFoundation.Controls.WPF.TeamExplorer;

namespace Amazon.AWSToolkit.CodeCommitTeamExplorer.Base
{
    public class TeamExplorerInvitationBase : TeamExplorerServiceInvitationBase
    {
        public const string TeamExplorerInvitationSectionId = "C2443FCC-6D62-4D31-B08A-C4DE70109C7F";

        public override void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected static DrawingBrush CreateDrawingBrush(ImageSource imageSource)
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
