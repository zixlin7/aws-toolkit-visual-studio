using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models.Configuration;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class ListConfigurationDetailTests
    {
        private static readonly string Value = "[\"value1\",\"value2\"]";

        private static readonly IDictionary<string, string> ValueMappings = new Dictionary<string, string>();

        private static readonly ICollection<ListConfigurationDetail.ListItem> Items =
            new List<ListConfigurationDetail.ListItem>();

        private static readonly ICollection<ListConfigurationDetail.ListItem> SelectedItems =
            new List<ListConfigurationDetail.ListItem>();

        static ListConfigurationDetailTests()
        {
            ValueMappings.Add("display 1", "value1");
            ValueMappings.Add("display 2", "value2");
            ValueMappings.Add("display 3", "value3");
            ValueMappings.Add("display 4", "value4");
            ValueMappings.Add("display 5", "value5");

            Items.Add(new ListConfigurationDetail.ListItem("display 1", "value1"));
            Items.Add(new ListConfigurationDetail.ListItem("display 2", "value2"));
            Items.Add(new ListConfigurationDetail.ListItem("display 3", "value3"));
            Items.Add(new ListConfigurationDetail.ListItem("display 4", "value4"));
            Items.Add(new ListConfigurationDetail.ListItem("display 5", "value5"));

            SelectedItems.Add(new ListConfigurationDetail.ListItem("display 1", "value1"));
            SelectedItems.Add(new ListConfigurationDetail.ListItem("display 2", "value2"));
        }

        private readonly ListConfigurationDetail _sut = new ListConfigurationDetail();

        [Fact]
        public void ValueMappingsBecomeItems()
        {
            _sut.ValueMappings = ValueMappings;
            Assert.True(Items.SequenceEqual(_sut.Items));
        }

        [Fact]
        public void ValueBecomesSelectedItems()
        {
            _sut.Items = Items;
            _sut.Value = Value;
            Assert.True(SelectedItems.SequenceEqual(_sut.SelectedItems));
        }

        [Fact]
        public void SelectedItemsUpdateValue()
        {
            // Intentionally swapping order of setting Items/SelectedItems to verify either order works
            _sut.SelectedItems = SelectedItems;
            _sut.Items = Items;
            _sut.UpdateListValues();
            Assert.Equal(Value, _sut.Value);
        }
    }
}
