using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.SNS.Nodes;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.CloudFormation.Model;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for SelectTemplatePage.xaml
    /// </summary>
    public partial class SelectTemplatePage
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(SelectTemplatePage));

        CloudFormationRootViewModel _cloudFormationRootModel;
        public SelectTemplatePage(CloudFormationRootViewModel cloudFormationRootModel)
        {
            this._cloudFormationRootModel = cloudFormationRootModel;
            InitializeComponent();
            loadTopicList();

            this._ctlStackName.TextChanged += onStackNameTextChanged;
        }

        void onStackNameTextChanged(object sender, TextChangedEventArgs e)
        {
            string name = this._ctlStackName.Text;

            if (!SelectTemplateModel.IsValidStackName(name))
            {
                this._nameValidatedMsg.Visibility = Visibility.Visible;
                return;
            }

            this._nameValidatedMsg.Visibility = Visibility.Hidden;
        }

        private void onBrowseTemplateClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Select Template";
            dlg.CheckPathExists = true;
            dlg.Multiselect = false;

            if (!dlg.ShowDialog().GetValueOrDefault())
                return;

            CloudFormationTemplateWrapper wrapper = null;
            try
            {
                wrapper = CloudFormationTemplateWrapper.FromLocalFile(dlg.FileName);
                wrapper.LoadAndParse();
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to parse CloudFormation Template: " + dlg.FileName, ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("File is not a valid CloudFormation template");
                setFileToModel(null);
                return;
            }

            try
            {
                var request = new ValidateTemplateRequest { TemplateBody = wrapper.TemplateContent };
                this._cloudFormationRootModel.CloudFormationClient.ValidateTemplate(request);
            }
            catch (Exception ex)
            {
                LOGGER.Error("Failed to parse CloudFormation Template: " + dlg.FileName, ex);
                ToolkitFactory.Instance.ShellProvider.ShowError("File is not a valid CloudFormation template: " + ex.Message);
                setFileToModel(null);
                return;
            }

            setFileToModel(dlg.FileName);
        }

        void setFileToModel(string filename)
        {
            var model = this.DataContext as SelectTemplateModel;
            model.LocalFile = filename;
            this._ctlLocalFile.ToolTip = filename;

            if (filename != null)
                this._ctlLocalFile.Text = System.IO.Path.GetFileName(filename);
            else
                this._ctlLocalFile.Text = null;
        }


        private void loadTopicList()
        {
            ISNSRootViewModel model = ToolkitFactory.Instance.Navigator.SelectedAccount.AccountViewModel.FindSingleChild<ISNSRootViewModel>(false);
            foreach (var child in model.Children)
            {
                var topic = child as ISNSTopicViewModel;
                if(topic == null)
                    continue;

                this._ctlSNSTopic.Items.Add(topic.TopicArn);
            }
        }

        private void onCreateTopicClick(object sender, RoutedEventArgs e)
        {
            ISNSRootViewModel model = ToolkitFactory.Instance.Navigator.SelectedAccount.AccountViewModel.FindSingleChild<ISNSRootViewModel>(false);
            ISNSRootViewMetaNode meta = model.MetaNode as ISNSRootViewMetaNode;
            var results = meta.OnCreateTopic(model);

            if (results.Success)
            {
                string topicArn = results.Parameters["CreatedTopic"] as string;
                this._ctlSNSTopic.Items.Add(topicArn);
                this._ctlSNSTopic.Text = topicArn;
                model.AddTopic(this._ctlSNSTopic.Text);
            }
        }
    }
}
