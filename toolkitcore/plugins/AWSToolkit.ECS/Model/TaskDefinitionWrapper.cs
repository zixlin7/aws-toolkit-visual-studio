using System.ComponentModel;
using System.Linq;
using Amazon.AWSToolkit.CommonUI;
using Amazon.ECS.Model;

namespace Amazon.AWSToolkit.ECS.Model
{
    public class TaskDefinitionWrapper : PropertiesModel, IWrapper
    {
        private TaskDefinition _taskDefinition;
        private readonly string _taskDefinitionArn;

        public TaskDefinitionWrapper(TaskDefinition taskDefinition)
        {
            _taskDefinition = taskDefinition;
            _taskDefinitionArn = taskDefinition.TaskDefinitionArn;
        }

        public TaskDefinitionWrapper(string arn)
        {
            _taskDefinitionArn = arn;
        }

        public void LoadFrom(TaskDefinition taskDefinition)
        {
            _taskDefinition = taskDefinition;
        }

        public bool IsLoaded
        {
            get { return _taskDefinition != null; }
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "TaskDefinition";
            componentName = this._taskDefinition.Family;
        }

        [DisplayName("Family")]
        [AssociatedIcon(true, "TaskDefinitionIcon")]
        public string Family
        {
            get
            {
                return _taskDefinition != null ? _taskDefinition.Family : _taskDefinitionArn.Split('/').LastOrDefault();
            }
        }

        [DisplayName("ARN")]
        public string TaskDefinitionArn
        {
            get { return _taskDefinitionArn; }
        }


        [Browsable(false)]
        public string TypeName
        {
            get { return "TaskDefinition"; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get
            {
                return string.Format("{0} ({1})", Family, _taskDefinitionArn);
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource TaskDefinitionIcon
        {
            get
            {
                var icon = IconHelper.GetIcon("taskdef.png");
                return icon.Source;
            }
        }
    }
}
