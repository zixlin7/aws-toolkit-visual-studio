using System.Windows;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework.PageComponents
{
    /// <summary>
    /// Interaction logic for ReviewPanelsContainer.xaml
    /// </summary>
    public partial class ReviewPanelsContainer
    {
        public ReviewPanelsContainer()
        {
            InitializeComponent();
        }

        public void AddReviewPanel(string sectionHeader, FrameworkElement reviewContent)
        {
            _reviewPanelsStack.Children.Add(new ReviewPanel
            {
                PanelHeader = sectionHeader, 
                PanelContent = reviewContent
            });
        }

        public void ClearPanels()
        {
            _reviewPanelsStack.Children.Clear();
        }
    }
}
