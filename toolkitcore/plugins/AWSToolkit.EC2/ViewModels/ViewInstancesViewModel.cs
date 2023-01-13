using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;

namespace Amazon.AWSToolkit.EC2.ViewModels
{
    public class ViewInstancesViewModel
    {
        public IInstanceRepository InstanceRepository { get; }

        private readonly ViewInstancesModel _viewInstancesModel;

        public ViewInstancesViewModel(ViewInstancesModel viewInstancesModel, IInstanceRepository instanceRepository)
        {
            InstanceRepository = instanceRepository;
            _viewInstancesModel = viewInstancesModel;
        }
    }
}
