﻿using System.Collections.Generic;
using Amazon.AWSToolkit;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using AWSDeployment;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class BaseBeanstalkDeploymentEngineTests
    {
        protected const string SampleRegionId = "us-east-1";

        protected readonly Mock<IAmazonIdentityManagementService> _iamClient = new Mock<IAmazonIdentityManagementService>();

        protected readonly Mock<DeploymentObserver> _observer = new Mock<DeploymentObserver>();

        public BaseBeanstalkDeploymentEngineTests()
        {
            _iamClient.Setup(mock => mock.CreateRole(It.IsAny<CreateRoleRequest>()))
                .Returns<CreateRoleRequest>(request => new CreateRoleResponse()
                    {Role = new Role() {RoleName = request.RoleName}});

            _iamClient.Setup(mock => mock.CreateInstanceProfile(It.IsAny<CreateInstanceProfileRequest>()))
                .Returns<CreateInstanceProfileRequest>(request => new CreateInstanceProfileResponse()
                {
                    InstanceProfile = new InstanceProfile() {InstanceProfileName = request.InstanceProfileName}
                });

            _iamClient.Setup(mock => mock.AddRoleToInstanceProfile(It.IsAny<AddRoleToInstanceProfileRequest>()))
                .Returns(new AddRoleToInstanceProfileResponse());
        }

        protected void GetInstanceProfileReturnsSomething()
        {
            _iamClient.Setup(mock => mock.GetInstanceProfile(It.IsAny<GetInstanceProfileRequest>()))
                .Returns(new GetInstanceProfileResponse()
                {
                    InstanceProfile = new InstanceProfile()
                });
        }

        protected void GetInstanceProfileThrows()
        {
            _iamClient.Setup(mock => mock.GetInstanceProfile(It.IsAny<GetInstanceProfileRequest>()))
                .Throws(new NoSuchEntityException(""));
        }

        protected void GetRoleReturnsSomething()
        {
            _iamClient.Setup(mock => mock.GetRole(It.IsAny<GetRoleRequest>()))
                .Returns(new GetRoleResponse()
                {
                    Role = new Role()
                });
        }

        protected void GetRoleThrows()
        {
            _iamClient.Setup(mock => mock.GetRole(It.IsAny<GetRoleRequest>()))
                .Throws(new NoSuchEntityException(""));
        }
    }

    public class ConfigureRoleAndProfileTests : BaseBeanstalkDeploymentEngineTests
    {
        [Fact]
        public void InstanceProfileAndRoleDoNotExist()
        {
            GetInstanceProfileThrows();
            GetRoleThrows();

            var roleOrProfileName = BeanstalkParameters.DefaultRoleName;
            var response = BeanstalkDeploymentEngine.ConfigureRoleAndProfile(
                _iamClient.Object,
                roleOrProfileName,
                SampleRegionId,
                _observer.Object);

            _iamClient.Verify(mock => mock.CreateRole(It.IsAny<CreateRoleRequest>()), Times.Once);
            _iamClient.Verify(mock => mock.CreateInstanceProfile(It.IsAny<CreateInstanceProfileRequest>()), Times.Once);

            Assert.Equal(roleOrProfileName, response);
        }

        [Fact]
        public void InstanceProfileExists()
        {
            GetInstanceProfileReturnsSomething();

            var roleOrProfileName = BeanstalkParameters.DefaultRoleName;
            var response = BeanstalkDeploymentEngine.ConfigureRoleAndProfile(
                _iamClient.Object,
                roleOrProfileName,
                SampleRegionId,
                _observer.Object);

            Assert.Equal(roleOrProfileName, response);
        }

        [Fact]
        public void RoleExists()
        {
            GetInstanceProfileThrows();
            GetRoleReturnsSomething();

            var roleOrProfileName = BeanstalkParameters.DefaultRoleName;
            var response = BeanstalkDeploymentEngine.ConfigureRoleAndProfile(
                _iamClient.Object,
                roleOrProfileName,
                SampleRegionId,
                _observer.Object);

            _iamClient.Verify(mock => mock.CreateRole(It.IsAny<CreateRoleRequest>()), Times.Never);
            _iamClient.Verify(mock => mock.CreateInstanceProfile(It.IsAny<CreateInstanceProfileRequest>()), Times.Once);

            Assert.Equal(roleOrProfileName, response);
        }
    }

    public class ConfigureServiceRoleTests : BaseBeanstalkDeploymentEngineTests
    {
        [Fact]
        public void RoleExists()
        {
            GetRoleReturnsSomething();

            var serviceRoleName = BeanstalkParameters.DefaultServiceRoleName;
            BeanstalkDeploymentEngine.ConfigureServiceRole(
                _iamClient.Object,
                serviceRoleName,
                SampleRegionId,
                _observer.Object);

            _iamClient.Verify(mock => mock.CreateRole(It.IsAny<CreateRoleRequest>()), Times.Never);
        }

        [Fact]
        public void RoleDoesNotExist()
        {
            GetRoleThrows();

            var serviceRoleName = BeanstalkParameters.DefaultServiceRoleName;
            BeanstalkDeploymentEngine.ConfigureServiceRole(
                _iamClient.Object,
                serviceRoleName,
                SampleRegionId,
                _observer.Object);

            _iamClient.Verify(mock => mock.CreateRole(It.IsAny<CreateRoleRequest>()), Times.Once);
            _iamClient.Verify(
                mock => mock.AttachRolePolicy(It.Is<AttachRolePolicyRequest>(request =>
                    request.PolicyArn == RolePolicies.ServiceRoleArns.AWSElasticBeanstalkManagedUpdatesCustomerRolePolicy)), Times.Once);
            _iamClient.Verify(
                mock => mock.AttachRolePolicy(It.Is<AttachRolePolicyRequest>(request =>
                    request.PolicyArn == RolePolicies.ServiceRoleArns.AWSElasticBeanstalkEnhancedHealth)), Times.Once);
        }
    }
}
