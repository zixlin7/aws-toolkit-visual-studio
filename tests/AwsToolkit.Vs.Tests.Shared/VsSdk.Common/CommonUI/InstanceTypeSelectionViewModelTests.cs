using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Repositories;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using CommonUI.Models;

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.CommonUI
{
    public class InstanceTypeSelectionViewModelTests
    {
        private readonly InstanceTypeSelectionViewModel _sut;
        private readonly Mock<IInstanceTypeRepository> _instanceTypeRepository = new Mock<IInstanceTypeRepository>();

        private readonly FakeCredentialIdentifier _sampleCredentialsId = FakeCredentialIdentifier.Create("fake-profile");
        private readonly ToolkitRegion _sampleToolkitRegion = new ToolkitRegion();
        private readonly List<InstanceTypeModel> _instanceTypes = new List<InstanceTypeModel>();

        public InstanceTypeSelectionViewModelTests()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            SetupListInstanceTypes();

            _sut = new InstanceTypeSelectionViewModel(_instanceTypeRepository.Object, taskContext.Factory);
            _sut.CredentialsId = _sampleCredentialsId;
            _sut.Region = _sampleToolkitRegion;
        }

        private void SetupListInstanceTypes()
        {
            _instanceTypeRepository.Setup(mock => mock.ListInstanceTypesAsync(_sampleCredentialsId, _sampleToolkitRegion))
                .ReturnsAsync(() => _instanceTypes);
        }

        [StaFact]
        public async Task RefreshInstanceTypesAsync()
        {
            int instanceTypeCount = 10;
            PopulateSampleInstanceTypes(instanceTypeCount);

            await _sut.RefreshInstanceTypesAsync();

            Assert.Equal(_instanceTypes, _sut.InstanceTypes);
        }

        [StaFact]
        public async Task RefreshInstanceTypesAsync_OneArchitectureRestriction()
        {
            int instanceTypeCount = 10;
            PopulateSampleInstanceTypes(instanceTypeCount);
            _instanceTypes[3].Architectures.Add("garbage");
            _instanceTypes[4].Architectures.Add("arm64");
            _instanceTypes[5].Architectures.Add("x86_64");
            _instanceTypes[6].Architectures.Add("arm64");
            _instanceTypes[6].Architectures.Add("x86_64");

            _sut.Architectures.Add("arm64");
            await _sut.RefreshInstanceTypesAsync();

            Assert.Equal(_instanceTypes.Where(t => t.Architectures.Contains("arm64")),
                _sut.InstanceTypes);
        }

        [StaFact]
        public async Task RefreshInstanceTypesAsync_TwoArchitectureRestrictions()
        {
            int instanceTypeCount = 10;
            PopulateSampleInstanceTypes(instanceTypeCount);
            _instanceTypes[3].Architectures.Add("garbage");
            _instanceTypes[4].Architectures.Add("arm64");
            _instanceTypes[5].Architectures.Add("x86_64");
            _instanceTypes[6].Architectures.Add("arm64");
            _instanceTypes[6].Architectures.Add("x86_64");

            _sut.Architectures.Add("arm64");
            _sut.Architectures.Add("x86_64");
            await _sut.RefreshInstanceTypesAsync();

            Assert.Equal(_instanceTypes.Where(t =>
                    t.Architectures.Contains("arm64") || t.Architectures.Contains("x86_64")),
                _sut.InstanceTypes);
        }

        public static IEnumerable<object[]> MatchingFilterContents =>
            new List<object[]>
            {
                new object[] { "hello", "hello" },
                new object[] { "hello", "he" },
                new object[] { "hello", "l" },
                new object[] { "hello", "H" },
                new object[] { "Hello", "h" },
            };

        [Theory]
        [MemberData(nameof(MatchingFilterContents))]
        public void IsObjectFiltered_MatchId(string candidateText, string filter)
        {
            var instanceType = CreateSampleInstanceTypeModel(candidateText);
            Assert.True(InstanceTypeSelectionViewModel.IsObjectFiltered(instanceType, filter));
        }

        [Theory]
        [MemberData(nameof(MatchingFilterContents))]
        public void IsObjectFiltered_MatchArchitectures(string candidateText, string filter)
        {
            var instanceType = CreateSampleInstanceTypeModel(string.Empty);
            instanceType.Architectures.Add(candidateText);
        
            Assert.True(InstanceTypeSelectionViewModel.IsObjectFiltered(instanceType, filter));
        }

        [Theory]
        [InlineData("arm z")]
        [InlineData("z arm")]
        [InlineData("86 z")]
        public void IsObjectFiltered_MultipleFilters(string filter)
        {
            var instanceType = CreateSampleInstanceTypeModel("zzz");
            instanceType.Architectures.Add("arm64");
            instanceType.Architectures.Add("x86_64");

            Assert.True(InstanceTypeSelectionViewModel.IsObjectFiltered(instanceType, filter));
        }

        public static IEnumerable<object[]> NonMatchingFilterContents =>
            new List<object[]>
            {
                new object[] { "hello", "hello!" },
                new object[] { "hello", "x" },
                new object[] { "hello", "Q" },
            };

        [Theory]
        [MemberData(nameof(NonMatchingFilterContents))]
        public void IsObjectFiltered_NoMatchId(string candidateText, string filter)
        {
            var instanceType = CreateSampleInstanceTypeModel(candidateText);
            Assert.False(InstanceTypeSelectionViewModel.IsObjectFiltered(instanceType, filter));
        }

        [Theory]
        [MemberData(nameof(NonMatchingFilterContents))]
        public void IsObjectFiltered_NoMatchArchitectures(string candidateText, string filter)
        {
            var instanceType = CreateSampleInstanceTypeModel(string.Empty);
            instanceType.Architectures.Add(candidateText);

            Assert.False(InstanceTypeSelectionViewModel.IsObjectFiltered(instanceType, filter));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void IsObjectFiltered_NoFilter(string filter)
        {
            var instanceType = CreateSampleInstanceTypeModel("hello-world");
            Assert.True(InstanceTypeSelectionViewModel.IsObjectFiltered(instanceType, filter));
        }

        [Theory]
        [InlineData("arm y")]
        [InlineData("y arm")]
        [InlineData("86 y")]
        [InlineData("z 32")]
        public void IsObjectFiltered_NoMatchMultipleFilters(string filter)
        {
            var instanceType = CreateSampleInstanceTypeModel("zzz");
            instanceType.Architectures.Add("arm64");
            instanceType.Architectures.Add("x86_64");

            Assert.False(InstanceTypeSelectionViewModel.IsObjectFiltered(instanceType, filter));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(3)]
        [InlineData(false)]
        public void IsObjectFiltered_NonInstanceTypeModel(object candidate)
        {
            Assert.False(InstanceTypeSelectionViewModel.IsObjectFiltered(candidate, "3"));
        }

        private void PopulateSampleInstanceTypes(int count)
        {
            Enumerable.Range(0, count)
                .ToList()
                .ForEach(i => _instanceTypes.Add(CreateSampleInstanceTypeModel($"sample-instance-type-{i}")));
        }

        private InstanceTypeModel CreateSampleInstanceTypeModel(string instanceTypeId)
        {
            return new InstanceTypeModel
            {
                Id = instanceTypeId,
                Architectures = new List<string>(),
            };
        }
    }
}
