using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Amazon.AWSToolkit.CommonUI;

using Xunit;
using Xunit.Sdk;

namespace AWSToolkit.Tests.CommonUI
{
    public class DataErrorInfoTests
    {
        private readonly DataErrorInfo _sut;

        private readonly IDataErrorInfo _sutAsIDataErrorInfo;

        public DataErrorInfoTests()
        {
            _sut = new DataErrorInfo(this);
            _sutAsIDataErrorInfo = _sut;
        }

        [Fact]
        public void HasErrorsReturnsTrueWithErrorsAndFalseWithout()
        {
            Assert.False(_sut.HasErrors);
            _sut.AddError("Test Error");
            Assert.True(_sut.HasErrors);
        }

        [Fact]
        public void HasErrorsReturnsFalseWhenHadErrorsButAllCleared()
        {
            Assert.False(_sut.HasErrors);

            _sut.AddError("Test Error");

            Assert.True(_sut.HasErrors);

            _sut.ClearErrors();

            Assert.False(_sut.HasErrors);
        }

        [Fact]
        public void HasErrorsReturnsFalseWhenHadErrorsButAllRemoved()
        {
            const string error = "Error";

            Assert.False(_sut.HasErrors);

            _sut.AddError(error, null, "Property1", "Property2");

            Assert.True(_sut.HasErrors);

            _sut.RemoveError(error);

            Assert.False(_sut.HasErrors);
        }

        [Fact]
        public void GetErrorsAtEntityLevelWorksForNullEmpty()
        {
            const string error = "Entity Error";

            Assert.False(_sut.HasErrors);

            _sut.AddError(error);

            Assert.True(_sut.HasErrors);

            Assert.Collection(_sut.GetErrors(null).Cast<string>(), s => Assert.Equal(error, s));
            Assert.Collection(_sut.GetErrors(string.Empty).Cast<string>(), s => Assert.Equal(error, s));
        }

        [Fact]
        public void GetErrorsReturnsNoDuplicatesAtEntityLevel()
        {
            const string error = "Entity Error";

            Assert.False(_sut.HasErrors);

            _sut.AddError(error);
            _sut.AddError(error, null);
            _sut.AddError(error, null, null);
            _sut.AddError(error, string.Empty);
            _sut.AddError(error, null, string.Empty);

            Assert.True(_sut.HasErrors);

            Assert.Equal(error, _sutAsIDataErrorInfo.Error);
            Assert.Equal(error, _sutAsIDataErrorInfo[null]);
            Assert.Equal(error, _sutAsIDataErrorInfo[string.Empty]);

            Assert.Single(_sut.GetErrors(null));
            Assert.Collection(_sut.GetErrors(string.Empty).Cast<string>(), s => Assert.Equal(error, s));
        }

        [Fact]
        public void GetErrorsReturnsNoDuplicatesAtPropertyLevel()
        {
            const string error = "Property Error";

            Assert.False(_sut.HasErrors);

            _sut.AddError(error, "Property1");
            _sut.AddError(error, "Property1");
            _sut.AddError(error, "Property2");

            Assert.True(_sut.HasErrors);

            AssertOnlyHasCrossPropertyError(error, "Property1", "Property2");
        }

        [Fact]
        public void AddingCrossPropertyErrorsIsRetrievablePerProperty()
        {
            const string error = "Cross-property Error";

            Assert.False(_sut.HasErrors);

            _sut.AddError(error, "Property1", "Property2", "Property3");

            Assert.True(_sut.HasErrors);

            AssertOnlyHasCrossPropertyError(error, "Property1", "Property2", "Property3");
        }

