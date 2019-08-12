using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Amazon.Auth.AccessControlPolicy;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.PolicyEditor.Model;

using log4net;

namespace Amazon.AWSToolkit.PolicyEditor
{
    /// <summary>
    /// Interaction logic for StandalonePolicyEditor.xaml
    /// </summary>
    public partial class StandalonePolicyEditor : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(StandalonePolicyEditor));

        IStandalonePolicyEditorController _controller;
        Policy _policy;
        PolicyModel _model;

        Button _saveBtn;

        public StandalonePolicyEditor(IStandalonePolicyEditorController controller)
        {
            this._controller = controller;
            this._model = createPolicyModel();
            this.DataContext = this._model;

            InitializeComponent();
            this._ctlPolicyEditor.Policy = this._policy;

            AddSaveButton();
        }

        public override string Title => this._controller.Title;

        public override string UniqueId => string.Format("Policy,{0},{1}", this._controller.PolicyMode.ToString(), this._controller.Title);

        PolicyModel createPolicyModel()
        {
            var document = this._controller.GetPolicyDocument();
            if (string.IsNullOrEmpty(document))
                this._policy = new Policy();
            else
                this._policy = Policy.FromJson(document);

            var model = new PolicyModel(this._controller.PolicyMode, this._policy);
            model.OnChange += new EventHandler(onPolicyModelChange);
            return model;
        }

        void onPolicyModelChange(object sender, EventArgs e)
        {
            if (this._saveBtn != null)
            {
                this._saveBtn.IsEnabled = true;
            }
        }

        void AddSaveButton()
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.DecodePixelHeight = 16;
            bi.DecodePixelWidth = 16;
            bi.StreamSource = new CommonIcons().SaveIcon;
            bi.EndInit();

            Image image = new Image();
            image.Source = bi;
            image.Width = image.Height = 16;

            TextBlock block = new TextBlock();
            block.Text = "Save";

            WrapPanel panel = new WrapPanel();
            panel.Children.Add(image);
            panel.Children.Add(block);

            this._saveBtn = new Button();
            this._saveBtn.Content = panel;
            this._saveBtn.Click += new RoutedEventHandler(onSave);
            this._saveBtn.IsEnabled = false;

            this._ctlPolicyEditor.MainToolBar.Items.Insert(0, this._saveBtn);
        }

        void onSave(object sender, RoutedEventArgs e)
        {
            try
            {
                this._controller.SavePolicyDocument(this._policy.ToJson());

                this._saveBtn.IsEnabled = false;
            }
            catch (Exception ex)
            {
                LOGGER.Info("Error saving policy", ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error saving policy: " + ex.Message);
            }
        }
    }
}
