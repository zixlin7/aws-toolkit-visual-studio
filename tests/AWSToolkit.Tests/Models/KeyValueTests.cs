using System.ComponentModel;
using System.Linq;

using Amazon.AWSToolkit.Models;

using Xunit;

namespace AWSToolkit.Tests.Models
{
    public class KeyValueTests
    {
        private readonly KeyValue _sut = new KeyValue() { Key = "sample-key", Value = "sample-value", };

        [Fact]
        public void IsDuplicateKeyRaisesErrorsChanged()
        {
            Assert.Raises<DataErrorsChangedEventArgs>(
                handler => _sut.ErrorsChanged += handler,
                handler => _sut.ErrorsChanged -= handler,
                () => _sut.IsDuplicateKey = true);

        }

        [Fact]
        public void Equals_SameObject()
        {
            Assert.True(_sut.Equals(_sut));
        }

        [Fact]
        public void Equals_SameObjectValues()
        {
            var other = new KeyValue() { Key = _sut.Key, Value = _sut.Value };
            Assert.True(_sut.Equals(other));
        }

        [Fact]
        public void Equals_DifferentObject()
        {
            var other = new KeyValue();
            Assert.False(_sut.Equals(other));
            Assert.False(_sut == other);
        }

        [Fact]
        public void HasErrors_DuplicateKey()
        {
            _sut.IsDuplicateKey = true;
            Assert.True(_sut.HasErrors);
        }

        [Fact]
        public void HasErrors_NoDuplicateKey()
        {
            _sut.IsDuplicateKey = false;
            Assert.False(_sut.HasErrors);
        }

        [Theory]
        [InlineData(null)]
        [InlineData(nameof(KeyValue.Key))]
        public void GetErrors_DuplicateKey(string propertyName)
        {
            _sut.IsDuplicateKey = true;
            var errors = _sut.GetErrors(propertyName).OfType<string>().ToList();
            Assert.NotEmpty(errors);
            Assert.Contains<string>(errors, x => x.Contains(_sut.Key));
        }

        [Theory]
        [InlineData(null)]
        [InlineData(nameof(KeyValue.Key))]
        public void GetErrors_NoDuplicateKey(string propertyName)
        {
            _sut.IsDuplicateKey = false;
            var errors = _sut.GetErrors(propertyName);
            Assert.Empty(errors);
        }
    }
}
