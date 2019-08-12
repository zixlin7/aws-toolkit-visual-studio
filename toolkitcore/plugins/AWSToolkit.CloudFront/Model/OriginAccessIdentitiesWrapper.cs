namespace Amazon.AWSToolkit.CloudFront.Model
{
    public class OriginAccessIdentitiesWrapper
    {

        public OriginAccessIdentitiesWrapper(string id, string comment, string canonicalUserId)
        {
            this.Id = id;
            this.Comment = comment;
            this.CanonicalUserId = canonicalUserId;
        }

        public string Comment
        {
            get;
        }

        public string Id
        {
            get;
        }

        public string CanonicalUserId
        {
            get;
        }

        public string FormattedCanonicalUserId
        {
            get
            {
                if (this.CanonicalUserId.Length < 10)
                    return this.CanonicalUserId;

                return this.CanonicalUserId.Substring(0, 10) + "...";
            }
        }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(this.Comment))
                    return FormattedCanonicalUserId;

                return string.Format("{0} ({1})", this.Comment, this.FormattedCanonicalUserId);
            }
        }
    }
}
