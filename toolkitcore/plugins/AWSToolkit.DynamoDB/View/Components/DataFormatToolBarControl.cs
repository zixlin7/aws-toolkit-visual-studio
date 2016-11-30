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

using Amazon.AWSToolkit.CommonUI;
using Microsoft.Windows.Controls;

namespace Amazon.AWSToolkit.DynamoDB.View.Components
{
    /// <summary>
    /// </summary>
    [TemplatePart(Name = "PART_FormatToolBar", Type = typeof(ToolBar))]
    public class DataFormatToolBarControl : Control
    {
        ToolBar _formatToolBar;
        ToolBar _editToolBar;
        RadioButton _btnAsString, _btnAsNumber, _btnAsStringSet, _btnAsNumberSet;

        static DataFormatToolBarControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataFormatToolBarControl), new FrameworkPropertyMetadata(typeof(DataFormatToolBarControl)));
        }

        [Flags]
        public enum CellDataFormatType
        {
            Indeterminate   = 0,
            IsString        = 0x00000001,
            IsNumber        = 0x00000002,
            IsSet           = 0x00010000
        }

        public CellDataFormatType CellDataFormat
        {
            get
            {
                if (_btnAsString != null && _btnAsString.IsChecked == true)
                    return CellDataFormatType.IsString;

                if (_btnAsStringSet != null && _btnAsStringSet.IsChecked == true)
                    return CellDataFormatType.IsString | CellDataFormatType.IsSet;

                if (_btnAsNumber != null && _btnAsNumber.IsChecked == true)
                    return CellDataFormatType.IsNumber;

                if (_btnAsNumberSet != null && _btnAsNumberSet.IsChecked == true)
                    return CellDataFormatType.IsNumber | CellDataFormatType.IsSet;

                return CellDataFormatType.Indeterminate;
            }
            set
            {
                bool isNumeric = (value & CellDataFormatType.IsNumber) == CellDataFormatType.IsNumber;
                bool isSet = (value & CellDataFormatType.IsSet) == CellDataFormatType.IsSet;

                if (!isNumeric && !isSet)
                    this._btnAsString.IsChecked = true;
                else if (!isNumeric && isSet)
                    this._btnAsStringSet.IsChecked = true;
                else if (isNumeric && !isSet)
                    this._btnAsNumber.IsChecked = true;
                else if (isNumeric && isSet)
                    this._btnAsNumberSet.IsChecked = true;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _formatToolBar = base.GetTemplateChild("PART_FormatToolBar") as ToolBar;
            // too early to find overflow grid area, have to wait until Loaded fires
            if (_formatToolBar != null)
                _formatToolBar.Loaded += new RoutedEventHandler(toolbar_Loaded);
            _editToolBar = base.GetTemplateChild("PART_EditToolBar") as ToolBar;
            if (_editToolBar != null)
                _editToolBar.Loaded += new RoutedEventHandler(toolbar_Loaded);

            _btnAsString = base.GetTemplateChild("PART_AsStringButton") as RadioButton;
            if (_btnAsString != null)
                _btnAsString.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _btnAsNumber = base.GetTemplateChild("PART_AsNumberButton") as RadioButton;
            if (_btnAsNumber != null)
                _btnAsNumber.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _btnAsStringSet = base.GetTemplateChild("PART_AsStringSetButton") as RadioButton;
            if (_btnAsStringSet != null)
                _btnAsStringSet.Checked += new RoutedEventHandler(btnFormatType_Checked);

            _btnAsNumberSet = base.GetTemplateChild("PART_AsNumberSetButton") as RadioButton;
            if (_btnAsNumberSet != null)
                _btnAsNumberSet.Checked += new RoutedEventHandler(btnFormatType_Checked);
        }

        void toolbar_Loaded(object sender, RoutedEventArgs e)
        {
            ToolBar toolbar = sender as ToolBar;
            if (toolbar == null) return;

            UIUtils.HideToolbarOverflowGrid(toolbar);
            // inner border also has an 11 pixel (by default) right margin before overflow grid
            var mainPanelBorder = toolbar.Template.FindName("MainPanelBorder", toolbar) as FrameworkElement;
            if (mainPanelBorder != null)
                mainPanelBorder.Margin = new Thickness(0);

            toolbar.Loaded -= new RoutedEventHandler(toolbar_Loaded);
        }

        void btnFormatType_Checked(object sender, RoutedEventArgs e)
        {
            RoutedEventArgs args = new RoutedEventArgs(DataFormatChangedEvent);
            RaiseEvent(args);
        }

        #region Routed Events

        public static readonly RoutedEvent DataFormatChangedEvent
            = EventManager.RegisterRoutedEvent("DataFormatChanged",
                                               RoutingStrategy.Bubble,
                                               typeof(RoutedEventHandler),
                                               typeof(DataFormatToolBarControl));

        public event RoutedEventHandler DataFormatChanged
        {
            add { AddHandler(DataFormatChangedEvent, value); }
            remove { RemoveHandler(DataFormatChangedEvent, value); }
        }

        #endregion
    }

    /// <summary>
    /// State object attached to the grid when editing state raised via the 
    /// DataFormatToolBarAdornerHelper.AdornerStateObject dependency property
    /// </summary>
    public class DataFormatToolBarEditingState
    {
        /// <summary>
        /// The cell on which edit state was raised
        /// </summary>
        public DataGridCell Cell { get; set; }

        /// <summary>
        /// The format of the current data in the cell
        /// </summary>
        public DataFormatToolBarControl.CellDataFormatType CellDataFormat { get; set; }
    }
}
