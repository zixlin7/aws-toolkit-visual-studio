using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit;

using Amazon.S3;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class CreateBucketController : BaseContextCommand
    {
        CreateBucketControl _control;
        CreateBucketModel _model;
        S3RootViewModel _rootModel;
        ActionResults _results;

        public override ActionResults Execute(IViewModel model)
        {
            this._rootModel = model as S3RootViewModel;
            if (this._rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._model = new CreateBucketModel();
            this._control = new CreateBucketControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control);

            if(this._results == null)
                return new ActionResults().WithSuccess(false);
            return this._results;
        }

        public CreateBucketModel Model
        {
            get { return this._model; }
        }

        public bool Persist()
        {
            try
            {
                PutBucketRequest request = new PutBucketRequest()
                {
                    BucketName = this._control.Model.BucketName,
                    BucketRegionName = this._rootModel.CurrentEndPoint.RegionSystemName
                };
                
                this._rootModel.S3Client.PutBucket(request);

                this._results = new ActionResults()
                    .WithSuccess(true)
                    .WithFocalname(this._control.Model.BucketName)
                    .WithShouldRefresh(true);

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating bucket: " + e.Message);
                this._results = new ActionResults().WithSuccess(false);
                return false;
            }
        }
    }
}
