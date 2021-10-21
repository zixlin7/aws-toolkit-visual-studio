using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class ConfigurationDetailExtensionMethodsTests
    {
        [Fact]
        public void GetDetailAndDescendants_Null()
        {
            List<ConfigurationDetail> details = null;
            // ReSharper disable once ExpressionIsAlwaysNull
            var result = details.GetDetailAndDescendants();

            Assert.Empty(result);
        }

        [Fact]
        public void GetDetailAndDescendants_FlatList()
        {
            List<ConfigurationDetail> details = new List<ConfigurationDetail>()
            {
                new ConfigurationDetail(),
                new ConfigurationDetail(),
            };

            var result = details.GetDetailAndDescendants();

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public void GetDetailAndDescendants_FilteredFlatList()
        {
            List<ConfigurationDetail> details = new List<ConfigurationDetail>()
            {
                ConfigurationDetailBuilder.Create().WithName("aaa").Build(),
                ConfigurationDetailBuilder.Create().WithName("bbb").Build(),
                ConfigurationDetailBuilder.Create().WithName("ccc").Build(),
            };

            var result = details.GetDetailAndDescendants(d => d.Name != "bbb").ToList();

            Assert.Equal(2, result.Count);
            Assert.Contains(result, detail => detail.Name == "aaa");
            Assert.Contains(result, detail => detail.Name == "ccc");
        }

        [Fact]
        public void GetDetailAndDescendants_NestedChildren()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithName("A")
                .WithChild(ConfigurationDetailBuilder.Create().WithName("A1"))
                .WithChild(ConfigurationDetailBuilder.Create().WithName("A2")
                    .WithChild(ConfigurationDetailBuilder.Create().WithName("A2i"))
                    .WithChild(ConfigurationDetailBuilder.Create().WithName("A2ii"))
                ).Build();

            List<ConfigurationDetail> details = new List<ConfigurationDetail>()
            {
                detail,
                detail,
            };

            var result = details.GetDetailAndDescendants();

            Assert.Equal(10, result.Count());
        }

        [Fact]
        public void GetDetailAndDescendants_FilteredNestedChildren()
        {
            var details = new List<ConfigurationDetail>()
            {
                ConfigurationDetailBuilder.Create()
                    .WithName("vehicles - marker")
                    .WithChild(ConfigurationDetailBuilder.Create().WithName("trucks")
                        // (Ford) should be excluded because its parent is filtered out
                        .WithChild(ConfigurationDetailBuilder.Create().WithName("Ford - marker"))
                        .WithChild(ConfigurationDetailBuilder.Create().WithName("Dodge"))
                    ).WithChild(ConfigurationDetailBuilder.Create().WithName("cars - marker")
                        .WithChild(ConfigurationDetailBuilder.Create().WithName("VW - marker")
                            .WithChild(ConfigurationDetailBuilder.Create().WithName("Jetta - marker"))
                            .WithChild(ConfigurationDetailBuilder.Create().WithName("Golf"))
                        )
                        .WithChild(ConfigurationDetailBuilder.Create().WithName("Tesla"))
                    ).Build()
            };

            var result = details.GetDetailAndDescendants(detail => detail.Name.Contains("marker")).ToList();

            Assert.Equal(4, result.Count());
            Assert.Contains(result, detail => detail.Name == "vehicles - marker");
            Assert.Contains(result, detail => detail.Name == "cars - marker");
            Assert.Contains(result, detail => detail.Name == "VW - marker");
            Assert.Contains(result, detail => detail.Name == "Jetta - marker");
        }

        [Fact]
        public void GenerateSummary_Publish()
        {
            var details = SampleConfigurationDetails();

            var result = details.GenerateSummary( false);
            var expected = CreatePublishExpected();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateSummary_Republish()
        {
            var details = SampleConfigurationDetails();

            var result = details.GenerateSummary(true);
            var expected = CreateRepublishExpected();

            Assert.Equal(expected, result);
        }

        [Fact]
        public void GenerateSummaryForLeaf()
        {
            var details = new List<ConfigurationDetail>()
            {
                ConfigurationDetailBuilder.Create()
                    .WithName("config1")
                    .WithValue("val1")
                    .IsVisible()
                    .Build()
            };

            var result = details.GenerateSummary(false);
            Assert.Equal("config1: val1\r\n", result);
        }

        [Fact]
        public void GenerateSummaryForParent()
        {
            var details = new List<ConfigurationDetail>()
            {
                ConfigurationDetailBuilder.Create()
                    .WithName("config1")
                    .WithType(typeof(object))
                    .IsVisible()
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("child1")
                        .WithValue("val1")
                        .IsVisible())
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("child2")
                        .WithValue("val2"))
                    .Build()
            };

            var result = details.GenerateSummary(false);
            Assert.Contains("config1:",result);
            Assert.Contains("child1: val1", result);
            Assert.DoesNotContain("child2:", result);
        }

        private string CreatePublishExpected()
        {
            return $"vehicles - marker:\r\n" +
                   $"    trucks:\r\n" +
                   $"        Ford - marker: ford\r\n" +
                   $"        Dodge: dodge\r\n" +
                   $"    Bikes: bike\r\n" +
                   $"fuel: diesel\r\n" +
                   $"music:\r\n" +
                   $"    spotify: False\r\n" +
                   $"    audible: 1\r\n";
        }

        private string CreateRepublishExpected()
        {
            return $"vehicles - marker:\r\n" +
                   $"    cars - marker:\r\n" +
                   $"        VW - marker:\r\n" +
                   $"            Jetta - marker: jetta\r\n" +
                   $"        Honda:\r\n" +
                   $"            CRV: crv\r\n" +
                   $"fuel: diesel\r\n" +
                   $"food:\r\n" +
                   $"    burger\r\n" +
                   $"    fries: 1\r\n";
        }

        private IList<ConfigurationDetail> SampleConfigurationDetails()
        {
            return new List<ConfigurationDetail>()
            {
                ConfigurationDetailBuilder.Create()
                    .WithName("vehicles - marker")
                    .WithType(typeof(object))
                    .IsVisible()
                    .IsSummaryDisplayable()
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("trucks")
                        .WithType(typeof(object))
                        .IsVisible()
                        .WithChild(ConfigurationDetailBuilder.Create()
                            .WithName("Ford - marker")
                            .WithValue("ford")
                            .IsVisible()
                        )
                        .WithChild(ConfigurationDetailBuilder.Create()
                            .WithName("Dodge")
                            .WithValue("dodge")
                            .IsVisible()
                        )
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("cars - marker")
                        .WithType(typeof(object))
                        .IsVisible()
                        .IsAdvanced()
                        .IsSummaryDisplayable()
                        .WithChild(ConfigurationDetailBuilder.Create()
                            .WithName("VW - marker")
                            .WithType(typeof(object))
                            .IsVisible()
                            .IsAdvanced()
                            .IsSummaryDisplayable()
                            .WithChild(ConfigurationDetailBuilder.Create()
                                .WithName("Jetta - marker")
                                .WithValue("jetta")
                                .IsSummaryDisplayable()
                                .IsVisible()
                            )
                            .WithChild(ConfigurationDetailBuilder.Create()
                                .WithName("Golf")
                                .WithValue("golf"))
                        )
                        .WithChild(ConfigurationDetailBuilder.Create()
                            .WithName("Honda")
                            .WithType(typeof(object))
                            .IsAdvanced()
                            .IsSummaryDisplayable()
                            .WithChild(ConfigurationDetailBuilder.Create()
                                .WithName("CRV")
                                .WithValue("crv")
                                .IsSummaryDisplayable()
                            )
                        )
                        .WithChild(ConfigurationDetailBuilder.Create()
                            .WithName("Toyota")
                            .WithType(typeof(object))
                            .IsVisible()
                            .WithChild(ConfigurationDetailBuilder.Create()
                                .WithName("Camry")
                                .IsVisible()
                            )
                        )
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("Bikes")
                        .WithValue("bike")
                        .IsVisible()
                    ).Build(),
                ConfigurationDetailBuilder.Create()
                    .WithName("fuel")
                    .WithValue("diesel")
                    .IsVisible()
                    .IsSummaryDisplayable()
                    .Build(),
                ConfigurationDetailBuilder.Create()
                    .WithName("music")
                    .WithType(typeof(object))
                    .IsVisible()
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("spotify")
                        .WithType(typeof(bool))
                        .WithValue(false)
                        .IsVisible()
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("audible")
                        .WithValue(1)
                        .IsVisible()
                    ).Build(),
                ConfigurationDetailBuilder.Create()
                    .WithName("food")
                    .WithType(typeof(object))
                    .IsSummaryDisplayable()
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("burger")
                        .WithType(typeof(bool))
                        .WithValue(true)
                        .IsSummaryDisplayable()
                    )
                    .WithChild(ConfigurationDetailBuilder.Create()
                        .WithName("fries")
                        .WithValue(1)
                        .IsSummaryDisplayable()
                    ).Build(),
            };
        }
    }
}