        [Fact]
        public void FailedAssertionsInCallbacksResultInFailedTests()
        {
            // This test doesn't directly validate DataErrorInfo, but rather confirms assumptions about how assertions behave
            // in callbacks to give confidence in tests that utilize assertions in callbacks.

            const string userMessage = "Can you believe it!?  This is the expected exception!  No doubt about it.  Nobody would throw a real exception with such a pointless message.";

            _sut.ErrorsChanged += (sender, e) =>
            {
                Assert.True(false, userMessage);
            };

            var ex = Assert.Throws<TrueException>(() => _sut.AddError("Error"));
            Assert.Equal(userMessage, ex.Message);
        }

        [Fact]
        public void OnErrorsChangedFires()
        {
            string expected = null;

            _sut.ErrorsChanged += (sender, e) =>
            {
                Assert.Equal(this, sender);
                Assert.Equal(expected, e.PropertyName);
            };

            Assert.False(_sut.HasErrors);

            expected = "";
            _sut.AddError("Error", null);

            expected = "";
            _sut.AddError("Error", string.Empty);

            expected = "Property1";
            _sut.AddError("Error", "Property1");

            expected = "Property2";
            _sut.AddError("Error", "Property2");

            Assert.True(_sut.HasErrors);
        }

        [Fact]
        public void OnErrorsChangedFiresForCrossPropertyErrors()
        {
            const string error = "Error";
            var expected = new List<string>() { "Property1", "Property2", "Property3" };

            _sut.ErrorsChanged += (sender, e) =>
            {
                Assert.Equal(this, sender);
                Assert.Contains(e.PropertyName, expected);

                expected.Remove(e.PropertyName);
            };

            Assert.False(_sut.HasErrors);

            _sut.AddError(error, "Property1", "Property2", "Property3");

            Assert.True(_sut.HasErrors);
        }

        [Fact]
        public void RemoveErrorRemovesForSingleAndCrossPropertyErrors()
        {
            const string error = "Error";

            Assert.False(_sut.HasErrors);

            _sut.AddError(error, null, "Property1", "Property2", "Property3");

            Assert.True(_sut.HasErrors);

            AssertOnlyHasCrossPropertyError(error, string.Empty, "Property1", "Property2", "Property3");

            _sut.RemoveError(error);

            Assert.Equal(string.Empty, _sutAsIDataErrorInfo.Error);
            Assert.Empty(_sut.GetErrors(null));
            Assert.Empty(_sut.GetErrors("Property1"));
            Assert.Empty(_sut.GetErrors("Property2"));
            Assert.Empty(_sut.GetErrors("Property3"));

            Assert.False(_sut.HasErrors);
        }

        [Fact]
        public void ClearErrorsRemovesAllErrors()
        {
            const string error = "Error";

            Assert.False(_sut.HasErrors);

            _sut.AddError(error, null, "Property1", "Property2", "Property3");

            Assert.True(_sut.HasErrors);

            _sut.ClearErrors();

            Assert.Empty(_sut.GetErrors(null));

            Assert.False(_sut.HasErrors);
        }

        [Fact]
        public void ClearErrorsRemovesErrorsForSpecificProperties()
        {
            const string error = "Error";

            Assert.False(_sut.HasErrors);

            _sut.AddError(error, null, "Property1", "Property2", "Property3");

            Assert.True(_sut.HasErrors);

            _sut.ClearErrors("Property1", "Property3");

            Assert.Single(_sut.GetErrors(null));
            Assert.Contains(error, _sut.GetErrors(string.Empty).OfType<string>());
            Assert.Empty(_sut.GetErrors("Property1"));
            Assert.Single(_sut.GetErrors("Property2"));
            Assert.Contains(error, _sut.GetErrors("Property2").OfType<string>());
            Assert.Empty(_sut.GetErrors("Property3"));

            Assert.True(_sut.HasErrors);
        }

        private void AssertOnlyHasCrossPropertyError(object error, params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                Assert.Single(_sut.GetErrors(propertyName));
                Assert.Collection(_sut.GetErrors(propertyName).Cast<string>(), s => Assert.Equal(error, s));
                Assert.Equal(error, _sutAsIDataErrorInfo[propertyName]);
            }
        }
    }
}
