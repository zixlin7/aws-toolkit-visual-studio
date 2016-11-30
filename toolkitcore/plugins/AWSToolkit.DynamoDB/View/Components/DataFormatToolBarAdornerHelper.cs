using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Microsoft.Windows.Controls;

namespace Amazon.AWSToolkit.DynamoDB.View.Components
{
    public class DataFormatToolBarAdornerHelper
    {
        /// <summary>
        /// Dependency property allows grid designer to set the desired editing template
        /// </summary>
        public static readonly DependencyProperty AdornerControlTemplateProperty =
            DependencyProperty.RegisterAttached("AdornerControlTemplate", typeof(ControlTemplate), typeof(DataFormatToolBarAdornerHelper),
            new PropertyMetadata(AdornerControlTemplateChanged));

        public static ControlTemplate GetAdornerControlTemplate(UIElement target)
        {
            return (ControlTemplate)target.GetValue(AdornerControlTemplateProperty);
        }

        public static void SetAdornerControlTemplate(UIElement target, ControlTemplate value)
        {
            target.SetValue(AdornerControlTemplateProperty, value);
        }

        /// <summary>
        /// Allows the adorner control template to be changed at runtime, if necessary
        /// </summary>
        /// <param name="d"></param>
        /// <param name="e"></param>
        private static void AdornerControlTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateAdorner((UIElement)d, GetIsVisible((UIElement)d), (ControlTemplate)e.NewValue);
        }

        /// <summary>
        /// 'Internal' dependency property allows the adorner to be toggled
        /// </summary>
        public static readonly DependencyProperty IsVisibleProperty =
            DependencyProperty.RegisterAttached("IsVisible", typeof(bool), typeof(DataFormatToolBarAdornerHelper),
            new PropertyMetadata(IsVisibleChanged));

        public static bool GetIsVisible(UIElement target)
        {
            return (bool)target.GetValue(IsVisibleProperty);
        }

        public static void SetIsVisible(UIElement target, bool value)
        {
            SetIsVisible(target, value, null);
        }

        public static void SetIsVisible(UIElement target, bool value, object state)
        {
            target.SetValue(EditingStateInfoProperty, state);
            target.SetValue(IsVisibleProperty, value);
        }

        private static void IsVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UpdateAdorner((UIElement)d, (bool)e.NewValue, GetAdornerControlTemplate((UIElement)d));
        }

        /// <summary>
        /// Existing adorner tracks which adorner, if any, is currently in place for a given element
        /// </summary>
        public static readonly DependencyProperty ExistingAdornerProperty =
            DependencyProperty.RegisterAttached("ExistingAdorner", typeof(DataFormatToolBarAdorner), typeof(DataFormatToolBarAdornerHelper));

        public static DataFormatToolBarAdorner GetExistingAdorner(DependencyObject target)
        {
            return (DataFormatToolBarAdorner)target.GetValue(ExistingAdornerProperty);
        }

        public static void SetExistingAdorner(DependencyObject target, DataFormatToolBarAdorner value)
        {
            target.SetValue(ExistingAdornerProperty, value);
        }

        /// <summary>
        /// Shows or hides the adorner and child control based on designated template
        /// </summary>
        /// <param name="adorned"></param>
        private static void UpdateAdorner(UIElement adorned)
        {
            UpdateAdorner(adorned, GetIsVisible(adorned), GetAdornerControlTemplate(adorned));
        }

        private static void UpdateAdorner(UIElement adorned, bool isVisible, ControlTemplate controlTemplate)
        {
            var layer = AdornerLayer.GetAdornerLayer(adorned);

            if (layer == null)
            {
                // if we don't have an adorner layer it's probably
                // because it's too early in the window's construction
                // Let's re-run at a slightly later time
                Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Loaded,
                                                         new Action<UIElement>(o => UpdateAdorner(o)),
                                                         adorned);
                return;
            }

            var existingAdorner = GetExistingAdorner(adorned);
            if (existingAdorner == null)
            {
                if (controlTemplate != null && isVisible)
                {
                    // show
                    var newAdorner = new DataFormatToolBarAdorner(adorned);
                    newAdorner.Child = new Control() { Template = controlTemplate, Focusable = false, };
                    layer.Add(newAdorner);
                    SetExistingAdorner(adorned, newAdorner);
                }
            }
            else
            {
                if (controlTemplate != null && isVisible)
                {
                    // switch template
                    Control ctrl = existingAdorner.Child;
                    ctrl.Template = controlTemplate;
                }
                else
                {
                    // hide
                    existingAdorner.Child = null;
                    layer.Remove(existingAdorner);
                    SetExistingAdorner(adorned, null);
                }
            }
        }

        /// <summary>
        /// 'Internal' dependency property used to pass an optional state object to the control raised
        /// within the adorner
        /// </summary>
        public static readonly DependencyProperty EditingStateInfoProperty =
            DependencyProperty.RegisterAttached("EditingStateInfo", typeof(DataFormatToolBarEditingState), typeof(DataFormatToolBarAdornerHelper));

        public static DataFormatToolBarEditingState GetEditingStateInfo(DependencyObject target)
        {
            return (DataFormatToolBarEditingState)target.GetValue(EditingStateInfoProperty);
        }

        public static void SetEditingStateInfo(DependencyObject target, DataFormatToolBarEditingState value)
        {
            target.SetValue(EditingStateInfoProperty, value);
        }
    
    }
}
