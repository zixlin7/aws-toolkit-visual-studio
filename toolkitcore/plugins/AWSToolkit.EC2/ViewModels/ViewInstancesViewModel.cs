using System.Collections.Generic;
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
        private readonly IElasticIpRepository _elasticIpRepository;

        public ViewInstancesViewModel(ViewInstancesModel viewInstancesModel, IInstanceRepository instanceRepository, IElasticIpRepository elasticIpRepository, 
            ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            InstanceRepository = instanceRepository;
            _elasticIpRepository = elasticIpRepository;
            ViewInstancesModel = viewInstancesModel;
        }

        public async Task<IEnumerable<AddressWrapper>> GetAvailableElasticIpsAsync(RunningInstanceWrapper instance)
        {
            var domainFilter = GetDomain(instance);

            return await GetUnassociatedElasticIpsAsync(domainFilter);
        }

        public static string GetDomain(RunningInstanceWrapper instance)
        {
            return string.IsNullOrWhiteSpace(instance.VpcId)
                ? AddressWrapper.DOMAIN_EC2
                : AddressWrapper.DOMAIN_VPC;
        }

        public async Task<IEnumerable<AddressWrapper>> GetUnassociatedElasticIpsAsync(string domainFilter)
        {
            var elasticIps = await _elasticIpRepository.ListElasticIpsAsync();

            return elasticIps
                .Where(address => string.IsNullOrWhiteSpace(address.InstanceId) && address.Domain == domainFilter);
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
