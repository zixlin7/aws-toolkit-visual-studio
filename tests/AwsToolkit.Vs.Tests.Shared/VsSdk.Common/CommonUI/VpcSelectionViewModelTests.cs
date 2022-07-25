using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tests.Common.Context;

using CommonUI.Models;

using Microsoft.VisualStudio.Threading;

using Moq;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.CommonUI
{
    public class VpcSelectionViewModelTests
    {
        private readonly VpcSelectionViewModel _sut;
        private readonly Mock<IVpcRepository> _vpcRepository = new Mock<IVpcRepository>();

        private readonly FakeCredentialIdentifier _sampleCredentialsId = FakeCredentialIdentifier.Create("fake-profile");
        private readonly ToolkitRegion _sampleToolkitRegion = new ToolkitRegion();
        private readonly List<VpcEntity> _vpcs = new List<VpcEntity>();

        public VpcSelectionViewModelTests()
        {
#pragma warning disable VSSDK005 // ThreadHelper.JoinableTaskContext requires VS Services from a running VS instance
            var taskContext = new JoinableTaskContext();
#pragma warning restore VSSDK005

            SetupListVpcs();

            _sut = new VpcSelectionViewModel(_vpcRepository.Object, taskContext.Factory);
            _sut.CredentialsId = _sampleCredentialsId;
            _sut.Region = _sampleToolkitRegion;
        }

        private void SetupListVpcs()
        {
            _vpcRepository.Setup(mock => mock.ListVpcsAsync(_sampleCredentialsId, _sampleToolkitRegion))
                .ReturnsAsync(() => _vpcs);
        }

        [StaFact]
        public async Task RefreshVpcsAsync()
        {
            int vpcCount = 10;
            PopulateSampleVpcs(vpcCount);

            await _sut.RefreshVpcsAsync();

            Assert.Equal(_vpcs, _sut.Vpcs);
        }

        private void PopulateSampleVpcs(int count)
        {
            Enumerable.Range(0, count)
                .ToList()
                .ForEach(i => _vpcs.Add(CreateSampleVpcEntity($"sample-vpc-id-{i}")));
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
            var entity = CreateSampleVpcEntity(candidateText);
            Assert.True(VpcSelectionViewModel.IsObjectFiltered(entity, filter));
        }

        [Theory]
        [MemberData(nameof(MatchingFilterContents))]
        public void IsObjectFiltered_MatchDescription(string candidateText, string filter)
        {
            var entity = CreateSampleVpcEntity(string.Empty);
            entity.Name = candidateText;

            Assert.True(VpcSelectionViewModel.IsObjectFiltered(entity, filter));
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
            var entity = CreateSampleVpcEntity(candidateText);
            Assert.False(VpcSelectionViewModel.IsObjectFiltered(entity, filter));
        }

        [Theory]
        [MemberData(nameof(NonMatchingFilterContents))]
        public void IsObjectFiltered_NoMatchDescription(string candidateText, string filter)
        {
            var entity = CreateSampleVpcEntity(string.Empty);
            entity.Name = candidateText;

            Assert.False(VpcSelectionViewModel.IsObjectFiltered(entity, filter));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void IsObjectFiltered_NoFilter(string filter)
        {
            var entity = CreateSampleVpcEntity("hello-world");
            Assert.True(VpcSelectionViewModel.IsObjectFiltered(entity, filter));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(3)]
        [InlineData(false)]
        public void IsObjectFiltered_NonVpcEntity(object candidate)
        {
            Assert.False(VpcSelectionViewModel.IsObjectFiltered(candidate, "3"));
        }

        private VpcEntity CreateSampleVpcEntity(string vpcId)
        {
            return new VpcEntity
            {
                Id = vpcId,
            };
        }
    }
}
