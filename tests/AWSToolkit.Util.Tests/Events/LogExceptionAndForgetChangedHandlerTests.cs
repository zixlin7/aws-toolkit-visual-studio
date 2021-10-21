using System.ComponentModel;
using System.IO;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Events;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Events
{
    public class LogExceptionAndForgetChangedHandlerTests
    {
        public class SimpleModel : BaseModel
        {
            private int _number;

            public int Number
            {
                get => _number;
                set => SetProperty(ref _number, value);
            }
        }

        private readonly SimpleModel _model = new SimpleModel();

        [Fact]
        public void ShouldForgetException()
        {
            _model.PropertyChanged += LogExceptionAndForgetChangedHandler.Create(ThrowingHandlePropertyChanged);
            _model.Number = 100;
        }

        private static void ThrowingHandlePropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            throw new IOException("This should be forgotten.");
        }

        private bool _called = false;

        [Fact]
        public void ShouldUnsubscribeEventHandler()
        {
            // arrange.
            var handler = LogExceptionAndForgetChangedHandler.Create(SpyHandlePropertyChanged);

            // act.
            _model.PropertyChanged += handler;
            _model.PropertyChanged -= handler;

            _model.Number = 100;

            // assert.
            Assert.False(_called);
        }

        private void SpyHandlePropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            _called = true;
        }

        [Fact]
        public void ShouldNotUnsubscribeEventHandler()
        {
            // act.
            _model.PropertyChanged += LogExceptionAndForgetChangedHandler.Create(SpyHandlePropertyChanged);
            _model.PropertyChanged -= LogExceptionAndForgetChangedHandler.Create(SpyHandlePropertyChanged);

            _model.Number = 100;

            // assert.
            Assert.True(_called);
        }
    }
}
