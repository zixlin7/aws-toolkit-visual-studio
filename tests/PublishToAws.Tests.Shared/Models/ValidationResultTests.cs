using System.Collections.Generic;

using Amazon.AWSToolkit.Publish.Models.Configuration;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class ValidationResultTests
    {
        private readonly ValidationResult _sut = new ValidationResult();
        private readonly string _sampleDetailId = "detail-id";
        private readonly string _sampleError = "error-message";

        [Fact]
        public void AddError()
        {
            _sut.AddError(_sampleDetailId, _sampleError);

            Assert.True(_sut.HasError(_sampleDetailId));
        }

        [Fact]
        public void HasError()
        {
            _sut.AddError(_sampleDetailId, _sampleError);

            Assert.True(_sut.HasError(_sampleDetailId));
        }

        [Fact]
        public void HasError_NoError()
        {
            Assert.False(_sut.HasError("qwerty"));
        }

        [Fact]
        public void GetError()
        {
            _sut.AddError(_sampleDetailId, _sampleError);

            Assert.Equal(_sampleError, _sut.GetError(_sampleDetailId));
        }

        [Fact]
        public void GetErrantDetailIds()
        {
            _sut.AddError(_sampleDetailId, _sampleError);

            Assert.Equal(new List<string>() { _sampleDetailId }, _sut.GetErrantDetailIds());
        }

        [Fact]
        public void HasErrors()
        {
            _sut.AddError(_sampleDetailId, _sampleError);

            Assert.True(_sut.HasErrors());
        }

        [Fact]
        public void HasErrors_NoError()
        {
            Assert.False(_sut.HasErrors());
        }
    }
}
