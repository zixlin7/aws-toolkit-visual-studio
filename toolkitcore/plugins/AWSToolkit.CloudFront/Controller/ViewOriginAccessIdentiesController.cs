using System.Windows;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFront.Nodes;
using Amazon.AWSToolkit.CloudFront.View;
using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.CloudFront;
using Amazon.CloudFront.Model;


namespace Amazon.AWSToolkit.CloudFront.Controller
{
    public class ViewOriginAccessIdentiesController : BaseContextCommand
    {
        ViewOriginAccessIdentiesControl _control;
        ViewOriginAccessIdentiesModel _model;
        IAmazonCloudFront _cfClient;

        public override ActionResults Execute(IViewModel model)
        {
            var rootModel = model as CloudFrontRootViewModel;
            if (rootModel == null)
                return new ActionResults().WithSuccess(false);

            this._cfClient = rootModel.CFClient;
            this._model = new ViewOriginAccessIdentiesModel();
            this._control = new ViewOriginAccessIdentiesControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(this._control, MessageBoxButton.OK);

            return new ActionResults().WithSuccess(true);
        }

        public ViewOriginAccessIdentiesModel Model => this._model;

        public void LoadModel()
        {
            this._model.Identities.Clear();
            var response = this._cfClient.ListCloudFrontOriginAccessIdentities();
            foreach (var oai in response.CloudFrontOriginAccessIdentityList.Items)
            {
                var identity = new ViewOriginAccessIdentiesModel.OriginAccessIdentity();
                identity.Id = oai.Id;
                identity.Comment = oai.Comment;
                identity.CanonicalUserId = oai.S3CanonicalUserId;
                this._model.Identities.Add(identity);
            }
        }

        public void CreateOriginAccessIdentity()
        {
            CreateOriginAccessIdentityController controller = new CreateOriginAccessIdentityController(this._cfClient);
            if (controller.Execute())
            {
                var newItem = new ViewOriginAccessIdentiesModel.OriginAccessIdentity();
                newItem.Id = controller.Model.OriginAccessIdentity.Id;
                newItem.CanonicalUserId = controller.Model.OriginAccessIdentity.S3CanonicalUserId;
                newItem.Comment = controller.Model.OriginAccessIdentity.CloudFrontOriginAccessIdentityConfig.Comment;
                this._model.Identities.Add(newItem);
            }
        }

        public void DeleteOriginAccessIdentities(ViewOriginAccessIdentiesModel.OriginAccessIdentity[] identities)
        {
            foreach (var identity in identities)
            {
                var getInfoResponse = this._cfClient.GetCloudFrontOriginAccessIdentity(
                    new GetCloudFrontOriginAccessIdentityRequest() { Id = identity.Id });

                var deleteRequest = new DeleteCloudFrontOriginAccessIdentityRequest()
                {
                    IfMatch = getInfoResponse.ETag,
                    Id = getInfoResponse.CloudFrontOriginAccessIdentity.Id
                };
                this._cfClient.DeleteCloudFrontOriginAccessIdentity(deleteRequest);
                this._model.Identities.Remove(identity);
            }
        }

    }
}
