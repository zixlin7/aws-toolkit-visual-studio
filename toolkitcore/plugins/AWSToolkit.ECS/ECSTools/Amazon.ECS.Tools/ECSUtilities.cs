using Amazon.Common.DotNetCli.Tools;
using Amazon.ECR;
using Amazon.ECR.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECS.Model;

namespace Amazon.ECS.Tools
{
    public static class ECSUtilities
    {

        public static List<PlacementConstraint> ConvertPlacementConstraint(string[] values)
        {
            if (values == null || values.Length == 0)
                return null;

            var list = new List<PlacementConstraint>();

            foreach(var value in values)
            {
                var tokens = value.Split('=');
                var constraint = new PlacementConstraint
                {
                    Type = tokens[0]
                };

                if (tokens.Length > 1)
                    constraint.Expression = tokens[1];

                list.Add(constraint);
            }

            return list;
        }

        public static List<PlacementStrategy> ConvertPlacementStrategy(string[] values)
        {
            if (values == null || values.Length == 0)
                return null;

            var list = new List<PlacementStrategy>();

            foreach (var value in values)
            {
                var tokens = value.Split('=');
                var constraint = new PlacementStrategy
                {
                    Type = tokens[0]
                };

                if (tokens.Length > 1)
                    constraint.Field = tokens[1];

                list.Add(constraint);
            }

            return list;
        }

        public static async Task<string> ExpandImageTagIfNecessary(IToolLogger logger, IAmazonECR ecrClient, string dockerImageTag)
        {
            try
            {
                if (dockerImageTag.Contains(".amazonaws."))
                    return dockerImageTag;

                string repositoryName = dockerImageTag;
                if (repositoryName.Contains(":"))
                    repositoryName = repositoryName.Substring(0, repositoryName.IndexOf(':'));

                DescribeRepositoriesResponse describeResponse = null;
                try
                {
                    describeResponse = await ecrClient.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
                    {
                        RepositoryNames = new List<string> { repositoryName }
                    });
                }
                catch (Exception e)
                {
                    if (!(e is RepositoryNotFoundException))
                    {
                        throw;
                    }
                }

                // Not found in ECR, assume pulling Docker Hub
                if (describeResponse == null)
                {
                    return dockerImageTag;
                }

                var fullPath = describeResponse.Repositories[0].RepositoryUri + dockerImageTag.Substring(dockerImageTag.IndexOf(':'));
                logger?.WriteLine($"Determined full image name to be {fullPath}");
                return fullPath;
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error determing full repository path for the image {dockerImageTag}: {e.Message}", DockerToolsException.ECSErrorCode.FailedToExpandImageTag);
            }
        }
    }
}
