using System;
using System.IO;
using Amazon.AWSToolkit.CodeCommit.Interface.Model;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class CreateRepositoryModel : BaseRepositoryModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string LocalFolder { get; set; }

        public INewCodeCommitRepositoryInfo GetNewRepositoryInfo()
        {
            var info = new NewRepositoryInfo
            {
                OwnerAccount = Account,
                Region = SelectedRegion,
                Name = Name,
                Description = Description
            };

            var finalPathComponent = Path.GetFileName(LocalFolder);
            if (!finalPathComponent.Equals(Name, StringComparison.OrdinalIgnoreCase))
            {
                info.LocalFolder = Path.Combine(LocalFolder, Name);
            }

            return info;
        }
    }
}
