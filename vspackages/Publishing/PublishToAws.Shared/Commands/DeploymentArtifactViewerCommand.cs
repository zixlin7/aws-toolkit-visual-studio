using System;
using System.Collections.Generic;
using System.Windows.Input;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public class DeploymentArtifactViewerCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        private readonly PublishToAwsDocumentViewModel _viewModel;
        private readonly IDictionary<DeploymentArtifact, ICommand> _commands;

        /// <summary>
        /// Creates a WPF command that opens a Deployment Artifact for viewing
        /// </summary>
        public static DeploymentArtifactViewerCommand Create(PublishToAwsDocumentViewModel viewModel)
        {
            return new DeploymentArtifactViewerCommand(viewModel,
                new Dictionary<DeploymentArtifact, ICommand>()
                {
                    {
                        DeploymentArtifact.BeanstalkEnvironment,
                        BeanstalkEnvironmentViewerCommandFactory.Create(viewModel)
                    },
                    {
                        DeploymentArtifact.CloudFormationStack,
                        StackViewerCommandFactory.Create(viewModel)
                    },
                });
        }

        public DeploymentArtifactViewerCommand(PublishToAwsDocumentViewModel viewModel, IDictionary<DeploymentArtifact, ICommand> commands)
        {
            _viewModel = viewModel;
            _commands = commands;
        }

        public bool CanExecute(object parameter)
        {
            return GetCommand()?.CanExecute(parameter) ?? false;
        }

        public void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                GetCommand()?.Execute(parameter);
            }

            RaiseCanExecuteChanged();
        }

        private ICommand GetCommand()
        {
            if (_viewModel.PublishDestination?.DeploymentArtifact == null)
            {
                return null;
            }

            if (_commands.TryGetValue(_viewModel.PublishDestination.DeploymentArtifact, out var command))
            {
                return command;
            }

            return null;
        }

        private void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
