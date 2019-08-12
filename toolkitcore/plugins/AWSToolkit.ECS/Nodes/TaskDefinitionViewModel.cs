using Amazon.AWSToolkit.ECS.Model;
using Amazon.ECS;
using log4net;

namespace Amazon.AWSToolkit.ECS.Nodes
{
    public class TaskDefinitionViewModel : FeatureViewModel
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TaskDefinitionViewModel));

        readonly RootViewModel _rootViewModel;
        readonly IAmazonECS _ecsClient;
        readonly TaskDefinitionWrapper _taskDefinition;

        public TaskDefinitionViewModel(TaskDefinitionViewMetaNode metaNode, RootViewModel viewModel, TaskDefinitionWrapper taskDefinition)
            : base(metaNode, viewModel, taskDefinition.Family)
        {
            this._rootViewModel = viewModel;
            this._taskDefinition = taskDefinition;
            this._ecsClient = viewModel.ECSClient;
        }

        public RootViewModel RootViewModel => this._rootViewModel;

        public TaskDefinitionWrapper TaskDefinition => this._taskDefinition;

        protected override string IconName => "Amazon.AWSToolkit.ECS.Resources.EmbeddedImages.taskdef.png";

        public string Family => _taskDefinition.Family;
    }
}
