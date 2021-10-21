using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

using Amazon.AWSToolkit.Publish.Models;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class PublishResourceTemplateSelectorTests
    {
        private readonly PublishResourceTemplateSelector _sut = new PublishResourceTemplateSelector()
        {
            LinkEditor = new DataTemplate(),
            TextEditor = new DataTemplate(),
        };

        [Theory]
        [InlineData(null)]
        [InlineData(5)]
        [InlineData(false)]
        public void SelectTemplate_NotAString(object item)
        {
            Assert.Null(_sut.SelectTemplate(item, null));
        }

        [Fact]
        public void SelectTemplate_Text()
        {
            Assert.Equal(_sut.TextEditor, _sut.SelectTemplate("hello world", null));
        }

        [Theory]
        [InlineData("http://www.amazon.com")]
        [InlineData("https://www.amazon.com")]
        public void SelectTemplate_Link(string link)
        {
            Assert.Equal(_sut.LinkEditor, _sut.SelectTemplate(link, null));
        }
    }
}
