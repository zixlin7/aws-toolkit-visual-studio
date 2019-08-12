namespace Amazon.AWSToolkit.VisualStudio.ToolWindow
{
    /// <summary>
    /// Interaction logic for AWSNavigatorToolControl
    /// </summary>
    public partial class AWSNavigatorToolControl
    {
        public AWSNavigatorToolControl()
        {
            InitializeComponent();

            ThemeUtil.UpdateDictionariesForTheme(this.Resources);

            this._navigatorHost.Children.Add(ToolkitFactory.Instance.Navigator);
        }
    }
}