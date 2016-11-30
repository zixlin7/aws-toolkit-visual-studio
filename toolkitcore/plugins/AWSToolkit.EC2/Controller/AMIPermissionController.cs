using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.View;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;


namespace Amazon.AWSToolkit.EC2.Controller
{
    public class AMIPermissionController
    {
        ActionResults _results;
        IAmazonEC2 _ec2Client;
        AMIPermissionModel _model;

        public ActionResults Execute(IAmazonEC2 ec2Client, ImageWrapper image)
        {
            this._model = new AMIPermissionModel(image);
            this._ec2Client = ec2Client;
            var control = new AMIPermissionControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);
            if (this._results != null)
                return this._results;

            return new ActionResults().WithSuccess(false);
        }

        public AMIPermissionModel Model
        {
            get { return this._model; }
        }

        public IAmazonEC2 EC2Client { get { return this._ec2Client; } }

        public void LoadModel()
        {
            this._model.IsPublic = this._model.Image.NativeImage.Public;

            this._model.UserIds = new ObservableCollection<MutableString>();
            this._model.OriginalUserIds = new HashSet<string>();

            var response = this._ec2Client.DescribeImageAttribute(new DescribeImageAttributeRequest()
            {
                ImageId = this._model.Image.NativeImage.ImageId,
                Attribute = "launchPermission"
            });

            foreach (var launch in response.ImageAttribute.LaunchPermissions.OrderBy(x => x.UserId))
            {
                if (string.IsNullOrEmpty(launch.UserId))
                    continue;

                this._model.OriginalUserIds.Add(launch.UserId);
                this._model.UserIds.Add(new MutableString(launch.UserId));
            }
        }

        public void SavePermissions()
        {
            addUsers();
            removeUsers();
            changeVisibility();

            this._results = new ActionResults().WithSuccess(true);
        }

        void changeVisibility()
        {
            string currentValue = this._model.IsPublic ?
                EC2Constants.IMAGE_VISIBILITY_PUBLIC : EC2Constants.IMAGE_VISIBILITY_PRIVATE;

            if (this._model.IsPublic == this._model.Image.NativeImage.Public)
                return;

            var request = new ModifyImageAttributeRequest()
            {
                ImageId = this._model.Image.NativeImage.ImageId,
                Attribute = "launchPermission",
                UserGroups = new List<string>(){"all"}
            };

            if (this._model.IsPublic)
            {
                request.OperationType = "add";               
            }
            else
            {
                request.OperationType = "remove";
            }
            this._ec2Client.ModifyImageAttribute(request);

            this._model.Image.FormattedVisibility = this.Model.IsPublic ?
                EC2Constants.IMAGE_VISIBILITY_PUBLIC : EC2Constants.IMAGE_VISIBILITY_PRIVATE;
        }

        void addUsers()
        {
            List<string> toBeAdded = new List<string>();
            foreach (var userId in this.Model.UserIds)
            {
                if (userId.Value == null || userId.Value.Trim() == "")
                    continue;

                if (!this.Model.OriginalUserIds.Contains(userId.Value))
                {
                    toBeAdded.Add(userId.Value.Replace("-", "").Trim());
                }
            }

            var request = new ModifyImageAttributeRequest()
            {
                ImageId = this._model.Image.NativeImage.ImageId,
                OperationType = "add",
                Attribute = "launchPermission"
            };
            request.UserIds = toBeAdded;

            this._ec2Client.ModifyImageAttribute(request);
        }

        void removeUsers()
        {
            List<string> toBeRemoved = new List<string>();
            foreach (var userId in this.Model.OriginalUserIds)
            {
                if (!this.Model.UserIds.Contains(new MutableString(userId)))
                {
                    toBeRemoved.Add(userId.Replace("-","").Trim());
                }
            }

            var request = new ModifyImageAttributeRequest()
            {
                ImageId = this._model.Image.NativeImage.ImageId,
                OperationType = "remove",
                Attribute = "launchPermission"
            };
            request.UserIds = toBeRemoved;
            this._ec2Client.ModifyImageAttribute(request);
        }
    }
}
