using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Collections;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class CollectionExtensionMethodsTests
    {
        [Fact]
        public void AddsAllItemsInEnumerable()
        {
            ICollection<string> items = new List<string>();
            items.Add("item1");
            items.Add("item2");

            ICollection<string> toAdd = new List<string>();
            toAdd.Add("item3");
            toAdd.Add("item4");
            toAdd.Add("item5");

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");
            expected.Add("item4");
            expected.Add("item5");

            items.AddAll(toAdd);
            Assert.True(expected.SequenceEqual(items));
        }
    }
}
