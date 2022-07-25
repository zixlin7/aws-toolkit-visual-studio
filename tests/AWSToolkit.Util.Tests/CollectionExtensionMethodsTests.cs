using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Collections;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests
{
    /// <summary>
    /// Tests extension methods that provides additional functionality to collection objects.
    /// </summary>
    /// <remarks>
    /// These methods may behave differently based on the underlying collection being executed
    /// against.  For example, lists maintain order, sets maintain uniqueness.  The Add/Remove
    /// implementations specific to certain objects can change the outcome.  These tests do not
    /// attempt to verify all possible behavioral side-effects of various collection types.
    /// </remarks>
    public class CollectionExtensionMethodsTests
    {
        public readonly List<string> _sut = new List<string>() { "item1", "item2", "item3" };

        public static readonly object[][] EmptyEnumerables = { new object[] { null }, new object[] { new List<string>() } };

        #region AddAll(this ICollection<>)
        [Fact]
        public void AddsAllItemsInEnumerableToCollection()
        {
            ICollection<string> sut = _sut;

            ICollection<string> toAdd = new List<string>();
            toAdd.Add("item4");
            toAdd.Add("item5");

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");
            expected.Add("item4");
            expected.Add("item5");

            sut.AddAll(toAdd);
            Assert.True(expected.SequenceEqual(sut));
        }

        [Theory]
        [MemberData(nameof(EmptyEnumerables))]
        public void AddAllWithEmptyEnumerableReturnsOriginalCollection(object emptyEnumerable)
        {
            ICollection<string> sut = _sut;

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");

            sut.AddAll((IEnumerable<string>) emptyEnumerable);
            Assert.True(expected.SequenceEqual(sut));
        }
        #endregion

        #region AddAll(this IList)
        [Fact]
        public void AddsAllItemsInEnumerableToList()
        {
            IList sut = _sut;

            IList toAdd = new List<string>();
            toAdd.Add("item4");
            toAdd.Add("item5");

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");
            expected.Add("item4");
            expected.Add("item5");

            sut.AddAll(toAdd);
            Assert.True(expected.SequenceEqual(sut.Cast<string>()));
        }

        [Theory]
        [MemberData(nameof(EmptyEnumerables))]
        public void AddAllWithEmptyEnumerableReturnsOriginalList(object emptyEnumerable)
        {
            IList sut = _sut;

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");

            sut.AddAll((IEnumerable) emptyEnumerable);
            Assert.True(expected.SequenceEqual(sut.Cast<string>()));
        }
        #endregion

        #region RemoveAll(this ICollection<>)
        [Fact]
        public void RemovesAllItemsInEnumerableToCollection()
        {
            ICollection<string> sut = _sut;

            ICollection<string> toRemove = new List<string>();
            toRemove.Add("item1");
            toRemove.Add("item3");

            ICollection<string> expected = new List<string>();
            expected.Add("item2");

            sut.RemoveAll(toRemove);
            Assert.True(expected.SequenceEqual(sut));
        }

        [Theory]
        [MemberData(nameof(EmptyEnumerables))]
        public void RemoveAllWithEmptyEnumerableReturnsOriginalCollection(object emptyEnumerable)
        {
            ICollection<string> sut = _sut;

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");

            sut.RemoveAll((IEnumerable<string>) emptyEnumerable);
            Assert.True(expected.SequenceEqual(sut));
        }

        [Fact]
        public void RemoveAllWithValueNotInCollectionDoesNotThrowException()
        {
            ICollection<string> sut = _sut;

            ICollection<string> toRemove = new List<string>();
            toRemove.Add("This is nowhere to be found");

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");

            sut.RemoveAll(toRemove);
            Assert.True(expected.SequenceEqual(sut));
        }
        #endregion

        #region RemoveAll(this IList)
        [Fact]
        public void RemovesAllItemsInEnumerableToList()
        {
            IList sut = _sut;

            IList toRemove = new List<string>();
            toRemove.Add("item2");
            toRemove.Add("item3");

            ICollection<string> expected = new List<string>();
            expected.Add("item1");

            sut.RemoveAll(toRemove);
            Assert.True(expected.SequenceEqual(sut.Cast<string>()));
        }

        [Theory]
        [MemberData(nameof(EmptyEnumerables))]
        public void RemoveAllWithEmptyEnumerableReturnsOriginalList(object emptyEnumerable)
        {
            IList sut = _sut;

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");

            sut.RemoveAll((IEnumerable) emptyEnumerable);
            Assert.True(expected.SequenceEqual(sut.Cast<string>()));
        }

        [Fact]
        public void RemoveAllWithValueNotInListDoesNotThrowException()
        {
            IList sut = _sut;

            IList toRemove = new List<string>();
            toRemove.Add("This is nowhere to be found");

            ICollection<string> expected = new List<string>();
            expected.Add("item1");
            expected.Add("item2");
            expected.Add("item3");

            sut.RemoveAll(toRemove);
            Assert.True(expected.SequenceEqual(sut.Cast<string>()));
        }
        #endregion
    }
}
