using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using Microsoft.Windows.Controls;

namespace Amazon.AWSToolkit.DynamoDB.View.Components
{
    public class DataFormatToolBarAdorner : Adorner
    {
        public DataFormatToolBarAdornerPlaceholder Placeholder { get; set; }

        // this is the actual control buried inside the adorner layer, ie the grid of toolbar(s)
        DataFormatToolBarControl _innerControl = null;
        DataFormatToolBarControl.CellDataFormatType _initialCellDataFormat;

        public DataFormatToolBarAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
        }

        protected override int VisualChildrenCount
        {
            get
            {
                return 1;
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException();
            return _child;
        }

        private Control _child;
        public Control Child
        {
            get { return _child; }
            set
            {
                if (_child != null)
                {
                    RemoveVisualChild(_child);
                }
                _child = value;
                if (_child != null)
                {
                    AddVisualChild(_child);
                }
            }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        public static FrameworkElement FindVisual(Visual startVisual, string controlPartName)
        {
            FrameworkElement foundElement = null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(startVisual) && foundElement == null; i++)
            {
                Visual childVisual = (Visual)VisualTreeHelper.GetChild(startVisual, i);
                FrameworkElement element = childVisual as FrameworkElement;
                if (element != null && element.Name == controlPartName)
                    foundElement = element;
                else
                    foundElement = FindVisual(childVisual, controlPartName);
            }

            return foundElement;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Point placementPoint;
            if (Placeholder != null)
            {
                // position the tool panel intelligently alongside the cell being edited
                DataGridCell activeCell = Placeholder.EditingState.Cell;
                placementPoint = PositionRelativeToCell(ToolBarControl, activeCell);

            }
            else
                placementPoint = new Point(0, 0);

            Child.Arrange(new Rect(placementPoint, finalSize));
            return new Size(Child.ActualWidth, Child.ActualHeight);
        }

        private DataFormatToolBarControl ToolBarControl
        {
            get
            {
                if (_innerControl == null)
                {
                    _innerControl = FindVisual(Child, "_dataFormatToolBarControl") as DataFormatToolBarControl;

                    if (this._innerControl != null)
                    {
                        this._innerControl.CellDataFormat = Placeholder.EditingState.CellDataFormat;
                    }
                }

                return _innerControl;
            }
        }

        /// <summary>
        /// Inspects the dimensions of the true control being overlaid in the adorner and
        /// sets a display location relative to the grid cell so that the adorner control
        /// is fully visible
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        Point PositionRelativeToCell(FrameworkElement adornerControlChild, DataGridCell cell)
        {
            double verticalOffset = 0; 
            double horizontalOffset = 0;

            double testX = cell.ActualWidth + horizontalOffset;
            double testY = 0; 
            Point pt;

            if (adornerControlChild != null)
            {
                if (adornerControlChild.ActualHeight > 0)
                {
                    pt = cell.TranslatePoint(new Point(0, testY + adornerControlChild.ActualHeight), Placeholder.AdornedElement);
                    if (pt.Y > Placeholder.AdornedElement.ActualHeight)
                    {
                        // shift to be above the cell, if possible
                        testY = -adornerControlChild.ActualHeight - verticalOffset;
                        pt = cell.TranslatePoint(new Point(0, testY), Placeholder.AdornedElement);
                        if (pt.Y < 0)
                            testY = 0;
                    }
                }

                if (adornerControlChild.ActualWidth > 0)
                {
                    pt = cell.TranslatePoint(new Point(testX + adornerControlChild.ActualWidth, 0), Placeholder.AdornedElement);
                    if (pt.X > Placeholder.AdornedElement.ActualWidth)
                    {
                        // shift to be leftwards of the cell, if possible
                        testX = -adornerControlChild.ActualWidth - horizontalOffset;
                        pt = cell.TranslatePoint(new Point(testX, 0), Placeholder.AdornedElement);
                        if (pt.X < 0)
                            testX = 0;
                    }
                }
            }

            return cell.TranslatePoint(new Point(testX, testY), Placeholder.AdornedElement);
        }
    }
}
