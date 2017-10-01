using Amazon.ECS.Tools.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Amazon.ECR.Model;
using Amazon.ECR;

namespace Amazon.ECS.Tools.Commands
{
    public class PushDockerImageCommand : BaseCommand
    {
        public const string COMMAND_NAME = "push";
        public const string COMMAND_DESCRIPTION = "Execute \"dotnet publish\", \"docker build\" and then push the image to Amazon ECR.";

        public static readonly IList<CommandOption> CommandOptions = BuildLineOptions(new List<CommandOption>
        {
            DefinedCommandOptions.ARGUMENT_PROJECT_LOCATION,
            DefinedCommandOptions.ARGUMENT_CONFIGURATION,
            DefinedCommandOptions.ARGUMENT_FRAMEWORK,
            DefinedCommandOptions.ARGUMENT_DOCKER_TAG
        });


        public string Configuration { get; set; }
        public string TargetFramework { get; set; }
        public string DockerImageTag { get; set; }


        public string PushedImageUri { get; private set; }


        public PushDockerImageCommand(IToolLogger logger, string workingDirectory, string[] args)
            : base(logger, workingDirectory, CommandOptions, args)
        {
        }

        /// <summary>
        /// Parse the CommandOptions into the Properties on the command.
        /// </summary>
        /// <param name="values"></param>
        protected override void ParseCommandArguments(CommandOptions values)
        {
            base.ParseCommandArguments(values);

            Tuple<CommandOption, CommandOptionValue> tuple;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_CONFIGURATION.Switch)) != null)
                this.Configuration = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_FRAMEWORK.Switch)) != null)
                this.TargetFramework = tuple.Item2.StringValue;
            if ((tuple = values.FindCommandOption(DefinedCommandOptions.ARGUMENT_DOCKER_TAG.Switch)) != null)
                this.DockerImageTag = tuple.Item2.StringValue;
        }


        public override async Task<bool> ExecuteAsync()
        {
            try
            {
                string configuration = this.GetStringValueOrDefault(this.Configuration, DefinedCommandOptions.ARGUMENT_CONFIGURATION, false) ?? "Release";
                string targetFramework = this.GetStringValueOrDefault(this.TargetFramework, DefinedCommandOptions.ARGUMENT_FRAMEWORK, false);
                string dockerImageTag = this.GetStringValueOrDefault(this.DockerImageTag, DefinedCommandOptions.ARGUMENT_DOCKER_TAG, true);

                if (!dockerImageTag.Contains(":"))
                    dockerImageTag += ":latest";

                var projectLocation = Utilities.DetermineProjectLocation(this.WorkingDirectory, this.ProjectLocation);


                var dotnetCli = new DotNetCLIWrapper(this.Logger, projectLocation);
                this.Logger?.WriteLine("Executing publish command");
                if (dotnetCli.Publish(this.DefaultConfig, projectLocation, "obj/Docker/publish", targetFramework, configuration) != 0)
                {
                    throw new DockerToolsException("Error executing \"dotnet publish\"", DockerToolsException.ErrorCode.DotnetPublishFailed);
                }

                var dockerCli = new DockerCLIWrapper(this.Logger, projectLocation);
                this.Logger?.WriteLine("Executing docker build");
                if (dockerCli.Build(this.DefaultConfig, projectLocation, dockerImageTag) != 0)
                {
                    throw new DockerToolsException("Error executing \"docker build\"", DockerToolsException.ErrorCode.DockerBuildFailed);
                }

                await InitiateDockerLogin(dockerCli);

                Repository repository = await SetupECRRepository(dockerImageTag.Substring(0, dockerImageTag.IndexOf(':')));

                var targetTag = repository.RepositoryUri + dockerImageTag.Substring(dockerImageTag.IndexOf(':'));
                this.Logger?.WriteLine($"Taging image {dockerImageTag} with {targetTag}");
                if(dockerCli.Tag(dockerImageTag, targetTag) != 0)
                {
                    throw new DockerToolsException("Error executing \"docker tag\"", DockerToolsException.ErrorCode.DockerTagFail);
                }

                this.Logger?.WriteLine("Pushing image to ECR repository");
                if (dockerCli.Push(targetTag) != 0)
                {
                    throw new DockerToolsException("Error executing \"docker push\"", DockerToolsException.ErrorCode.DockerPushFail);
                }

                this.PushedImageUri = targetTag;
                this.Logger.WriteLine($"Image {this.PushedImageUri} Push Complete. ");
            }
            catch (DockerToolsException e)
            {
                this.Logger.WriteLine(e.Message);
                this.LastToolsException = e;
                return false;
            }
            catch (Exception e)
            {
                this.Logger.WriteLine($"Unknown error executing docker push to Amazon EC2 Container Registry: {e.Message}");
                this.Logger.WriteLine(e.StackTrace);
                return false;
            }
            return true;
        }

        private async Task<Repository> SetupECRRepository(string ecrRepositoryName)
        {
            try
            {
                DescribeRepositoriesResponse describeResponse = null;
                try
                {
                    describeResponse = await this.ECRClient.DescribeRepositoriesAsync(new DescribeRepositoriesRequest
                    {
                        RepositoryNames = new List<string> { ecrRepositoryName }
                    });
                }
                catch(Exception e)
                {
                    if (!(e is RepositoryNotFoundException))
                    {
                        throw;
                    }
                }

                Repository repository;
                if (describeResponse != null && describeResponse.Repositories.Count == 1)
                {
                    this.Logger?.WriteLine($"Found existing ECR Repository {ecrRepositoryName}");
                    repository = describeResponse.Repositories[0];
                }
                else
                {
                    this.Logger?.WriteLine($"Creating ECR Repository {ecrRepositoryName}");
                    repository = (await this.ECRClient.CreateRepositoryAsync(new CreateRepositoryRequest
                    {
                        RepositoryName = ecrRepositoryName
                    })).Repository;
                }

                return repository;
            }
            catch(Exception e)
            {
                throw new DockerToolsException($"Error determining Amazon ECR repository: {e.Message}", DockerToolsException.ErrorCode.FailedToSetupECRRepository);
            }
        }

        private async Task InitiateDockerLogin(DockerCLIWrapper dockerCLI)
        {
            try
            {
                this.Logger?.WriteLine("Fetching ECR authorization token to use to login with the docker CLI");
                var response = await this.ECRClient.GetAuthorizationTokenAsync(new GetAuthorizationTokenRequest());

                var authTokenBytes = Convert.FromBase64String(response.AuthorizationData[0].AuthorizationToken);
                var authToken = Encoding.UTF8.GetString(authTokenBytes);
                var decodedTokens = authToken.Split(':');

                this.Logger?.WriteLine("Executing docker CLI login command");
                if (dockerCLI.Login(decodedTokens[0], decodedTokens[1], response.AuthorizationData[0].ProxyEndpoint) != 0)
                {
                    throw new DockerToolsException($"Error executing the docker login command", DockerToolsException.ErrorCode.DockerCLILoginFail);
                }
            }
            catch(DockerToolsException)
            {
                throw;
            }
            catch(Exception e)
            {
                throw new DockerToolsException($"Error logging on with the docker CLI: {e.Message}", DockerToolsException.ErrorCode.GetECRAuthTokens);
            }
        }
    }
}
