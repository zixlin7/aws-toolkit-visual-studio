using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.Images
{
    /// <summary>
    /// A custom control to render images served by Visual Studio SDK in the toolkit
    /// </summary>
    public partial class VsImage : UserControl
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(VsImage));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                nameof(Source), typeof(BitmapSource), typeof(VsImage));

        public BitmapSource Source
        {
            get => (BitmapSource) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private static IMultiValueConverter _imageThemeConverter;

        public static void Initialize(IMultiValueConverter imageThemeConverter)
        {
            _imageThemeConverter = imageThemeConverter;
        }

        public VsImage()
        {
            InitializeComponent();
            Loaded += VsImage_Loaded;
            Unloaded += VsImage_Unloaded;

            BindSource();
        }

        private void VsImage_Loaded(object sender, RoutedEventArgs e)
        {
            ThemeUtil.ThemeChange += ThemeUtil_ThemeChange;
        }

        private void VsImage_Unloaded(object sender, RoutedEventArgs e)
        {
            ThemeUtil.ThemeChange -= ThemeUtil_ThemeChange;
        }

        private void ThemeUtil_ThemeChange(object sender, EventArgs e)
        {
            // Hack
            // UIs that are using ToolkitThemes.UseToolkitTheme will automatically re-apply their
            // theme-appropriate image, due to the bindings that use the parent control Background property.
            // UIs that use the older Generic.xaml resource dictionary, and keys like awsToolWindowBackgroundBrushKey
            // do not raise events when the theme changes. We listen for the theme change event, and re-create the
            // binding as a way of forcing the image to be re-calculated using the new theme.
            BindSource();
        }

        private void BindSource()
        {
            if (_imageThemeConverter == null)
            {
                Logger.Error("VsImage created before theme converter was initialized. Image will be empty.");
                return;
            }

            var binding = CreateImageSourceBinding();
            VsImageControl.SetBinding(Image.SourceProperty, binding);
        }

        private MultiBinding CreateImageSourceBinding()
        {
            var binding = new MultiBinding { Converter = _imageThemeConverter };

            var vsImageRelSrc = new RelativeSource(RelativeSourceMode.FindAncestor)
            {
                AncestorType = typeof(VsImage)
            };

            // Binding 1: VsImage Source property - indicates what image should be shown
            binding.Bindings.Add(new Binding(nameof(Source))
            {
                RelativeSource = vsImageRelSrc,
            });

            // Binding 2: VsImage object - used to get the parent control's background
            binding.Bindings.Add(new Binding(".")
            {
                RelativeSource = vsImageRelSrc,
            });

            // Binding 3: Whether or not the image is visible - used to update the binding whenever this changes (changing the theme for example)
            binding.Bindings.Add(new Binding(nameof(IsVisible))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
            });

            // Binding 4: Background of the VsImage's parent control - used to update the binding whenever this changes (changing the theme for example)
            binding.Bindings.Add(new Binding(nameof(Background))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                {
                    AncestorType = typeof(Control),
                    AncestorLevel = 2,
                },
            });

            // Binding 5: Background of the VsImage's grand-parent control - used to update the binding whenever this changes (changing the theme for example)
            // This is necessary for scenarios where a RadioButton contains an image
            binding.Bindings.Add(new Binding(nameof(Background))
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
                {
                    AncestorType = typeof(Control),
                    AncestorLevel = 3,
                },
            });

            return binding;
        }
    }
}
