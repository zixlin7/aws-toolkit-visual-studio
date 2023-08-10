using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Amazon.AWSToolkit.CommonUI.Images;

using log4net;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Represents a help tooltip with the ability to surface a hyperlink
    /// </summary>
    [TemplatePart(Name = _toolTipPopupPartName, Type = typeof(Popup))]
    [TemplatePart(Name = _helpImagePartName, Type = typeof(VsImage))]
    public partial class HelpHyperlinkPopup : ContentControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(HelpHyperlinkPopup));

        protected const string _toolTipPopupPartName = "PART_ToolTipPopup";

        protected const string _helpImagePartName = "PART_HelpImage";

        protected Popup _toolTipPopup { get; set; }

        protected VsImage _helpImage { get; set; }

        public HelpHyperlinkPopup()
        {
            InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _toolTipPopup = GetTemplateChild<Popup>(_toolTipPopupPartName);
            _helpImage = GetTemplateChild<VsImage>(_helpImagePartName);
        }

        private T GetTemplateChild<T>(string name) where T : DependencyObject
        {
            var child = GetTemplateChild(name) as T;
            if (child == null)
            {
                _logger.Error($"Cannot find templated child {name} of type {typeof(T)}.");
            }

            return child;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            _toolTipPopup.IsOpen = true;
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (!_toolTipPopup.IsMouseOver && !_helpImage.IsMouseOver)
            {
                _toolTipPopup.IsOpen = false;
            }
        }
    }
}
