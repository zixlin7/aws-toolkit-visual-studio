using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CodeCommit.Model
{
    public class CreateRepositoryModel : BaseRepositoryModel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string LocalFolder { get; set; }
    }
}
