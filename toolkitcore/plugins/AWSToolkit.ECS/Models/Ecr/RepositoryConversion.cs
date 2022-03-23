namespace Amazon.AWSToolkit.ECS.Models.Ecr
{
    public static class RepositoryConversion
    {
        public static Repository AsRepository(this Amazon.ECR.Model.Repository repository)
        {
            return new Repository()
            {
                CreatedOn = repository.CreatedAt.ToLocalTime(),
                Arn = repository.RepositoryArn,
                Name = repository.RepositoryName,
                Uri = repository.RepositoryUri,
            };
        }
    }
}
