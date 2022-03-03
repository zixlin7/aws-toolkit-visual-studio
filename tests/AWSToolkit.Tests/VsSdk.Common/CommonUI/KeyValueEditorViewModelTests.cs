using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Models;

using CommonUI.Models;

using Xunit;

namespace AWSToolkit.Tests.VsSdk.Common.CommonUI
{
    public class KeyValueEditorViewModelTests
    {
        private KeyValueEditorViewModel _sut = new KeyValueEditorViewModel();

        [Fact]
        public void GetValidKeyValuesDoesNotReturnEmptyKeys()
        {
            KeyValue validKeyValue = new KeyValue("key1", "Has valid key name");

            _sut.SetKeyValues(new KeyValue[] {
                new KeyValue("", "Empty string is invalid key name"),
                validKeyValue, // Mix this in so it's not first or last that might let through a coding bug
                new KeyValue("   ", "Whitespace is invalid key name")
            });

            IEnumerable<KeyValue> returnedKeyValues = _sut.GetValidKeyValues();
            Assert.Single(returnedKeyValues, validKeyValue);
        }

        [Fact]
        public void NullReferenceExceptionThrownForNullKey()
        {
            // TODO This is really a side-effect.  KeyValueConversion.ToAssignmentString() should probably explicitly
            // throw NullArgumentException on a null key or else handle it gracefully if a null key is allowed in
            // other uses of that method, if any.  Tested here for completeness though as a null key in the context
            // of GetValidKeyValues wouldn't be returned, but it will never make it that far with the current code.
            Assert.Throws<NullReferenceException>(() => _sut.SetKeyValues(new KeyValue[] { new KeyValue(null, "Go boom!") }));
        }
    }
}
