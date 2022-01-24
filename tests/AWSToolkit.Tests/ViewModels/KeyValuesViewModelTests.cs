using System;
using System.Linq;

using Amazon.AWSToolkit.Models;
using Amazon.AWSToolkit.ViewModels;

using Xunit;

namespace AWSToolkit.Tests.ViewModels
{
    public class KeyValuesViewModelTests
    {
        private readonly KeyValuesViewModel _sut = new KeyValuesViewModel();

        public KeyValuesViewModelTests()
        {
            AddKeyValue("sample-key", "sample-value");
        }

        [Fact]
        public void KeyValuesUpdateBatchAssignments()
        {
            AddKeyValue("hello", "world");

            Assert.Equal(
                "sample-key=sample-value" + Environment.NewLine + "hello=world" + Environment.NewLine,
                _sut.BatchAssignments);
        }

        [Fact]
        public void KeyChangeUpdatesBatchAssignments()
        {
            _sut.Collection.First().Key = "key";
            Assert.Equal(
                "key=sample-value" + Environment.NewLine,
                _sut.BatchAssignments);
        }

        [Fact]
        public void ValueChangeUpdatesBatchAssignments()
        {
            _sut.Collection.First().Value = "value";
            Assert.Equal(
                "sample-key=value" + Environment.NewLine,
                _sut.BatchAssignments);
        }

        [Fact]
        public void BatchAssignmentsUpdateCollection()
        {
            _sut.BatchAssignments = "hello=world" + Environment.NewLine + "bees=5";
            Assert.Equal(2, _sut.Collection.Count);
            Assert.Contains(_sut.Collection, keyValue => keyValue.Key == "hello" && keyValue.Value == "world");
            Assert.Contains(_sut.Collection, keyValue => keyValue.Key == "bees" && keyValue.Value == "5");
        }

        [Fact]
        public void AddKeyValueCommand()
        {
            _sut.AddKeyValue.Execute(null);
            Assert.Equal(2, _sut.Collection.Count);
        }

        [Fact]
        public void RemoveKeyValueCommand()
        {
            _sut.RemoveKeyValue.Execute(_sut.Collection.First());
            Assert.Empty(_sut.Collection);
        }

        [Fact]
        public void IdentifyDuplicateKeys()
        {
            AddKeyValue("hi", "there");
            AddKeyValue("hi", "hello");
            AddKeyValue("hello", "world");

            Assert.True(_sut.Collection.Where(x => x.Key == "hi").All(x => x.IsDuplicateKey));
            Assert.True(_sut.Collection.Where(x => x.Key != "hi").All(x => !x.IsDuplicateKey));
            var errors = _sut.GetErrors(null).OfType<string>().ToList();
            Assert.Single(errors);
            Assert.Contains(errors, error => error.Contains("hi"));
        }

        private void AddKeyValue(string key, string value)
        {
            _sut.Collection.Add(new KeyValue(key, value));
        }
    }
}
