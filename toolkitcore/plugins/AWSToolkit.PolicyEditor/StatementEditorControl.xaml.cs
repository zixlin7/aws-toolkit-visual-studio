using System.Windows;

namespace Amazon.AWSToolkit.PolicyEditor
{
    /// <summary>
    /// Interaction logic for StatementEditorControl.xaml
    /// </summary>
    public partial class StatementEditorControl 
    {
        public StatementEditorControl()
        {
            this.DataContextChanged += new DependencyPropertyChangedEventHandler(onDataContextChange);
            InitializeComponent();
        }

        void onDataContextChange(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this._ctlTabContainer.SelectedIndex == -1)
                this._ctlTabContainer.SelectedIndex = 0;
        }

        public void RemovePrincipalTab()
        {
            if (this._ctlTabContainer.Items.Contains(this._ctlPrincipalTab))
                this._ctlTabContainer.Items.Remove(this._ctlPrincipalTab);
        }

        public void OnDrag(object sender, DragEventArgs e)
        {
            var control = getPolicyEditorControl();
            if (control == null)
                return;

            control.OnDrag(sender, e);
        }

        public void OnDrop(object sender, DragEventArgs e)
        {
            var control = getPolicyEditorControl();
            if (control == null)
                return;

            control.OnDrop(sender, e);
        }

        PolicyEditorControl getPolicyEditorControl()
        {
            FrameworkElement parent = this.Parent as FrameworkElement;
            while (parent != null)
            {
                if (parent is PolicyEditorControl)
                {
                    return (PolicyEditorControl)parent;
                }

                parent = parent.Parent as FrameworkElement;
            }

            return null;
        }

    }
}
