using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;

namespace Amazon.AWSToolkit.EC2.ViewModels
{
    public class ViewInstancesViewModel
    {
        public IInstanceRepository InstanceRepository { get; }
        public ViewInstancesModel ViewInstancesModel { get; }

        private readonly ToolkitContext _toolkitContext;

        public ViewInstancesViewModel(ViewInstancesModel viewInstancesModel, IInstanceRepository instanceRepository,
            ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            InstanceRepository = instanceRepository;
            ViewInstancesModel = viewInstancesModel;
        }

        public RunningInstanceWrapper GetInstanceModel(string instanceId)
        {
            return ViewInstancesModel.RunningInstances.FirstOrDefault(i => i.InstanceId == instanceId);
        }

        public async Task ReloadInstancesAsync()
        {
            var instances = await InstanceRepository.ListInstancesAsync();

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                ViewInstancesModel.RunningInstances.Clear();
                ViewInstancesModel.RunningInstances.AddAll(instances);
            });
        }
    }
}
