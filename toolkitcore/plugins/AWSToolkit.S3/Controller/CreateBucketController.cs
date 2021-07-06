using System;

using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Nodes;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.Shared;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class CreateBucketController : BaseContextCommand
    {
        private const string ParameterCreateCancelled = "CreateCancelled";

        private readonly ToolkitContext _toolkitContext;
        private readonly IAWSToolkitShellProvider _shellProvider;

        CreateBucketControl _control;
        CreateBucketModel _model;
        S3RootViewModel _rootModel;
        ActionResults _results;

        public CreateBucketController(ToolkitContext toolkitContext, IAWSToolkitShellProvider shellProvider)
        {
            _toolkitContext = toolkitContext;
            _shellProvider = shellProvider;
        }

        public override ActionResults Execute(IViewModel model)
        {
            var result = ExecuteCreateBucket(model);

            RecordMetric(result);

            return result;
        }

        private ActionResults ExecuteCreateBucket(IViewModel model)
        {
            try
            {
                this._rootModel = model as S3RootViewModel;
                if (this._rootModel == null)
                {
                    return new ActionResults().WithSuccess(false);
                }

                this._model = new CreateBucketModel();
                this._control = new CreateBucketControl(this);
                if (!_shellProvider.ShowModal(this._control))
                {
                    return new ActionResults()
                        .WithSuccess(false)
                        .WithParameter(ParameterCreateCancelled, true);
                }

                if(this._results == null)
                    return new ActionResults().WithSuccess(false);
                return this._results;
            }
            catch (Exception e)
            {
                ShowCreationError(e);
                return new ActionResults().WithSuccess(false);
            }
        }

        public CreateBucketModel Model => this._model;

        public bool Persist()
        {
            try
            {
                PutBucketRequest request = new PutBucketRequest()
                {
                    BucketName = this._control.Model.BucketName,
                    BucketRegionName = this._rootModel.Region.Id
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
                ShowCreationError(e);
                this._results = new ActionResults().WithSuccess(false);
                return false;
            }
        }

        private void ShowCreationError(Exception e)
        {
            _shellProvider.ShowError($"Error creating bucket:{Environment.NewLine}{e.Message}");
        }

        private void RecordMetric(ActionResults actionResults)
        {
            _toolkitContext.TelemetryLogger.RecordS3CreateBucket(new S3CreateBucket()
            {
                AwsAccount = _toolkitContext.ConnectionManager.ActiveAccountId,
                AwsRegion = _toolkitContext.ConnectionManager.ActiveRegion.Id,
                Result = GetMetricsResult(actionResults),
            });
        }

        private static Result GetMetricsResult(ActionResults actionResults)
        {
            if (actionResults.GetParameter(ParameterCreateCancelled, false))
            {
                return Result.Cancelled;
            }

            if (actionResults.Success)
            {
                return Result.Succeeded;
            }

            return Result.Failed;
        }
    }
}
