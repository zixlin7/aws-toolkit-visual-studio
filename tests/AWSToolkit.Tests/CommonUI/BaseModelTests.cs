using System;

using Amazon.AWSToolkit.CommonUI;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class BaseModelTests
    {
        public class TestModel : BaseModel
        {
            private string _value;

            public string ValueUsingImpliedSetter
            {
                get => _value;
                set => SetProperty(ref _value, value);
            }

            public string ValueUsingMemberExpression
            {
                get => _value;
                set => SetProperty(ref _value, value, () => ValueUsingMemberExpression);
            }
        }

        private readonly TestModel _sut = new TestModel();
        private readonly string _sampleValue = Guid.NewGuid().ToString();

        [Fact]
        public void SetProperty_ImpliedPropertyName()
        {
            Assert.PropertyChanged(_sut,
                nameof(TestModel.ValueUsingImpliedSetter),
                () => _sut.ValueUsingImpliedSetter = _sampleValue);

            Assert.Equal(_sampleValue, _sut.ValueUsingImpliedSetter);
        }

        [Fact]
        public void SetProperty_MemberExpression()
        {
            Assert.PropertyChanged(_sut,
                nameof(TestModel.ValueUsingMemberExpression),
                () => _sut.ValueUsingMemberExpression = _sampleValue);

            Assert.Equal(_sampleValue, _sut.ValueUsingMemberExpression);
        }
    }
}
