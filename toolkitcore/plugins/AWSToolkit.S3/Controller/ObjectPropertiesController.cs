using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Amazon.AWSToolkit.S3.View;
using Amazon.AWSToolkit.S3.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace Amazon.AWSToolkit.S3.Controller
{
    public class ObjectPropertiesController
    {        
        IAmazonS3 _s3Client;
        ObjectPropertiesModel _model;

        public ObjectPropertiesController(IAmazonS3 s3Client, string bucketName, string key)
            : this(s3Client, new ObjectPropertiesModel(bucketName, key, s3Client.GetPublicURL(bucketName, key)))
        {
        }

        public ObjectPropertiesController(IAmazonS3 s3Client, ObjectPropertiesModel model)
        {
            this._s3Client = s3Client;
            this._model = model;
        }

        public ObjectPropertiesModel Model => this._model;

        public void Execute()
        {
            ObjectPropertiesControl control = new ObjectPropertiesControl(this);
            ToolkitFactory.Instance.ShellProvider.ShowModal(control);
        }

        public void LoadModel()
        {
            var listObjectResponse = this._s3Client.ListObjects(new ListObjectsRequest()
            {
                BucketName = this._model.BucketName,
                Prefix = this.Model.Key,
                MaxKeys = 1
            });

            if (listObjectResponse.S3Objects.Count == 0)
                return;

            var s3o = listObjectResponse.S3Objects[0];
            this._model.StorageClass = s3o.StorageClass;

            GetObjectMetadataResponse getMetadataResponse;
            try
            {
                getMetadataResponse = this._s3Client.GetObjectMetadata(new GetObjectMetadataRequest()
                {
                    BucketName = this._model.BucketName,
                    Key = this._model.Key
                });
            }
            catch
            {
                this._model.ErrorRetrievingMetadata = true;
                return;
            }
            this._model.UseServerSideEncryption = getMetadataResponse.ServerSideEncryptionMethod != ServerSideEncryptionMethod.None;
            this._model.UsesKMSServerSideEncryption = getMetadataResponse.ServerSideEncryptionMethod == ServerSideEncryptionMethod.AWSKMS;
            this._model.WebsiteRedirectLocation = getMetadataResponse.WebsiteRedirectLocation;

            LoadModelMetadata();
            LoadObjectTags();
            LoadModelPermissions();

            if (getMetadataResponse.RestoreInProgress || getMetadataResponse.RestoreExpiration.HasValue)
            {
                if (getMetadataResponse.RestoreInProgress)
                    this._model.RestoreInfo = "Restore currently in progress";
                else
                    this._model.RestoreInfo = string.Format("Restored copy will expire on {0}", getMetadataResponse.RestoreExpiration);
            }
        }

        private void LoadModelMetadata()
        {
            var getMetadataResponse = this._s3Client.GetObjectMetadata(new GetObjectMetadataRequest()
            {
                BucketName = this._model.BucketName,
                Key = this._model.Key
            });

            List<Metadata> metadataEntries = new List<Metadata>();
            var metadata = getMetadataResponse.Metadata;
            foreach (string key in metadata.Keys)
            {
                metadataEntries.Add(new Metadata(key, metadata[key]));
            }

            var headers = getMetadataResponse.Headers;
            foreach (string key in headers.Keys)
            {
                if (Metadata.HEADER_NAMES.Contains(key))
                {
                    metadataEntries.Add(new Metadata(key, headers[key]));
                }
            }

            foreach (var entry in metadataEntries.OrderBy(item => item.Key))
            {
                this._model.MetadataEntries.Add(entry);
            }
        }

        private void LoadObjectTags()
        {
            var getObjectTagsResponse = this._s3Client.GetObjectTagging(new GetObjectTaggingRequest
            {
                BucketName = this._model.BucketName,
                Key = this._model.Key
            });

            this._model.OriginalTags.Clear();
            this._model.Tags.Clear();
            foreach(Tag tag in getObjectTagsResponse.Tagging)
            {
                this._model.Tags.Add(tag);
                this._model.OriginalTags[tag.Key] = tag.Value;
            }
        }

        private void LoadModelPermissions()
        {
            var getACLResponse = this._s3Client.GetACL(new GetACLRequest()
            {
                BucketName = this._model.BucketName,
                Key = this._model.Key
            });

            Permission.LoadPermissions(this._model.PermissionEntries, getACLResponse.AccessControlList);
        }

        public void Persist()
        {
            if (this._model.UsesKMSServerSideEncryption ||
                this._model.ErrorRetrievingMetadata)
            {
                //ToolkitFactory.Instance.ShellProvider.ShowError("Unable to update", "The file uses AWS KMS Server Side Encryption, unable to modify at this time");
                return;
            }

            var listObjectResponse = this._s3Client.ListObjects(new ListObjectsRequest()
            {
                BucketName = this._model.BucketName,
                Prefix = this._model.Key,
                MaxKeys = 1
            });

            // This is retrieved in case there is a problem saving the ACL.  
            // After the CopyObject that resets the ACL so if we don't at least 
            // restore the orignal then we lose all the Grants.
            var getACLResponse = this._s3Client.GetACL(new GetACLRequest()
            {
                BucketName = this._model.BucketName,
                Key = this._model.Key
            });

            if (listObjectResponse.S3Objects.Count == 0)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("The file {0} no longer exists", this._model.Name);
                return;
            }

            var s3o = listObjectResponse.S3Objects[0];

            var copyRequest = new CopyObjectRequest()
            {
                DestinationBucket = this._model.BucketName,
                DestinationKey = this._model.Key,
                SourceBucket = this._model.BucketName,
                SourceKey = this._model.Key,
                WebsiteRedirectLocation = this._model.WebsiteRedirectLocation,
                MetadataDirective = S3MetadataDirective.REPLACE,
                StorageClass = this._model.StorageClass,
            };

            if (this._model.UseServerSideEncryption)
            {
                copyRequest.ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256;
            }

            SetupMetadataAndHeaders(copyRequest);

            this._s3Client.CopyObject(copyRequest);
            PersistPermision(s3o, getACLResponse.AccessControlList);

            PersistTags();
        }

        private void SetupMetadataAndHeaders(CopyObjectRequest copyRequest)
        {
            NameValueCollection nvcMetadata;
            NameValueCollection nvcHeader;

            Metadata.GetMetadataAndHeaders(this._model.MetadataEntries, out nvcMetadata, out nvcHeader);
            foreach(var name in nvcMetadata.AllKeys)
                copyRequest.Metadata[name] = nvcMetadata[name];
            foreach (var name in nvcHeader.AllKeys)
                copyRequest.Headers[name] = nvcHeader[name];
        }

        private void PersistPermision(S3Object s3o, S3AccessControlList orignalACL)
        {
            try
            {
                S3AccessControlList list = Permission.ConvertToAccessControlList(this._model.PermissionEntries, Permission.PermissionMode.Object);
                list.Owner = s3o.Owner;

                this._s3Client.PutACL(new PutACLRequest()
                {
                    BucketName = this._model.BucketName,
                    Key = this._model.Key,
                    AccessControlList = list
                });
            }
            // If there was an error setting the ACL then restore the orignal ACL.               
            catch
            {
                try
                {
                    this._s3Client.PutACL(new PutACLRequest()
                    {
                        BucketName = this._model.BucketName,
                        Key = this._model.Key,
                        AccessControlList = orignalACL
                    });
                }
                catch { }
                throw;
            }
        }

        private void PersistTags()
        {
            bool different = false;

            if(this._model.OriginalTags.Count != this._model.Tags.Count)
            {
                different = true;
            }
            else
            {
                foreach(var tag in this._model.Tags)
                {
                    if(!this._model.OriginalTags.ContainsKey(tag.Key))
                    {
                        different = true;
                        break;
                    }

                    var originalValue = this._model.OriginalTags[tag.Key];
                    if(!string.Equals(tag.Value, originalValue, StringComparison.Ordinal))
                    {
                        different = true;
                        break;
                    }
                }
            }

            if (!different)
                return;

            var request = new PutObjectTaggingRequest
            {
                BucketName = this._model.BucketName,
                Key = this._model.Key,
                Tagging = new Tagging()
            };
            request.Tagging.TagSet = new List<Tag>();


            foreach (var tag in this._model.Tags)
            {
                request.Tagging.TagSet.Add(tag);
            }

            this._s3Client.PutObjectTagging(request);
        }

        public string GetPreSignedURL()
        {
            return this._s3Client.GetPreSignedURL(new GetPreSignedUrlRequest()
            {
                BucketName = this._model.BucketName,
                Key = this._model.Key,
                Expires = DateTime.Now.AddMinutes(1)
            });
        }
    }
}
