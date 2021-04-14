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

        public void CopyAmi(ImageWrapper image, ToolkitRegion destination)
        {
            var controller = new CopyAmiController(this.Region.Id, destination, this.FeatureViewModel.AccountViewModel);
            controller.Execute(this.EC2Client, new List<ImageWrapper> { image });
        }

        public void Deregister(IList<ImageWrapper> images)
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
        }

        public void EditPermission(ImageWrapper image)
        {
            var controller = new AMIPermissionController();
            controller.Execute(this.EC2Client, image);
        }

        public void LaunchInstance(ImageWrapper image)
        {
            var controller = new LaunchController();
            var result = controller.Execute(this.FeatureViewModel, image.NativeImage);

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

    }
}
