using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Amazon.AWSToolkit.CommonUI.Images
{
    /// <summary>
    /// A custom control to render images served by Visual Studio SDK in the toolkit
    /// </summary>
    public partial class VsImage : UserControl
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(
                "Source", typeof(BitmapSource), typeof(VsImage));

        private static IMultiValueConverter _imageThemeConverter;
        private static IValueConverter _brushToColorConverter;

        public static void Initialize(IMultiValueConverter imageThemeConverter, IValueConverter brushToColorConverter)
        {
            _imageThemeConverter = imageThemeConverter;
            _brushToColorConverter = brushToColorConverter;
        }

        public VsImage()
        {
            InitializeComponent();
            DataContext = this;

            var binding = CreateImageSourceBinding();
            this.VsImageControl.SetBinding(Image.SourceProperty, binding);
        }

        public BitmapSource Source
        {
            get => (BitmapSource) GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        private MultiBinding CreateImageSourceBinding()
        {
            var backgroundRelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor)
            {
                AncestorLevel = 4,
                AncestorType = typeof(Control)
            };

            var binding = new MultiBinding { Converter = _imageThemeConverter };
            binding.Bindings.Add(new Binding(nameof(Source)));
            binding.Bindings.Add(new Binding("Background")
            {
                RelativeSource = backgroundRelativeSource,
                Converter = _brushToColorConverter
            });

            return binding;
        }
    }
}
