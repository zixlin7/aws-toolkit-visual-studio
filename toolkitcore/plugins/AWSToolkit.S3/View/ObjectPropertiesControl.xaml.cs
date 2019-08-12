using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Controller;

namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for ObjectPropertiesControl.xaml
    /// </summary>
    public partial class ObjectPropertiesControl : BaseAWSControl
    {
        ObjectPropertiesController _controller;
        ObjectPropertiesModel _model;

        public ObjectPropertiesControl()
            : this(null)
        {
        }

        public ObjectPropertiesControl(ObjectPropertiesController controller)
        {
            this._controller = controller;
            this._model = controller.Model;

            InitializeComponent();

            this._ctlFileIcon.DataContext = this;
            this._ctlAccessIcon.DataContext = this;
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            base.NotifyPropertyChanged("FileIcon");
            base.NotifyPropertyChanged("AccessIcon");
            return this._controller.Model;
        }

        protected override void PostDataContextBound()
        {
            if (this._controller.Model.StoredInGlacier ||
                this._controller.Model.UsesKMSServerSideEncryption ||
                this._controller.Model.ErrorRetrievingMetadata)
            {
                this._ctlStorageClass.IsEnabled = false;
                this._cllServerSideEncryption.IsEnabled = false;
                this._ctlMetadata.IsEnabled = false;
                this._ctlPermissions.IsEnabled = false;


                if (this._controller.Model.StoredInGlacier)
                {
                    this._ctlStorageClass.Content = "Stored in Amazon Glacier";

                    if (!string.IsNullOrEmpty(this._controller.Model.RestoreInfo))
                    {
                        this._ctlRestoreInfo.Text = this._controller.Model.RestoreInfo;
                        this._ctlRestoreInfo.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }

        public ObjectPropertiesModel Model => this._model;

        public object FileIcon
        {
            get
            {
                Image image = IconHelper.GetIconByExtension(this.Model.Key);

                // If no image found the use generic file icon.
                if(image == null)
                    return Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.generic-file.png");

                return image.Source;
            }
        }

        public Stream AccessIcon
        {
            get
            {
                Stream stream;
                if (this._model.IsPublic)
                {
                    stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.S3.Resources.EmbeddedImages.make-public.png");
                }
                else
                {
                    stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.S3.Resources.EmbeddedImages.private.png");
                }
                return stream;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                string url = this._controller.GetPreSignedURL();
                Process.Start(new ProcessStartInfo(url));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to object: " + ex.Message);
            }
        }

        public override string Title => string.Format("Properties: {0}", this._model.Name);

        public override string UniqueId => string.Format("ObjectProperties:{0}:{1}", this._model.BucketName, this._model.Key);

        public override bool OnCommit()
        {
            if (this._controller.Model.StoredInGlacier)
                return true;

            this._controller.Persist();
            return true;
        }
    }
}
