using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Amazon.AWSToolkit.CommonUI
{
    public class CommonIcons
    {
        public Stream FolderIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.folder.png");
                return stream;
            }
        }

        public Stream LeftIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.generic-leftarrow.png");
                return stream;
            }
        }

        public Stream RightIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.generic-rightarrow.png");
                return stream;
            }
        }

        public Stream RefreshIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.refresh.png");
                return stream;
            }
        }

        public Stream CreateNewBucketIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.S3.new-bucket.png");
                return stream;
            }
        }

        public Stream AddIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.generic-add.png");
                return stream;
            }
        }

        public Stream CopyIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.copy.png");
                return stream;
            }
        }

        public Stream RemoveIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.generic-remove.png");
                return stream;
            }
        }

        public Stream AbortIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.abort.png");
                return stream;
            }
        }

        public Stream SaveIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.save.png");
                return stream;
            }
        }

        public Stream ExportIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.export.png");
                return stream;
            }
        }

        public Stream ImportIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.import.png");
                return stream;
            }
        }

        public Stream WarningIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.warning.png");
                return stream;
            }
        }

        public Stream WarningIcon16x16
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.warning16x16.png");
                return stream;
            }
        }

        public Stream InvalidIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.invalid.png");
                return stream;
            }
        }

        public Stream CreateNewTopicIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.SNS.create_topic.png");
                return stream;
            }
        }

        public Stream BucketIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.S3.bucket.png");
                return stream;
            }
        }


        public Stream SendIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.SNS.send-message.png");
                return stream;
            }
        }

        public Stream PublishTopicIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.SNS.publish_to_topic.png");
                return stream;
            }
        }

        public Stream CreateSubscriptionIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.SNS.create_subscription.png");
                return stream;
            }
        }

        public Stream DeleteSubscriptionIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.SNS.delete_subscription.png");
                return stream;
            }
        }

        public Stream ExecuteIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.execute.png");
                return stream;
            }
        }

        public Stream AddColumnIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.add-column.png");
                return stream;
            }
        }

        public Stream FetchMoreIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.fetch_one_page.png");
                return stream;
            }
        }

        public Stream FetchLotsMoreIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.fetch_few_pages.png");
                return stream;
            }
        }

        public Stream CommitChangesIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.commit-changes.png");
                return stream;
            }
        }

        public Stream UploadIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.upload.png");
                return stream;
            }
        }

        public Stream DownloadIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.download.png");
                return stream;
            }
        }

        public Stream CreateFolderIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.S3.create-folder.png");
                return stream;
            }
        }

        public Stream CreateAccessKeyIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.IdentityManagement.key_create.png");
                return stream;
            }
        }

        public Stream DeleteAccessKeyIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.IdentityManagement.key_delete.png");
                return stream;
            }
        }

        public Stream HelpIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.help.png");
                return stream;
            }
        }

        public Stream NavBackIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.navback.png");
                return stream;
            }
        }

        public Stream NavForwardIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.navforward.png");
                return stream;
            }
        }

        public Stream CancelIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.cancel.png");
                return stream;
            }
        }

        public Stream CompleteIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.complete.png");
                return stream;
            }
        }

        public Stream Complete24x24Icon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.complete_24x24.png");
                return stream;
            }
        }

        public Stream ShowHideIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.showhide.png");
                return stream;
            }
        }

        public Stream AddAccountIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.Accounts.AddAccounts.png");
                return stream;
            }
        }

        public Stream EditAccountIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.Accounts.EditAccount.png");
                return stream;
            }
        }

        public Stream DeleteAccountIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.Accounts.DeleteAccount.png");
                return stream;
            }
        }

        public Stream OKIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.ok.png");
                return stream;
            }
        }

        public Stream EditableSmallIcon
        {
            get
            {
                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.Resources.editable_small.png");
                return stream;
            }
        }

        public IDictionary<string, Stream> Cached
        {
            get 
            { 
                return null; 
            }
        }
    }
}
