using System;
using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted
{
    /// <summary>
    /// Interaction logic for GettingStartedView.xaml
    /// </summary>
    public partial class GettingStartedView : BaseAWSControl
    {
        public override string Title => "AWS Getting Started";

        public override string UniqueId => "AWSGettingStarted";

        public override bool IsUniquePerAccountAndRegion => false;

        public GettingStartedView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Prevents any click events in the TextBlock from reaching the parent CheckBox
        /// </summary>
        private void TextBlock_SwallowEvents(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}
