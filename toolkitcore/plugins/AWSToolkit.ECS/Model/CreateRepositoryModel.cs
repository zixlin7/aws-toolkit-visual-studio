using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class CreateRepositoryModel : BaseModel
    {
        string _repositoryName;

        public CreateRepositoryModel()
        {
        }

        public string RepositoryName
        {
            get { return this._repositoryName; }
            set
            {
                this._repositoryName = value;
                this.NotifyPropertyChanged("RepositoryName");
            }
        }
    }
}
