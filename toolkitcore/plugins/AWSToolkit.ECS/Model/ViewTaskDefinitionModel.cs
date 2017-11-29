using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class ViewTaskDefinitionModel : BaseModel
    {
        private TaskDefinitionWrapper _taskDefinitionWrapper;

        public TaskDefinitionWrapper TaskDefinition
        {
            get { return _taskDefinitionWrapper; }
            internal set { _taskDefinitionWrapper = value; NotifyPropertyChanged("TaskDefinition"); }
        }
    }
}
