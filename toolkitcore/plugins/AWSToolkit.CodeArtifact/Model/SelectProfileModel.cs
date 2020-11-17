using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.CodeArtifact.Model
{

    public class SelectProfileModel : BaseModel
    {
        string _profileName;

        public SelectProfileModel()
        {
        }

        public string ProfileName
        {
            get => _profileName;

            set { SetProperty(ref _profileName, value, () => ProfileName); }
        }
    }
}
