using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Documents;

using Microsoft.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.DynamoDB.View.Components
{
    public class DataFormatToolBarAdornerPlaceholder : FrameworkElement
    {
        public Adorner Adorner
        {
            get
            {
                Visual current = this;
                while (current != null && !(current is Adorner))
                {
                    current = (Visual)VisualTreeHelper.GetParent(current);
                }

                return (Adorner)current;
            }
        }

        public FrameworkElement AdornedElement
        {
            get
            {
                return Adorner == null ? null : Adorner.AdornedElement as FrameworkElement;
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var controlAdorner = Adorner as DataFormatToolBarAdorner;
            if (controlAdorner != null)
            {
                controlAdorner.Placeholder = this;
                DataGrid grid = AdornedElement as DataGrid;
                EditingState = DataFormatToolBarAdornerHelper.GetEditingStateInfo(grid) as DataFormatToolBarEditingState;
            }

            FrameworkElement e = AdornedElement;
            return new Size(e.ActualWidth, e.ActualHeight);
        }

        public DataFormatToolBarEditingState EditingState { get; protected set; }
    }
}
