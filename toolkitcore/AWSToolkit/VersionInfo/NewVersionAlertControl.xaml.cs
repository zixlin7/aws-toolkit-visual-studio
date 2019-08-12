using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.VersionInfo
{
    /// <summary>
    /// Interaction logic for NewVersionAlertControl.xaml
    /// </summary>
    public partial class NewVersionAlertControl : BaseAWSControl
    {
        IList<VersionManager.Version> _versions;
        string _newUpdateLocation;
        bool _doNotRemindMeAgain = false;

        public NewVersionAlertControl(IList<VersionManager.Version> versions, string newUpdateLocation)
        {
            this._versions = versions;
            this._newUpdateLocation = newUpdateLocation;
            InitializeComponent();
            this._ctlDoNotRemindMe.DataContext = this;
            buildDocument();
        }

        public bool DoNotRemindMeAgain
        {
            get => this._doNotRemindMeAgain;
            set => this._doNotRemindMeAgain = value;
        }

        public override string Title => "Update to AWS Toolkit for Visual Studio";

        public override string MetricId => this.GetType().FullName;

        void buildDocument()
        {
            foreach (var version in this._versions)
            {
                if (Constants.VERSION_NUMBER.Equals(version.Number))
                    break;


                Paragraph para = new Paragraph();
                para.Inlines.Add(string.Format("{0} was released on {1}.", version.Number, version.ReleaseDate.ToLongDateString()));

                List changes = new List();
                foreach (string change in version.Changes)
                {
                    changes.ListItems.Add(new ListItem(new Paragraph(new Run(change))));
                }
                this._ctlVersionInfo.Blocks.Add(para);
                this._ctlVersionInfo.Blocks.Add(changes);
            }
        }

        void onReleaseLinkClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(this._newUpdateLocation));
            e.Handled = true;
        }
    }
}
