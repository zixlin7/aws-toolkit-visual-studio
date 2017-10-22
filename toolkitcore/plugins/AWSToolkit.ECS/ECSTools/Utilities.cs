using Amazon.ECR;
using Amazon.ECR.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.ECS.Tools
{
    public static class Utilities
    {

        internal static string[] SplitByComma(this string str)
        {
            return str?.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Determines the location of the project depending on how the workingDirectory and projectLocation
        /// fields are set. This location is root of the project.
        /// </summary>
        /// <param name="workingDirectory"></param>
        /// <param name="projectLocation"></param>
        /// <returns></returns>
        public static string DetermineProjectLocation(string workingDirectory, string projectLocation)
        {
            string location;
            if (string.IsNullOrEmpty(projectLocation))
            {
                location = workingDirectory;
            }
            else
            {
                if (Path.IsPathRooted(projectLocation))
                    location = projectLocation;
                else
                    location = Path.Combine(workingDirectory, projectLocation);
            }

            if (location.EndsWith(@"\") || location.EndsWith(@"/"))
                location = location.Substring(0, location.Length - 1);

            return location;
        }

        /// <summary>
        /// A utility method for parsing KeyValue pair CommandOptions.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ParseKeyValueOption(string option)
        {
            var parameters = new Dictionary<string, string>();
            if (string.IsNullOrWhiteSpace(option))
                return parameters;

            try
            {
                int currentPos = 0;
                while (currentPos != -1 && currentPos < option.Length)
                {
                    string name;
                    GetNextToken(option, '=', ref currentPos, out name);

                    string value;
                    GetNextToken(option, ';', ref currentPos, out value);

                    if (string.IsNullOrEmpty(name))
                        throw new DockerToolsException($"Error parsing option ({option}), format should be <key1>=<value1>;<key2>=<value2>", DockerToolsException.ErrorCode.CommandLineParseError);

                    parameters[name] = value ?? string.Empty;
                }
            }
            catch (DockerToolsException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new DockerToolsException($"Error parsing option ({option}), format should be <key1>=<value1>;<key2>=<value2>: {e.Message}", DockerToolsException.ErrorCode.CommandLineParseError);
            }


            return parameters;
        }
        private static void GetNextToken(string option, char endToken, ref int currentPos, out string token)
        {
            if (option.Length <= currentPos)
            {
                token = string.Empty;
                return;
            }

            int tokenStart = currentPos;
            int tokenEnd = -1;
            bool inQuote = false;
            if (option[currentPos] == '"')
            {
                inQuote = true;
                tokenStart++;
                currentPos++;

                while (currentPos < option.Length && option[currentPos] != '"')
                {
                    currentPos++;
                }

                if (option[currentPos] == '"')
                    tokenEnd = currentPos;
            }

            while (currentPos < option.Length && option[currentPos] != endToken)
            {
                currentPos++;
            }


            if (!inQuote)
            {
                if (currentPos < option.Length && option[currentPos] == endToken)
                    tokenEnd = currentPos;
            }

            if (tokenEnd == -1)
                token = option.Substring(tokenStart);
            else
                token = option.Substring(tokenStart, tokenEnd - tokenStart);

            currentPos++;
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
                throw new DockerToolsException($"Error determing full repository path for the image {dockerImageTag}: {e.Message}", DockerToolsException.ErrorCode.FailedToExpandImageTag);
            }
        }
    }
}
