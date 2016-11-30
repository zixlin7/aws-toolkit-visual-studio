using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Markup;

namespace Amazon.AWSToolkit.CommonUI.Components
{
    /// <summary>
    /// Renders a ToggleButton like control with drop-down characteristics of a ComboBox; the
    /// dropdown can be set to a custom user control within the boundaries of header (titling)
    /// and footer (cancel/apply) buttons.
    /// </summary>
    [TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
    [TemplatePart(Name = "PART_PopupInner", Type = typeof(UIElement))]
    [TemplatePart(Name = "PART_HeaderPanel", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_ContentPanel", Type = typeof(ContentPresenter))]
    [TemplatePart(Name = "PART_ControlButtonsPanel", Type = typeof(Grid))]
    [TemplatePart(Name = "PART_CancelButton", Type = typeof(Button))]
    [TemplatePart(Name = "PART_ApplyButton", Type = typeof(Button))]
    public class HeaderedDropPanel : ToggleButton
    {
        Popup _popupPart;
        UIElement _popupInnerPart;
        bool _applyChangesOnClose = false;

        static HeaderedDropPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(HeaderedDropPanel), new FrameworkPropertyMetadata(typeof(HeaderedDropPanel)));
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _popupPart = base.GetTemplateChild("PART_Popup") as Popup;
            if (_popupPart != null)
            {
                _popupPart.MouseDown += new MouseButtonEventHandler(_popupParts_MouseDown);
                _popupPart.Closed += new EventHandler(_popupPart_Closed);
            }

            _popupInnerPart = base.GetTemplateChild("PART_PopupInner") as UIElement;
            if (_popupInnerPart != null)
                _popupInnerPart.MouseDown += new MouseButtonEventHandler(_popupParts_MouseDown);

            Button cancelButton = base.GetTemplateChild("PART_CancelButton") as Button;
            if (cancelButton != null)
                cancelButton.Click += new RoutedEventHandler(cancelButton_Click);

            Button applyButton = base.GetTemplateChild("PART_ApplyButton") as Button;
            if (applyButton != null)
                applyButton.Click += new RoutedEventHandler(applyButton_Click);
        }

        // catch-all for clicks on any whitespace area in the popup; if we don't handle this
        // the togglebutton base will get it and close the popup
        void _popupParts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        void _popupPart_Closed(object sender, EventArgs e)
        {
            // have to cache this, if we call IsChecked then it will change
            bool applyChanges = _applyChangesOnClose;

            // avoid cycling checking that causes popup to reappear when
            // we uncheck the toggle button
            if (Mouse.DirectlyOver != this)
                IsChecked = false;

            if (applyChanges)
            {
                RoutedEventArgs args = new RoutedEventArgs(ApplyPressedEvent);
                RaiseEvent(args);
            }
        }

        void applyButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            _applyChangesOnClose = true;
            CloseDropDown();
        }

        void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            _applyChangesOnClose = false;
            e.Handled = true;
            CloseDropDown();
        }

        void CloseDropDown()
        {
            _popupPart.IsOpen = false;
        }

        protected override void OnChecked(RoutedEventArgs e)
        {
            base.OnChecked(e);

            RoutedEventArgs args = new RoutedEventArgs(DropPanelOpeningEvent);
            RaiseEvent(args);

            _applyChangesOnClose = false;
            _popupPart.IsOpen = true;
        }

        protected override void OnUnchecked(RoutedEventArgs e)
        {
            _applyChangesOnClose = true;
            CloseDropDown();
            base.OnUnchecked(e);
        }

        #region Dependency Properties

        public static readonly DependencyProperty DropDownImageProperty 
            = DependencyProperty.Register("DropDownImage", typeof(ImageSource), typeof(HeaderedDropPanel));
        public static readonly DependencyProperty DropDownLabelProperty 
            = DependencyProperty.Register("DropDownLabel", typeof(string), typeof(HeaderedDropPanel));
        public static readonly DependencyProperty PanelHeaderLabelProperty 
            = DependencyProperty.Register("PanelHeaderLabel", typeof(string), typeof(HeaderedDropPanel));

        /// <summary>
        /// Gets/sets the image displayed to the left of the label 
        /// when the control is collapsed
        /// </summary>
        public ImageSource DropDownImage
        {
            get { return (ImageSource)GetValue(DropDownImageProperty); }
            set { SetValue(DropDownImageProperty, value); }
        }

        /// <summary>
        /// Gets/sets the label used when the control is collapsed
        /// </summary>
        public string DropDownLabel
        {
            get { return (string)GetValue(DropDownLabelProperty); }
            set { SetValue(DropDownLabelProperty, value); }
        }

        /// <summary>
        /// Gets/sets the header label used on the expanded panel
        /// </summary>
        public string PanelHeaderLabel
        {
            get { return (string)GetValue(PanelHeaderLabelProperty); }
            set { SetValue(PanelHeaderLabelProperty, value); }
        }

        #endregion

        #region Routed Events

        public static readonly RoutedEvent ApplyPressedEvent
            = EventManager.RegisterRoutedEvent("ApplyPressed",
                                               RoutingStrategy.Bubble,
                                               typeof(RoutedEventHandler),
                                               typeof(HeaderedDropPanel));

        public event RoutedEventHandler ApplyPressed
        {
            add { AddHandler(ApplyPressedEvent, value); }
            remove { RemoveHandler(ApplyPressedEvent, value); }
        }

        public static readonly RoutedEvent DropPanelOpeningEvent
            = EventManager.RegisterRoutedEvent("DropPanelOpening",
                                               RoutingStrategy.Bubble,
                                               typeof(RoutedEventHandler),
                                               typeof(HeaderedDropPanel));

        public event RoutedEventHandler DropPanelOpening
        {
            add { AddHandler(DropPanelOpeningEvent, value); }
            remove { RemoveHandler(DropPanelOpeningEvent, value); }
        }

        #endregion
    }
}
