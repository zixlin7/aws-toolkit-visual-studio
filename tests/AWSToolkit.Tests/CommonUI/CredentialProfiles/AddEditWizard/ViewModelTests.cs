using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

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

        public static async Task<T> BootstrapViewModel<T>(ServiceProvider serviceProvider = null) where T : ViewModel, new()
        {
            var viewModel = new T
            {
                ServiceProvider = serviceProvider ?? new ServiceProvider()
            };
            await viewModel.RegisterServicesAsync();
            await viewModel.InitializeAsync();

            return viewModel;
        }

        [Theory]
        [InlineData(new object[] { true, null, Visibility.Visible })]
        [InlineData(new object[] { false, null, Visibility.Collapsed })]
        [InlineData(new object[] { true, "Hidden", Visibility.Visible })]
        [InlineData(new object[] { false, "Hidden", Visibility.Hidden })]
        [InlineData(new object[] { true, "Hidden,Collapsed", Visibility.Collapsed })]
        [InlineData(new object[] { false, "Hidden,Collapsed", Visibility.Hidden })]
        public void BoolToVisibilityReturnsExpectedResults(bool value, object parameter, Visibility expected)
        {
            var actual = ViewModel.BoolToVisibility(value, typeof(Visibility), parameter, null);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(new object[] { "Collapsed", Visibility.Collapsed })]
        [InlineData(new object[] { "Hidden", Visibility.Hidden })]
        [InlineData(new object[] { "Visible", Visibility.Visible })]
        public void TryParseAndSetWithValidValues(string value, Visibility expected)
        {
            var storage = new Visibility[10];
            var index = new Random().Next() % storage.Length;

            var parsed = ViewModel.TryParseAndSet(value, ref storage[index]);

            Assert.True(parsed);
            Assert.Equal(expected, storage[index]);
        }

        [Theory]
        [InlineData(new object[] { "collapsed" })]
        [InlineData(new object[] { "Hidden," })]
        [InlineData(new object[] { "HotGarbage" })]
        [InlineData(new object[] { null })]
        [InlineData(new object[] { "" })]
        public void TryParseAndSetWithInvalidValues(string value)
        {
            var storage = Visibility.Visible;

            var parsed = ViewModel.TryParseAndSet(value, ref storage);

            Assert.False(parsed);
            Assert.Equal(Visibility.Visible, storage);
        }
    }
}
