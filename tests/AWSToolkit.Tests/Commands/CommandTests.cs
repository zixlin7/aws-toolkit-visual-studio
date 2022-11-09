using Amazon.AWSToolkit.Commands;

using Xunit;

namespace AWSToolkit.Tests.Commands
{
    public class CommandTests
    {
        private TestCommand _sut = new TestCommand();

        [Fact]
        public void RaisesCanExecuteChangedWhenCanExecuteReturnChanges()
        {
            var canExecuteChangedRaised = false;

            _sut.CanExecuteChanged += (sender, e) => canExecuteChangedRaised = true;
            _sut.CanExecuteValue = true;
            _sut.CanExecute();

            Assert.True(canExecuteChangedRaised);
        }

        [Fact]
        public void DoesNotRaiseCanExecuteChangedWhenCanExecuteReturnSame()
        {
            var canExecuteChangedRaised = false;

            _sut.CanExecuteChanged += (sender, e) => canExecuteChangedRaised = true;
            _sut.CanExecuteValue = false;
            _sut.CanExecute();

            Assert.False(canExecuteChangedRaised);
        }

        [Fact]
        public void ExecutesWhenCanExecuteReturnsTrue()
        {
            _sut.CanExecuteValue = true;
            _sut.Execute();

            Assert.True(_sut.Executed);
        }

        [Fact]
        public void DoesNotExecuteWhenCanExecuteReturnsFalse()
        {
            _sut.CanExecuteValue = false;
            _sut.Execute();

            Assert.False(_sut.Executed);
        }
    }

    public class TestCommand : Command
    {
        public bool CanExecuteValue { get; set; }

        public bool Executed { get; set; }

        protected override bool CanExecuteCore(object parameter)
        {
            return CanExecuteValue;
        }

        protected override void ExecuteCore(object parameter)
        {
            Executed = true;
        }
    }
}
