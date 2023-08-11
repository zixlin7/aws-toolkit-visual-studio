using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard;
using Amazon.AWSToolkit.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CommonUI.CredentialProfiles.AddEditWizard
{
    internal class TestRootViewModel : RootViewModel
    {
        internal TestRootViewModel()
            : base(new ToolkitContext()) { }
    }

    public class ViewModelTests
    {
        private readonly Mock<ViewModel> _mockSut;

        private ViewModel _sut => _mockSut.Object;

        public ViewModelTests()
        {
            _mockSut = new Mock<ViewModel>()
            {
                CallBase = true
            };

            _sut.ServiceProvider = new ServiceProvider();
        }

        [Fact]
        public void EnsureRootViewModelCreatesServiceProviderAndRegistersToolkitContextInCtor()
        {
            var sut = new TestRootViewModel();

            var actual = sut.ServiceProvider.RequireService<ToolkitContext>();
            Assert.NotNull(actual);
        }

        [Fact]

        public async Task RegisterServicesAsyncThrowsWhenServiceProviderNotSet()
        {
            _sut.ServiceProvider = null;
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _sut.RegisterServicesAsync());
        }

        public static IEnumerable<object[]> CallLifecycleMethodCallsCorrectMethodsData()
        {
            object[] data(Expression<Func<ViewModel, Task>> func) => new object[] { func };

            return new List<object[]>()
            {
                data(vm => vm.RegisterServicesAsync()),
                data(vm => vm.InitializeAsync()),
                data(vm => vm.ViewLoadedAsync()),
                data(vm => vm.ViewShownAsync()),
                data(vm => vm.ViewHiddenAsync()),
                data(vm => vm.ViewUnloadedAsync())
            };
        }

        [Theory]
        [MemberData(nameof(CallLifecycleMethodCallsCorrectMethodsData))]
        public async void CallLifecycleMethodCallsCorrectMethods(Expression<Func<ViewModel, Task>> func)
        {
            _mockSut.Verify(func, Times.Never());
            await func.Compile()(_sut);
            _mockSut.Verify(func, Times.Once());
        }
    }
}
