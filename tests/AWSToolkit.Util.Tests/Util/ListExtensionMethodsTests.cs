using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Util
{
    public class Split
    {
        private readonly IList<int> _sampleList = new ReadOnlyCollection<int>(Enumerable.Range(1, 6).ToList());

        [Fact]
        public void ReturnsSplitList()
        {
            var chunks = _sampleList.Split(3);
            Assert.Equal(2, chunks.Count);
            Assert.True(chunks.All(list => list.Count == 3));
        }

        [Fact]
        public void HandlesRemainders()
        {
            var chunks = _sampleList.Split(4);
            Assert.Equal(2, chunks.Count);
            Assert.True(chunks[0].Count == 4);
            Assert.True(chunks[1].Count == 2);
        }

        [Fact]
        public void MaintainsOrder()
        {
            var chunks = _sampleList.Split(3);
            Assert.Equal(_sampleList[0], chunks[0][0]);
            Assert.Equal(_sampleList[1], chunks[0][1]);
            Assert.Equal(_sampleList[2], chunks[0][2]);
            Assert.Equal(_sampleList[3], chunks[1][0]);
            Assert.Equal(_sampleList[4], chunks[1][1]);
            Assert.Equal(_sampleList[5], chunks[1][2]);
        }

        [Fact]
        public void HandlesEmpty()
        {
            var chunks = new List<int>().Split(3);
            Assert.Equal(0, chunks.Count);
        }

        [Fact]
        public void HandlesNull()
        {
            List<int> list = null;

            var chunks = list.Split(3);
            Assert.Equal(0, chunks.Count);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ThrowsOnBadInput(int badSplitSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _sampleList.Split(badSplitSize));
        }
    }
}