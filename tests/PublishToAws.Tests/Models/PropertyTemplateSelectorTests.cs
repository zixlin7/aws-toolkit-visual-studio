﻿using System;
using System.Collections.Generic;
using System.Windows;

using Amazon.AWSToolkit.Publish.Models;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class PropertyTemplateSelectorTests
    {
        private readonly PropertyTemplateSelector _sut = new PropertyTemplateSelector()
        {
            BlankEditor = new DataTemplate(),
            BooleanEditor = new DataTemplate(),
            NumericEditor = new DataTemplate(),
            TextEditor = new DataTemplate(),
        };

        [Fact]
        public void SelectTemplate_NullItem()
        {
            Assert.Null(_sut.SelectTemplate(null, null));
        }

        [Fact]
        public void SelectTemplate_NonConfigurationDetailItem()
        {
            Assert.Null(_sut.SelectTemplate("hello-world", null));
        }

        [Fact]
        public void SelectTemplate_StringDetail()
        {
            var detail = new ConfigurationDetail() {Type = typeof(string)};

            Assert.Equal(_sut.TextEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_ObjectDetail()
        {
            var detail = new ConfigurationDetail() {Type = typeof(object)};

            Assert.Equal(_sut.BlankEditor, _sut.SelectTemplate(detail, null));
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(double))]
        public void SelectTemplate_NumericDetail(Type numericType)
        {
            var detail = new ConfigurationDetail() {Type = numericType};

            Assert.Equal(_sut.NumericEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_BoolDetail()
        {
            var detail = new ConfigurationDetail() {Type = typeof(bool)};

            Assert.Equal(_sut.BooleanEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_UnknownDetail()
        {
            var detail = new ConfigurationDetail() {Type = typeof(float)};

            Assert.Null(_sut.SelectTemplate(detail, null));
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(float))]
        public void SelectTemplate_EnumDetail(Type type)
        {
            var detail = new ConfigurationDetail()
            {
                Type = type,
                ValueMappings = new Dictionary<string, string>() {{"256", "256"}}
            };

            Assert.Equal(_sut.EnumEditor, _sut.SelectTemplate(detail, null));
        }

        public static IEnumerable<object[]> ValueMappingData = new List<object[]>
        {
            new object[] {null},
            new object[] {new Dictionary<string, string>()},
        };

        [Theory]
        [MemberData(nameof(ValueMappingData))]
        public void SelectTemplate_ShouldIgnoreEnumEditor(Dictionary<string, string> dict)
        {
            var detail = new ConfigurationDetail()
            {
                Type = typeof(string),
                ValueMappings = dict
            };

            Assert.Equal(_sut.TextEditor, _sut.SelectTemplate(detail, null));
        }
    }
}
