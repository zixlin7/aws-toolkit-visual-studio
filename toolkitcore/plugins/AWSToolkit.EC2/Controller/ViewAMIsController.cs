using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.AWSToolkit.EC2.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class ViewAMIsController : FeatureController<ViewAMIsModel>
    {
        ViewAMIsControl _control;
        Dictionary<CommonImageFilters, List<Image>> _describeCache = new Dictionary<CommonImageFilters, List<Image>>();
        IEnumerable<IEC2Column> _columnsToSearch;
        private ToolkitContext _toolkitContext;

        public ViewAMIsController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        protected override void DisplayView()
        {
            this._control = new ViewAMIsControl(this, _toolkitContext.RegionProvider);
            ToolkitFactory.Instance.ShellProvider.OpenInEditor(this._control);
        }

        public void LoadModel()
        {
            this.RefreshImages(true);
        }

        public void RefreshImages(bool fullRefresh)
        {
            if (fullRefresh)
                this._describeCache.Clear();

            CommonImageFilters commonFilter = this.Model.CommonImageFilter;
            List<Image> images;
            if (!this._describeCache.TryGetValue(commonFilter, out images))
            {
                var request = new DescribeImagesRequest();
                request.Filters = new List<Filter>();
                if (this.Model.CommonImageFilter == CommonImageFilters.OWNED_BY_ME)
                    request.Owners = new List<string>(){"self"};
                else
                    request.Filters.AddRange(this.Model.CommonImageFilter.Filters);

                request.Filters.Add(new Filter() { Name = "image-type", Values = new List<string>() { "machine" } });

                var response = this.EC2Client.DescribeImages(request);

                images = response.Images;
                this._describeCache[commonFilter] = images;
            }

            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
            {
                this.Model.Images.Clear();
                foreach (var image in images.OrderBy(x => x.ImageId.ToLower()))
                {
                    var wrapper = new ImageWrapper(image);
                    if (passClientFilter(wrapper))
                        this.Model.Images.Add(wrapper);
                }
            }));
        }

        public void SetSearchColumns(IEnumerable<IEC2Column> columnsToSearch)
        {
            this._columnsToSearch = columnsToSearch;
            this.RefreshImages(false);
        }

        bool passClientFilter(ImageWrapper image)
        {
            if (this.Model.PlatformFilter == PlatformPicker.WINDOWS)
            {
                if (!image.IsWindowsPlatform)
                    return false;

            }
            else if (this.Model.PlatformFilter == PlatformPicker.LINUX) 
            {
                if (image.IsWindowsPlatform)
                    return false;
            }

            if (!string.IsNullOrEmpty(this.Model.TextFilter) && this._columnsToSearch != null)
            {
                string textFilter = this.Model.TextFilter.ToLower();

                foreach (var column in this._columnsToSearch)
                {
                    string text = column.GetTextValue(image);
                    if (text == null)
                        continue;

                    text = text.ToLower();
                    if (text.Contains(textFilter))
                        return true;
                }

                return false;
            }

            return true;
        }

        public ActionResults CopyAmi(ImageWrapper image, ToolkitRegion destination)
        {
            var controller = new CopyAmiController(this.Region.Id, destination, this.FeatureViewModel.AccountViewModel);
            return controller.Execute(this.EC2Client, new List<ImageWrapper> { image });
        }

        public ActionResults Deregister(IList<ImageWrapper> images)
        {
            var controller = new DeregisterAMIController();
            var results = controller.Execute(this.EC2Client, images);
            if(results.Success)
            {
                foreach (var image in images)
                {
                    this.Model.Images.Remove(image);
                }
            }

            return results;
        }

        public ActionResults EditPermission(ImageWrapper image)
        {
            var controller = new AMIPermissionController();
            return controller.Execute(EC2Client, image);
        }

        public void LaunchInstance(ImageWrapper image)
        {
            var controller = new LaunchController(_toolkitContext, image.NativeImage);
            var result = controller.Execute(this.FeatureViewModel);

            var rootModel = this.FeatureViewModel.Parent as EC2RootViewModel;
            if (result.Success && rootModel != null)
            {
                var instanceModel = rootModel.FindSingleChild<EC2InstancesViewModel>(false);
                if (instanceModel != null)
                {
                    instanceModel.ExecuteDefaultAction();
                }
            }
        }

        public void RecordCopyAmi(ActionResults result)
        {
            var data = CreateMetricData<Ec2CopyAmiToRegion>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2CopyAmiToRegion(data);
        }

        public void RecordDeleteAmi(int imageCount, ActionResults result)
        {
            var data = CreateMetricData<Ec2DeleteAmi>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.Value = imageCount;
            _toolkitContext.TelemetryLogger.RecordEc2DeleteAmi(data);
        }

        public void RecordEditAmiPermission(ActionResults result)
        {
            var data = CreateMetricData<Ec2EditAmiPermission>(result, _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            _toolkitContext.TelemetryLogger.RecordEc2EditAmiPermission(data);
        }
    }
}
